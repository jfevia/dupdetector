namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     The tool that produced the run.
/// </summary>
public sealed record SarifDriver
{

    /// <summary>
    ///     
    /// </summary>
    public required string InformationUri { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifRule> Rules { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Version { get; init; }
}
