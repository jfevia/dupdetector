namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     The SARIF log root.
/// </summary>
public sealed record SarifDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifRun> Runs { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Schema { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Version { get; init; }
}
