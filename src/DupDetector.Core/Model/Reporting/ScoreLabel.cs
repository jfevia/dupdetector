namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     Severity band for a percentage duplication score.
/// </summary>
public enum ScoreLabel
{
    /// <summary>
    ///     Below the industry gate.
    /// </summary>
    Low,

    /// <summary>
    ///     At or above the industry gate but still moderate.
    /// </summary>
    Medium,

    /// <summary>
    ///     Substantial duplication.
    /// </summary>
    High,

    /// <summary>
    ///     Pervasive duplication.
    /// </summary>
    Critical,
}
