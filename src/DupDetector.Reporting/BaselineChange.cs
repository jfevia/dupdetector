using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
///     The three sets of clusters a comparison produces.
/// </summary>
public sealed record BaselineChange
{

    /// <summary>
    ///     Gets the clusters absent from the baseline.
    /// </summary>
    public IReadOnlyList<DuplicateCluster> Added { get; }

    /// <summary>
    ///     Gets the clusters that gained copies.
    /// </summary>
    public IReadOnlyList<DuplicateCluster> Grown { get; }

    /// <summary>
    ///     Gets the baseline clusters no longer present.
    /// </summary>
    public IReadOnlyList<BaselineCluster> Removed { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaselineChange"/> class.
    /// </summary>
    /// <param name="added">The clusters absent from the baseline.</param>
    /// <param name="grown">The clusters that gained copies.</param>
    /// <param name="removed">The baseline clusters no longer present.</param>
    public BaselineChange(
        IReadOnlyList<DuplicateCluster> added,
        IReadOnlyList<DuplicateCluster> grown,
        IReadOnlyList<BaselineCluster> removed)
    {
        Added = added;
        Grown = grown;
        Removed = removed;
    }
}
