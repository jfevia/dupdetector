namespace DupDetector.Reporting.Documents;

/// <summary>
///     The thresholds a run applied and the clusters they withheld.
/// </summary>
public sealed class ScopeDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required bool IsExcludeTestFiles { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Kinds { get; init; }

    /// <summary>
    ///     Plain-language statements of what this run did not measure.
    /// </summary>
    public required IReadOnlyList<string> Limitations { get; init; }

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

    /// <summary>
    ///     
    /// </summary>
    public required SuppressedDocument Suppressed { get; init; }
}
