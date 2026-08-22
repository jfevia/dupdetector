using DupDetector.Core.Model.Reporting;
using System.Globalization;
using System.Text.Json;

namespace DupDetector.Reporting;

/// <summary>
///     Creates and reads <see cref="Baseline"/> values.
/// </summary>
public static class Baselines
{
    /// <summary>
    ///     Records the current run as a baseline for a later comparison.
    /// </summary>
    /// <param name="report">The report to record.</param>
    /// <param name="time">The clock supplying the timestamp.</param>
    /// <returns>The baseline.</returns>
    public static Baseline From(DetectionReport report, TimeProvider time)
    {
        var clusters = new List<BaselineCluster>(report.Clusters.Count);
        foreach (var cluster in report.Clusters)
        {
            var entry = new BaselineCluster
            {
                Id = cluster.Id,
                ContentKey = cluster.ContentKey,
                RemovableLines = cluster.Metrics.RemovableLines,
                Occurrences = cluster.Metrics.Occurrences,
            };

            clusters.Add(entry);
        }

        var baseline = new Baseline
        {
            GeneratedAtUtc = time.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            DuplicationPercentage = report.Summary.DuplicationPercentage,
            CodeDuplicationPercentage = report.Summary.CodeDuplicationPercentage,
            Clusters = clusters,
        };

        return baseline;
    }

    /// <summary>
    ///     Reads a baseline, rejecting malformed content rather than comparing against nothing.
    /// </summary>
    /// <param name="json">The serialized baseline.</param>
    /// <returns>The parsed baseline.</returns>
    public static Baseline Parse(string json)
    {
        var formatException = new FormatException("The baseline file is empty.");
        var parsed = JsonSerializer.Deserialize<Baseline>(json, JsonReports.Standalone)
            ?? throw formatException;

        return parsed;
    }
}
