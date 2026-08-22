using DupDetector.Core.Detection;
using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using DupDetector.Core.Scoring;

namespace DupDetector.Core.Pipeline;

/// <summary>
/// A note the pipeline needs to surface to its caller.
/// </summary>
public sealed record AnalysisNote(string Message);

/// <summary>
/// The outcome of a run: the report plus anything the caller should be told.
/// </summary>
public sealed record AnalysisResult(DetectionReport Report, IReadOnlyList<AnalysisNote> Notes);

/// <summary>
/// Runs extraction, detection, filtering and scoring over already-loaded source.
/// </summary>
// Touches no console, filesystem or clock, so the whole analysis is directly testable.
public static class AnalysisPipeline
{
    public static AnalysisResult Run(
        IReadOnlyList<SourceUnit> units,
        DetectionSettings settings,
        DiscoveryStats discovery,
        CancellationToken cancellationToken = default) =>
        Run(units, settings, discovery, CliqueBudget.Default, cancellationToken);

    public static AnalysisResult Run(
        IReadOnlyList<SourceUnit> units,
        DetectionSettings settings,
        DiscoveryStats discovery,
        CliqueBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(discovery);

        // Files reachable through several input paths are analysed once.
        var distinct = units
            .GroupBy(unit => unit.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(unit => unit.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var blocks = new List<CodeBlock>();
        var files = new List<SourceFile>(distinct.Length);

        foreach (var unit in distinct)
        {
            cancellationToken.ThrowIfCancellationRequested();
            blocks.AddRange(MemberBlockExtractor.Extract(unit, settings));

            // Only the descriptor is retained, so syntax trees become collectable here rather than
            // being held for the lifetime of the run.
            files.Add(unit.ToFile());
        }

        cancellationToken.ThrowIfCancellationRequested();
        var detected = DuplicateDetector.DetectDetailed(blocks, settings, budget);
        var outcome = ClusterFilters.ApplyDetailed(detected, settings);
        var clusters = outcome.Clusters;

        cancellationToken.ThrowIfCancellationRequested();
        var fileScores = AggregateScorer.ScoreFiles(files, clusters);
        var projectScores = AggregateScorer.ScoreProjects(fileScores);
        var scope = new AnalysisScope { Settings = settings, Suppressed = outcome.Suppressed };
        var summary = AggregateScorer.Summarize(fileScores, clusters, discovery);

        return new AnalysisResult(
            new DetectionReport(summary, clusters, fileScores, projectScores) { Scope = scope },
            Describe(clusters, files, settings));
    }

    /// <summary>
    /// Reports conditions that would otherwise silently change what the numbers mean.
    /// </summary>
    private static List<AnalysisNote> Describe(
        IReadOnlyList<DuplicateCluster> clusters,
        IReadOnlyList<SourceFile> files,
        DetectionSettings settings)
    {
        var notes = new List<AnalysisNote>();

        if (settings.MinProjectSpread > 1 && files.Any(file => !file.Project.IsKnown))
        {
            notes.Add(new AnalysisNote(
                $"--min-project-spread {settings.MinProjectSpread} could not be applied to every cluster: " +
                "some files have no project file, so their project spread is unknown."));
        }

        var degraded = clusters.Count(cluster => !cluster.IsCohesive);
        if (degraded > 0)
        {
            notes.Add(new AnalysisNote(
                $"{degraded} cluster(s) exceeded the grouping budget and were grouped by connectivity, " +
                "so some members may not resemble one another. They are marked isCohesive: false."));
        }

        return notes;
    }
}
