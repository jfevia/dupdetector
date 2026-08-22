using System.Text.Json.Serialization;

namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     The thresholds a run applied.
/// </summary>
public sealed record SarifSettings
{

    /// <summary>
    ///     
    /// </summary>
    [JsonPropertyName("excludeTestFiles")]
    public required bool IsExcludeTestFiles { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Kinds { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MaxFileSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MaxOccurrences { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MinFileSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MinLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MinProjectSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int MinTypeLines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required double Similarity { get; init; }
}
