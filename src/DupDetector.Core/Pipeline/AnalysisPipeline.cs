using DupDetector.Core.Detection;
using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;

using DupDetector.Core.Scoring;

namespace DupDetector.Core.Pipeline;

/// <summary>
///     Runs extraction, detection, filtering and scoring over already-loaded source.
/// </summary>
public static class AnalysisPipeline
{
    /// <summary>
    ///     Runs the analysis with the default grouping budget and no cancellation.
    /// </summary>
    /// <param name="units">The parsed source files to analyse.</param>
    /// <param name="settings">The thresholds that decide what is reported.</param>
    /// <param name="discovery">How the files were located.</param>
    /// <returns>The report and any notes the caller should be told.</returns>
    public static AnalysisResult Run(
        IReadOnlyList<SourceUnit> units,
        DetectionSettings settings,
        DiscoveryStats discovery)
    {
        var request = new AnalysisRequest
        {
            Units = units,
            Settings = settings,
            Discovery = discovery,
            Budget = CliqueBudget.Default,
        };

        return Run(request, CancellationToken.None);
    }

    /// <summary>
    ///     Runs the analysis with the default grouping budget.
    /// </summary>
    /// <param name="units">The parsed source files to analyse.</param>
    /// <param name="settings">The thresholds that decide what is reported.</param>
    /// <param name="discovery">How the files were located.</param>
    /// <param name="cancellationToken">Cancels the run between files and between stages.</param>
    /// <returns>The report and any notes the caller should be told.</returns>
    public static AnalysisResult Run(
        IReadOnlyList<SourceUnit> units,
        DetectionSettings settings,
        DiscoveryStats discovery,
        CancellationToken cancellationToken)
    {
        var request = new AnalysisRequest
        {
            Units = units,
            Settings = settings,
            Discovery = discovery,
            Budget = CliqueBudget.Default,
        };

        return Run(request, cancellationToken);
    }

    /// <summary>
    ///     Runs the analysis under an explicit grouping budget.
    /// </summary>
    /// <param name="units">The parsed source files to analyse.</param>
    /// <param name="settings">The thresholds that decide what is reported.</param>
    /// <param name="discovery">How the files were located.</param>
    /// <param name="budget">The ceiling on clique enumeration work.</param>
    /// <returns>The report and any notes the caller should be told.</returns>
    public static AnalysisResult Run(
        IReadOnlyList<SourceUnit> units,
        DetectionSettings settings,
        DiscoveryStats discovery,
        CliqueBudget budget)
    {
        var request = new AnalysisRequest
        {
            Units = units,
            Settings = settings,
            Discovery = discovery,
            Budget = budget,
        };

        return Run(request, CancellationToken.None);
    }

    /// <summary>
    ///     Reports conditions that would otherwise silently change what the numbers mean.
    /// </summary>
    /// <param name="clusters">The clusters reported for the run.</param>
    /// <param name="files">The files that were analysed.</param>
    /// <param name="settings">The thresholds that were applied.</param>
    /// <returns>The notes to surface, which may be empty.</returns>
    private static List<AnalysisNote> Describe(
        IReadOnlyList<DuplicateCluster> clusters,
        IReadOnlyList<SourceFile> files,
        DetectionSettings settings)
    {
        var notes = new List<AnalysisNote>();
        var anyProjectUnknown = false;
        foreach (var file in files)
        {
            if (!file.Project.IsKnown)
            {
                anyProjectUnknown = true;
                break;
            }
        }

        if (settings.MinProjectSpread > 1 && anyProjectUnknown)
        {
            var note = new AnalysisNote(
                $"--min-project-spread {settings.MinProjectSpread} could not be applied to every cluster: " +
                "some files have no project file, so their project spread is unknown.");
            notes.Add(note);
        }

        var degraded = 0;
        foreach (var cluster in clusters)
        {
            if (!cluster.IsCohesive)
            {
                degraded++;
            }
        }

        if (degraded > 0)
        {
            var note = new AnalysisNote(
                $"{degraded} cluster(s) exceeded the grouping budget and were grouped by connectivity, " +
                "so some members may not resemble one another. They are marked isCohesive: false.");
            notes.Add(note);
        }

        return notes;
    }

    private static List<SourceUnit> Distinct(IReadOnlyList<SourceUnit> units)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinct = new List<SourceUnit>(units.Count);
        foreach (var unit in units)
        {
            if (seen.Add(unit.Path))
            {
                distinct.Add(unit);
            }
        }

        distinct.Sort(static (left, right) => string.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase));
        return distinct;
    }

    private static AnalysisResult Run(AnalysisRequest request, CancellationToken cancellationToken)
    {
        var units = request.Units;
        var settings = request.Settings;
        var distinct = Distinct(units);
        var blocks = new List<CodeBlock>();
        var files = new List<SourceFile>(distinct.Count);

        foreach (var unit in distinct)
        {
            cancellationToken.ThrowIfCancellationRequested();
            blocks.AddRange(MemberBlockExtractor.Extract(unit, settings));
            files.Add(unit.ToFile());
        }

        cancellationToken.ThrowIfCancellationRequested();
        var detected = DuplicateDetector.DetectDetailed(blocks, settings, request.Budget);
        var outcome = ClusterFilters.ApplyDetailed(detected, settings);
        var clusters = outcome.Clusters;

        cancellationToken.ThrowIfCancellationRequested();
        var fileScores = AggregateScorer.ScoreFiles(files, clusters);
        var projectScores = AggregateScorer.ScoreProjects(fileScores);
        var scope = new AnalysisScope
        {
            Settings = settings,
            Suppressed = outcome.Suppressed,
        };

        var summary = AggregateScorer.Summarize(fileScores, clusters, request.Discovery);
        var report = new DetectionReport
        {
            Summary = summary,
            Clusters = clusters,
            FileScores = fileScores,
            ProjectScores = projectScores,
            Scope = scope,
        };

        var result = new AnalysisResult(report, Describe(clusters, files, settings));
        return result;
    }
}
