namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     The SARIF tool wrapper.
/// </summary>
public sealed record SarifTool
{
    /// <summary>
    ///     
    /// </summary>
    public required SarifDriver Driver { get; init; }
}
