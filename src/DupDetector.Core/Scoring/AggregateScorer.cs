using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

namespace DupDetector.Core.Scoring;

/// <summary>
///     Turns clusters into file, project and run level percentages.
/// </summary>
public static class AggregateScorer
{
    /// <summary>
    ///     Distinct duplicated lines per file path.
    /// </summary>
    /// <param name="clusters">The clusters whose instances contribute duplicated lines.</param>
    /// <returns>A count of distinct duplicated lines, keyed by file path.</returns>
    public static IReadOnlyDictionary<string, int> DuplicateLinesByFile(IEnumerable<DuplicateCluster> clusters)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in DuplicateRangesByFile(clusters))
        {
            var total = 0;
            foreach (var range in entry.Value)
            {
                total += range.Count;
            }

            counts[entry.Key] = total;
        }

        return counts;
    }

    /// <summary>
    ///     Distinct duplicated line ranges per file path, already merged.
    /// </summary>
    /// <param name="clusters">The clusters whose instances contribute duplicated ranges.</param>
    /// <returns>The merged duplicated ranges, keyed by file path.</returns>
    public static IReadOnlyDictionary<string, IReadOnlyList<LineRange>> DuplicateRangesByFile(
        IEnumerable<DuplicateCluster> clusters)
    {
        var ranges = new Dictionary<string, List<LineRange>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cluster in clusters)
        {
            foreach (var instance in cluster.Instances)
            {
                if (!ranges.TryGetValue(instance.FilePath, out var list))
                {
                    list = [];
                    ranges[instance.FilePath] = list;
                }

                list.Add(instance.Lines);
            }
        }

        var merged = new Dictionary<string, IReadOnlyList<LineRange>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ranges)
        {
            merged[entry.Key] = LineSpanMerger.Merge(entry.Value);
        }

        return merged;
    }

    /// <summary>
    ///     Expresses a part of a whole as a rounded percentage, treating a zero whole as zero percent.
    /// </summary>
    /// <param name="part">The duplicated portion.</param>
    /// <param name="whole">The total the portion is measured against.</param>
    /// <returns>The percentage, rounded to two places.</returns>
    public static double Percentage(int part, int whole)
    {
        return whole == 0 ? 0.0 : RoundPercentage(part * 100.0 / whole);
    }

    /// <summary>
    ///     Rounds a percentage to two places, away from zero rather than to even.
    /// </summary>
    /// <param name="value">The percentage to round.</param>
    /// <returns>The rounded percentage.</returns>
    public static double RoundPercentage(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    ///     Scores every file by the share of its lines that participate in a cluster.
    /// </summary>
    /// <param name="files">The files that were analysed.</param>
    /// <param name="clusters">The clusters reported for the run.</param>
    /// <returns>The file scores, densest first.</returns>
    public static IReadOnlyList<FileScore> ScoreFiles(
        IReadOnlyList<SourceFile> files,
        IReadOnlyList<DuplicateCluster> clusters)
    {
        var duplicateRanges = DuplicateRangesByFile(clusters);
        var byFile = GroupClustersByFile(clusters);
        var scores = new List<FileScore>(files.Count);

        foreach (var file in files)
        {
            var ranges = duplicateRanges.TryGetValue(file.Path, out var found) ? found : [];
            var affecting = byFile.TryGetValue(file.Path, out var list) ? list : [];
            scores.Add(ScoreFile(file, ranges, affecting));
        }

        scores.Sort(CompareByDensity);
        return scores;
    }

    /// <summary>
    ///     Aggregates file scores into per-project scores.
    /// </summary>
    /// <param name="fileScores">The scored files.</param>
    /// <returns>The project scores, densest first.</returns>
    public static IReadOnlyList<ProjectScore> ScoreProjects(IReadOnlyList<FileScore> fileScores)
    {
        var duplicatedByProject = new Dictionary<ProjectIdentity, int>();
        var totalByProject = new Dictionary<ProjectIdentity, int>();
        var order = new List<ProjectIdentity>();

        foreach (var score in fileScores)
        {
            if (!totalByProject.ContainsKey(score.Project))
            {
                order.Add(score.Project);
                duplicatedByProject[score.Project] = 0;
                totalByProject[score.Project] = 0;
            }

            duplicatedByProject[score.Project] += score.DuplicateLines;
            totalByProject[score.Project] += score.TotalLines;
        }

        var scores = new List<ProjectScore>(order.Count);
        foreach (var project in order)
        {
            var duplicated = duplicatedByProject[project];
            var total = totalByProject[project];
            var score = new ProjectScore
            {
                Project = project,
                DuplicateLines = duplicated,
                TotalLines = total,
                Percentage = Percentage(duplicated, total),
            };
            scores.Add(score);
        }

        scores.Sort(CompareProjectsByDensity);
        return scores;
    }

    /// <summary>
    ///     Totals the run.
    /// </summary>
    /// <param name="fileScores">The scored files.</param>
    /// <param name="clusters">The clusters reported for the run.</param>
    /// <param name="discovery">How the files were located.</param>
    /// <returns>The run level totals.</returns>
    public static ReportSummary Summarize(
        IReadOnlyList<FileScore> fileScores,
        IReadOnlyList<DuplicateCluster> clusters,
        DiscoveryStats discovery)
    {
        var duplicated = 0;
        var total = 0;
        var duplicatedCode = 0;
        var totalCode = 0;

        foreach (var score in fileScores)
        {
            duplicated += score.DuplicateLines;
            total += score.TotalLines;
            duplicatedCode += score.DuplicateCodeLines;
            totalCode += score.CodeLines;
        }

        var summary = new ReportSummary
        {
            TotalFiles = fileScores.Count,
            TotalClusters = clusters.Count,
            TotalDuplicateLines = duplicated,
            TotalLines = total,
            DuplicationPercentage = Percentage(duplicated, total),
            Discovery = discovery,
            TotalCodeLines = totalCode,
            TotalDuplicateCodeLines = duplicatedCode,
            CodeDuplicationPercentage = Percentage(duplicatedCode, totalCode),
        };

        return summary;
    }

    private static int CompareByDensity(FileScore left, FileScore right)
    {
        var byPercentage = right.Percentage.CompareTo(left.Percentage);
        return byPercentage != 0
            ? byPercentage
            : string.Compare(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);
    }

    private static int CompareProjectsByDensity(ProjectScore left, ProjectScore right)
    {
        var byPercentage = right.Percentage.CompareTo(left.Percentage);
        return byPercentage != 0
            ? byPercentage
            : string.Compare(left.Project.ToString(), right.Project.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, List<DuplicateCluster>> GroupClustersByFile(
        IReadOnlyList<DuplicateCluster> clusters)
    {
        var byFile = new Dictionary<string, List<DuplicateCluster>>(StringComparer.OrdinalIgnoreCase);
        foreach (var cluster in clusters)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var instance in cluster.Instances)
            {
                if (!seen.Add(instance.FilePath))
                {
                    continue;
                }

                if (!byFile.TryGetValue(instance.FilePath, out var list))
                {
                    list = [];
                    byFile[instance.FilePath] = list;
                }

                list.Add(cluster);
            }
        }

        return byFile;
    }

    private static FileScore ScoreFile(
        SourceFile file,
        IReadOnlyList<LineRange> ranges,
        List<DuplicateCluster> affecting)
    {
        var duplicated = 0;
        var duplicatedCode = 0;
        foreach (var range in ranges)
        {
            duplicated += range.Count;
            duplicatedCode += file.CodeLines.CountIn(range);
        }

        var widest = 0;
        foreach (var cluster in affecting)
        {
            if (cluster.Metrics.FileSpread > widest)
            {
                widest = cluster.Metrics.FileSpread;
            }
        }

        var score = new FileScore
        {
            Path = file.Path,
            Project = file.Project,
            DuplicateLines = duplicated,
            TotalLines = file.LineCount,
            Percentage = Percentage(duplicated, file.LineCount),
            IsTestFile = file.IsTestFile,
            ClusterCount = affecting.Count,
            WidestClusterSpread = widest,
            CodeLines = file.CodeLines.Total,
            DuplicateCodeLines = duplicatedCode,
        };

        return score;
    }
}
