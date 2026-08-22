namespace DupDetector.Core.Detection;

/// <summary>
/// Records how many clusters each threshold discarded.
/// </summary>
// A duplication percentage means nothing without the thresholds that produced it.
public sealed record SuppressionCounts
{
    public int BelowFileSpread { get; init; }

    public int BelowProjectSpread { get; init; }

    public int AboveFileSpread { get; init; }

    public int AboveOccurrences { get; init; }

    public int ContainedInLargerCluster { get; init; }

    public int ExcludedBySnippetPattern { get; init; }

    public int ExcludedByFileGlob { get; init; }

    public int ExcludedByProjectPattern { get; init; }

    /// <summary>Total clusters found by detection but not reported.</summary>
    public int Total =>
        BelowFileSpread + BelowProjectSpread + AboveFileSpread + AboveOccurrences +
        ContainedInLargerCluster + ExcludedBySnippetPattern + ExcludedByFileGlob + ExcludedByProjectPattern;

    public static SuppressionCounts Empty { get; } = new();
}
