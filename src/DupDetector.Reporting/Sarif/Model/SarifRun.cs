namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A single analysis run.
/// </summary>
public sealed record SarifRun
{

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifInvocation> Invocations { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifSummary Properties { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SarifResult> Results { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifTool Tool { get; init; }
}
