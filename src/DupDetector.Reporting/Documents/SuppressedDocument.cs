namespace DupDetector.Reporting.Documents;

/// <summary>
///     Clusters that were detected but withheld, by reason.
/// </summary>
public sealed class SuppressedDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required int AboveFileSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int AboveOccurrences { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int BelowFileSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int BelowProjectSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ContainedInLargerCluster { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ExcludedByFileGlob { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ExcludedByProjectPattern { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ExcludedBySnippetPattern { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int Total { get; init; }
}
