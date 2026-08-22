namespace DupDetector.Core.Detection;

/// <summary>
///     Records how many clusters each threshold discarded.
/// </summary>
public sealed record SuppressionCounts
{

    /// <summary>
    ///     Gets the counts for a run that withheld nothing.
    /// </summary>
    public static SuppressionCounts Empty { get; }

    /// <summary>
    ///     Gets the clusters withheld for spanning too many files.
    /// </summary>
    public int AboveFileSpread { get; init; }

    /// <summary>
    ///     Gets the clusters withheld for having too many copies.
    /// </summary>
    public int AboveOccurrences { get; init; }

    /// <summary>
    ///     Gets the clusters withheld for spanning too few files.
    /// </summary>
    public int BelowFileSpread { get; init; }

    /// <summary>
    ///     Gets the clusters withheld for spanning too few projects.
    /// </summary>
    public int BelowProjectSpread { get; init; }

    /// <summary>
    ///     Gets the clusters withheld because a larger cluster already covers them.
    /// </summary>
    public int ContainedInLargerCluster { get; init; }

    /// <summary>
    ///     Gets the clusters withheld by a file glob.
    /// </summary>
    public int ExcludedByFileGlob { get; init; }

    /// <summary>
    ///     Gets the clusters withheld by a project pattern.
    /// </summary>
    public int ExcludedByProjectPattern { get; init; }

    /// <summary>
    ///     Gets the clusters withheld by a snippet pattern.
    /// </summary>
    public int ExcludedBySnippetPattern { get; init; }

    /// <summary>
    ///     Gets the total clusters found by detection but not reported.
    /// </summary>
    public int Total
    {
        get
        {
            return BelowFileSpread + BelowProjectSpread + AboveFileSpread + AboveOccurrences +
                ContainedInLargerCluster + ExcludedBySnippetPattern + ExcludedByFileGlob + ExcludedByProjectPattern;
        }
    }

    static SuppressionCounts()
    {
        var empty = new SuppressionCounts();
        Empty = empty;
    }
}
