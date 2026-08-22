using System.Text.Json;

namespace DupDetector.Reporting;

/// <summary>
///     The minimum a run needs to record so a later run can say what changed.
/// </summary>
public sealed class Baseline
{
    /// <summary>
    ///     Gets the clusters as they stood when the baseline was written.
    /// </summary>
    public required IReadOnlyList<BaselineCluster> Clusters { get; init; }

    /// <summary>
    ///     Gets the duplication over analysable lines at that time.
    /// </summary>
    public required double CodeDuplicationPercentage { get; init; }

    /// <summary>
    ///     Gets the duplication over physical lines at that time.
    /// </summary>
    public required double DuplicationPercentage { get; init; }

    /// <summary>
    ///     Gets the moment the baseline was written.
    /// </summary>
    public required string GeneratedAtUtc { get; init; }

    /// <summary>
    ///     Serializes this baseline.
    /// </summary>
    /// <returns>The baseline as JSON.</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonReports.Standalone);
    }
}
