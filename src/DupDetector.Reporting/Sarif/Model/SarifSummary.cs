namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     Run totals and what the thresholds withheld.
/// </summary>
public sealed record SarifSummary
{

    /// <summary>
    ///     
    /// </summary>
    public required double CodeDuplicationPercentage { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required double DuplicationPercentage { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public IReadOnlyList<string>? Limitations { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int SuppressedClusters { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int TotalClusters { get; init; }
}
