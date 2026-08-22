namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     The named member a result points at.
/// </summary>
public sealed record SarifLogicalLocation
{
    /// <summary>
    ///     
    /// </summary>
    public required string FullyQualifiedName { get; init; }
}
