using DupDetector.Core.Model;

namespace DupDetector.Core.Detection;

/// <summary>
/// The clusters a run reports, together with what it chose not to report.
/// </summary>
public sealed record DetectionOutcome(
    IReadOnlyList<DuplicateCluster> Clusters,
    SuppressionCounts Suppressed);
