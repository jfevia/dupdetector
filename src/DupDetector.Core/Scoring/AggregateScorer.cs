using DupDetector.Core.Model;

namespace DupDetector.Core.Scoring;

/// <summary>
/// Turns clusters into file, project and run level percentages.
/// </summary>
/// <remarks>
/// Every percentage is duplicated lines over total lines. Because duplicated lines are counted with
/// <see cref="LineSpanMerger"/>, the numerator can never exceed the denominator and no clamp is needed.
/// </remarks>
public static class AggregateScorer
{
    /// <summary>Rounds a percentage to two places, away from zero rather than to even.</summary>
    public static double RoundPercentage(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static double Percentage(int part, int whole) =>
        whole == 0 ? 0.0 : RoundPercentage(part * 100.0 / whole);

    /// <summary>Distinct duplicated line ranges per file path, already merged.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<LineRange>> DuplicateRangesByFile(
        IEnumerable<DuplicateCluster> clusters)
    {
        ArgumentNullException.ThrowIfNull(clusters);

        var ranges = new Dictionary<string, List<LineRange>>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in clusters.SelectMany(cluster => cluster.Instances))
        {
            if (!ranges.TryGetValue(instance.FilePath, out var list))
            {
                list = [];
                ranges[instance.FilePath] = list;
            }

            list.Add(instance.Lines);
        }

        return ranges.ToDictionary(
            entry => entry.Key,
            entry => LineSpanMerger.Merge(entry.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Distinct duplicated lines per file path.</summary>
    public static IReadOnlyDictionary<string, int> DuplicateLinesByFile(IEnumerable<DuplicateCluster> clusters) =>
        DuplicateRangesByFile(clusters).ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Sum(range => range.Count),
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<FileScore> ScoreFiles(
        IReadOnlyList<SourceFile> files,
        IReadOnlyList<DuplicateCluster> clusters)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(clusters);

        var duplicateRanges = DuplicateRangesByFile(clusters);
        var byFile = new Dictionary<string, List<DuplicateCluster>>(StringComparer.OrdinalIgnoreCase);

        foreach (var cluster in clusters)
        {
            foreach (var path in cluster.Instances.Select(instance => instance.FilePath).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!byFile.TryGetValue(path, out var list))
                {
                    list = [];
                    byFile[path] = list;
                }

                list.Add(cluster);
            }
        }

        return [.. files
            .Select(file =>
            {
                var ranges = duplicateRanges.TryGetValue(file.Path, out var found) ? found : [];
                var duplicated = ranges.Sum(range => range.Count);
                var affecting = byFile.TryGetValue(file.Path, out var list) ? list : [];
                return new FileScore(
                    file.Path,
                    file.Project,
                    duplicated,
                    file.LineCount,
                    Percentage(duplicated, file.LineCount),
                    file.IsTestFile,
                    affecting.Count,
                    affecting.Count == 0 ? 0 : affecting.Max(cluster => cluster.Metrics.FileSpread))
                {
                    CodeLines = file.CodeLines.Total,
                    DuplicateCodeLines = ranges.Sum(file.CodeLines.CountIn),
                };
            })
            .OrderByDescending(score => score.Percentage)
            .ThenBy(score => score.Path, StringComparer.OrdinalIgnoreCase)];
    }

    public static IReadOnlyList<ProjectScore> ScoreProjects(IReadOnlyList<FileScore> fileScores)
    {
        ArgumentNullException.ThrowIfNull(fileScores);

        return [.. fileScores
            .GroupBy(score => score.Project)
            .Select(group =>
            {
                var duplicated = group.Sum(score => score.DuplicateLines);
                var total = group.Sum(score => score.TotalLines);
                return new ProjectScore(group.Key, duplicated, total, Percentage(duplicated, total));
            })
            .OrderByDescending(score => score.Percentage)
            .ThenBy(score => score.Project.ToString(), StringComparer.OrdinalIgnoreCase)];
    }

    public static ReportSummary Summarize(
        IReadOnlyList<FileScore> fileScores,
        IReadOnlyList<DuplicateCluster> clusters,
        DiscoveryStats discovery)
    {
        ArgumentNullException.ThrowIfNull(fileScores);
        ArgumentNullException.ThrowIfNull(clusters);

        var duplicated = fileScores.Sum(score => score.DuplicateLines);
        var total = fileScores.Sum(score => score.TotalLines);
        var duplicatedCode = fileScores.Sum(score => score.DuplicateCodeLines);
        var totalCode = fileScores.Sum(score => score.CodeLines);

        return new ReportSummary(
            fileScores.Count,
            clusters.Count,
            duplicated,
            total,
            Percentage(duplicated, total),
            discovery)
        {
            TotalCodeLines = totalCode,
            TotalDuplicateCodeLines = duplicatedCode,
            CodeDuplicationPercentage = Percentage(duplicatedCode, totalCode),
        };
    }
}
