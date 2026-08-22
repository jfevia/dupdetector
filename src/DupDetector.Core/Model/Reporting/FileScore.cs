namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     Share of a file's lines that participate in at least one duplicate cluster.
/// </summary>
public sealed record FileScore
{
    /// <summary>
    ///     Gets the number of clusters touching this file.
    /// </summary>
    public required int ClusterCount { get; init; }

    /// <summary>
    ///     Gets the lines carrying code, excluding blanks and comments.
    /// </summary>
    public int CodeLines { get; init; }

    /// <summary>
    ///     Gets the duplicated lines that carry code.
    /// </summary>
    public int DuplicateCodeLines { get; init; }

    /// <summary>
    ///     Gets the distinct duplicated lines in this file.
    /// </summary>
    public required int DuplicateLines { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the file is classified as test code.
    /// </summary>
    public required bool IsTestFile { get; init; }

    /// <summary>
    ///     Gets the absolute path of the file.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    ///     Gets the share of the file's lines that are duplicated.
    /// </summary>
    public required double Percentage { get; init; }

    /// <summary>
    ///     Gets the project the file belongs to.
    /// </summary>
    public required ProjectIdentity Project { get; init; }

    /// <summary>
    ///     Gets the physical line count of the file.
    /// </summary>
    public required int TotalLines { get; init; }

    /// <summary>
    ///     Gets the file spread of the widest cluster touching this file.
    /// </summary>
    public required int WidestClusterSpread { get; init; }
}
