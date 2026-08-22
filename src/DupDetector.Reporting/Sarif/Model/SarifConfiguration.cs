namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A SARIF rule default configuration.
/// </summary>
public sealed record SarifConfiguration
{
    /// <summary>
    ///     
    /// </summary>
    public required string Level { get; init; }
}
