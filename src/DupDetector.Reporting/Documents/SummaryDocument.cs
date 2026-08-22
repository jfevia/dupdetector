namespace DupDetector.Reporting.Documents;

/// <summary>
///     
/// </summary>
public sealed class SummaryDocument
{

    /// <summary>
    ///     Duplication over analysable lines, comparable with tools that report against NCLOC.
    /// </summary>
    public required double CodeDuplicationPercentage { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int DiscoveredFiles { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string DiscoveryMode { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required double DuplicationPercentage { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ExcludedFiles { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalClusters { get; init; }

    /// <summary>
    ///     Lines carrying code, excluding blanks and comments.
    /// </summary>
    public required int TotalCodeLines { get; init; }

    /// <summary>
    ///     Duplicated lines carrying code.
    /// </summary>
    public required int TotalDuplicateCodeLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalDuplicateLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalLines { get; init; }
}
