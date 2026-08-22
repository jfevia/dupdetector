using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
///     What changed between a baseline and the current run.
/// </summary>
public sealed record BaselineDelta
{

    /// <summary>
    ///     Gets the clusters absent from the baseline.
    /// </summary>
    public IReadOnlyList<DuplicateCluster> Added { get; }

    /// <summary>
    ///     Gets the clusters that gained copies since the baseline.
    /// </summary>
    public IReadOnlyList<DuplicateCluster> Grown { get; }

    /// <summary>
    ///     Gets a value indicating whether the run introduced duplication.
    /// </summary>
    public bool IsRegression
    {
        get
        {
            return Added.Count > 0 || Grown.Count > 0;
        }
    }

    /// <summary>
    ///     Gets the change in duplication, in percentage points.
    /// </summary>
    public double PercentagePointChange { get; }

    /// <summary>
    ///     Gets the baseline clusters no longer present.
    /// </summary>
    public IReadOnlyList<BaselineCluster> Removed { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BaselineDelta"/> class.
    /// </summary>
    /// <param name="change">The clusters that appeared, grew or were resolved.</param>
    /// <param name="percentagePointChange">The change in duplication, in percentage points.</param>
    public BaselineDelta(BaselineChange change, double percentagePointChange)
    {
        Added = change.Added;
        Grown = change.Grown;
        Removed = change.Removed;
        PercentagePointChange = percentagePointChange;
    }
}
