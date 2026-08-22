namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     An inclusive line region inside an artifact.
/// </summary>
public sealed record SarifRegion
{

    /// <summary>
    ///     
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int StartLine { get; init; }
}
