namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     Where a result was found.
/// </summary>
public sealed record SarifLocation
{

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifLogicalLocation> LogicalLocations { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifPhysicalLocation PhysicalLocation { get; init; }
}
