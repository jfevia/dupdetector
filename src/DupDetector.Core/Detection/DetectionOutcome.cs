using DupDetector.Core.Model;

namespace DupDetector.Core.Detection;

/// <summary>
///     The clusters a run reports, together with what it chose not to report.
/// </summary>
public sealed record DetectionOutcome
{

    /// <summary>
    ///     Gets the clusters to report.
    /// </summary>
    public IReadOnlyList<DuplicateCluster> Clusters { get; }

    /// <summary>
    ///     Gets the per-reason counts of what was withheld.
    /// </summary>
    public SuppressionCounts Suppressed { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DetectionOutcome"/> class.
    /// </summary>
    /// <param name="clusters">The clusters to report.</param>
    /// <param name="suppressed">The per-reason counts of what was withheld.</param>
    public DetectionOutcome(IReadOnlyList<DuplicateCluster> clusters, SuppressionCounts suppressed)
    {
        Clusters = clusters;
        Suppressed = suppressed;
    }
}
