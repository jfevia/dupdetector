namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A SARIF message or description string.
/// </summary>
public sealed record SarifText
{
    /// <summary>
    ///     
    /// </summary>
    public required string Text { get; init; }
}
