namespace DupDetector.Reporting.Documents;

/// <summary>
///     
/// </summary>
public sealed class FileScoreDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required int ClusterCount { get; init; }

    /// <summary>
    ///     Lines carrying code, excluding blanks and comments.
    /// </summary>
    public required int CodeLines { get; init; }

    /// <summary>
    ///     Duplicated lines carrying code.
    /// </summary>
    public required int DuplicateCodeLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int DuplicateLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required bool IsTestFile { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required double Percentage { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int WidestClusterSpread { get; init; }
}
