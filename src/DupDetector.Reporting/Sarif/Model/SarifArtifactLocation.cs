namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A SARIF artifact location.
/// </summary>
public sealed record SarifArtifactLocation
{
    /// <summary>
    ///     
    /// </summary>
    public required string Uri { get; init; }
}
