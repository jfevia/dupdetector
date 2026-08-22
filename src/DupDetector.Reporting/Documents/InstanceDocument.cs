namespace DupDetector.Reporting.Documents;

/// <summary>
///     
/// </summary>
public sealed class InstanceDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string File { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required bool IsTestFile { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Member { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int StartLine { get; init; }
}
