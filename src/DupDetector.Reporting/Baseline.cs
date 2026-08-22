using System.Text.Json;
using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
/// The minimum a run needs to record so a later run can say what changed.
/// </summary>
/// <remarks>
/// Only cluster identity and size are kept. Storing the full report would make baselines large and
/// would embed absolute paths that break the moment a checkout moves.
/// </remarks>
public sealed class Baseline
{
    public required string GeneratedAtUtc { get; init; }

    public required double DuplicationPercentage { get; init; }

    public required double CodeDuplicationPercentage { get; init; }

    public required IReadOnlyList<BaselineCluster> Clusters { get; init; }

    public static Baseline From(DetectionReport report, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(time);

        return new Baseline
        {
            GeneratedAtUtc = time.GetUtcNow().UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            DuplicationPercentage = report.Summary.DuplicationPercentage,
            CodeDuplicationPercentage = report.Summary.CodeDuplicationPercentage,
            Clusters =
            [
                .. report.Clusters.Select(cluster => new BaselineCluster
                {
                    Id = cluster.Id,
                    ContentKey = cluster.ContentKey,
                    RemovableLines = cluster.Metrics.RemovableLines,
                    Occurrences = cluster.Metrics.Occurrences,
                }),
            ],
        };
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonReportWriter.Standalone);

    /// <summary>
    /// Reads a baseline, rejecting malformed content rather than silently comparing against nothing.
    /// </summary>
    public static Baseline Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var parsed = JsonSerializer.Deserialize<Baseline>(json, JsonReportWriter.Standalone)
            ?? throw new FormatException("The baseline file is empty.");

        return parsed;
    }
}

/// <summary>Identity and size of one cluster as it stood in a previous run.</summary>
public sealed class BaselineCluster
{
    public required string Id { get; init; }

    /// <summary>Identity that survives copies being added, which is what the comparison keys on.</summary>
    public required string ContentKey { get; init; }

    public required int RemovableLines { get; init; }

    public required int Occurrences { get; init; }
}

/// <summary>
/// What changed between a baseline and the current run.
/// </summary>
public sealed record BaselineDelta(
    IReadOnlyList<DuplicateCluster> Added,
    IReadOnlyList<BaselineCluster> Removed,
    IReadOnlyList<DuplicateCluster> Grown,
    double PercentagePointChange)
{
    /// <summary>True when the run introduced duplication that was not in the baseline.</summary>
    public bool IsRegression => Added.Count > 0 || Grown.Count > 0;

    public static BaselineDelta Between(Baseline baseline, DetectionReport report)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(report);

        // Keyed on content, not id: an id encodes the full membership, so a cluster that gained a
        // copy would otherwise look like an unrelated new one and growth could never be reported.
        var before = baseline.Clusters
            .GroupBy(cluster => cluster.ContentKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var currentKeys = report.Clusters.Select(cluster => cluster.ContentKey).ToHashSet(StringComparer.Ordinal);

        var added = new List<DuplicateCluster>();
        var grown = new List<DuplicateCluster>();

        foreach (var cluster in report.Clusters)
        {
            if (!before.TryGetValue(cluster.ContentKey, out var previous))
            {
                added.Add(cluster);
            }
            else if (cluster.Metrics.Occurrences > previous.Occurrences)
            {
                grown.Add(cluster);
            }
        }

        return new BaselineDelta(
            added,
            [.. baseline.Clusters.Where(cluster => !currentKeys.Contains(cluster.ContentKey))],
            grown,
            Math.Round(
                report.Summary.DuplicationPercentage - baseline.DuplicationPercentage,
                2,
                MidpointRounding.AwayFromZero));
    }
}
