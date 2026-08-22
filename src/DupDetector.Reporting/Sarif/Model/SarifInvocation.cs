using System.Text.Json.Serialization;

namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     How the run was started.
/// </summary>
public sealed record SarifInvocation
{

    /// <summary>
    ///     
    /// </summary>
    public string? CommandLine { get; init; }

    /// <summary>
    ///     
    /// </summary>
    [JsonPropertyName("executionSuccessful")]
    public required bool IsExecutionSuccessful { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public SarifSettings? Properties { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public string? StartTimeUtc { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public SarifArtifactLocation? WorkingDirectory { get; init; }
}
