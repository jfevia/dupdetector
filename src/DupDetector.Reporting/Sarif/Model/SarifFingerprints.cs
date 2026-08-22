namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     Stable identity used to track a result across runs.
/// </summary>
public sealed record SarifFingerprints
{
    /// <summary>
    ///     
    /// </summary>
    public required string DupDetectorClusterId { get; init; }
}
