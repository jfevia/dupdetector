namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A file and line region.
/// </summary>
public sealed record SarifPhysicalLocation
{
    /// <summary>
    ///     
    /// </summary>
    public required SarifArtifactLocation ArtifactLocation { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifRegion Region { get; init; }
}
