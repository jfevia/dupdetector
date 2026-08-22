using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting;

/// <summary>
///     Compares a run against a baseline.
/// </summary>
public static class BaselineDeltas
{
    /// <summary>
    ///     Reports what changed between a baseline and the current run.
    /// </summary>
    /// <param name="baseline">The previously recorded state.</param>
    /// <param name="report">The current report.</param>
    /// <returns>The difference between the two.</returns>
    public static BaselineDelta Between(Baseline baseline, DetectionReport report)
    {
        var before = new Dictionary<string, BaselineCluster>(StringComparer.Ordinal);
        foreach (var cluster in baseline.Clusters)
        {
            if (!before.ContainsKey(cluster.ContentKey))
            {
                before[cluster.ContentKey] = cluster;
            }
        }

        var currentKeys = new HashSet<string>(StringComparer.Ordinal);
        var added = new List<DuplicateCluster>();
        var grown = new List<DuplicateCluster>();

        foreach (var cluster in report.Clusters)
        {
            currentKeys.Add(cluster.ContentKey);
            if (!before.TryGetValue(cluster.ContentKey, out var previous))
            {
                added.Add(cluster);
            }
            else if (cluster.Metrics.Occurrences > previous.Occurrences)
            {
                grown.Add(cluster);
            }
        }

        var removed = new List<BaselineCluster>();
        foreach (var cluster in baseline.Clusters)
        {
            if (!currentKeys.Contains(cluster.ContentKey))
            {
                removed.Add(cluster);
            }
        }

        var change = new BaselineChange(added, grown, removed);
        var shift = Math.Round(
            report.Summary.DuplicationPercentage - baseline.DuplicationPercentage,
            2,
            MidpointRounding.AwayFromZero);

        var delta = new BaselineDelta(change, shift);
        return delta;
    }
}
