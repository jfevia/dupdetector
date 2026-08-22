namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     One duplicate cluster expressed as a SARIF result.
/// </summary>
public sealed record SarifResult
{

    /// <summary>
    ///     
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifLocation> Locations { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifText Message { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifFingerprints PartialFingerprints { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifLocation> RelatedLocations { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string RuleId { get; init; }
}
