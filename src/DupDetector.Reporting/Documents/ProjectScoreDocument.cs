namespace DupDetector.Reporting.Documents;

/// <summary>
///     
/// </summary>
public sealed class ProjectScoreDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required int DuplicateLines { get; init; }

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
}
