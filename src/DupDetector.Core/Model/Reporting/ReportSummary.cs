namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     Run-level totals.
/// </summary>
public sealed record ReportSummary
{
    /// <summary>
    ///     Gets the duplication over analysable lines, comparable with tools reporting against NCLOC.
    /// </summary>
    public double CodeDuplicationPercentage { get; init; }

    /// <summary>
    ///     Gets how the files were located.
    /// </summary>
    public required DiscoveryStats Discovery { get; init; }

    /// <summary>
    ///     Gets the duplication over physical lines.
    /// </summary>
    public required double DuplicationPercentage { get; init; }

    /// <summary>
    ///     Gets the severity band, read from the analysable figure when one was measured.
    /// </summary>
    public ScoreLabel Label
    {
        get
        {
            return ScoreLabels.For(TotalCodeLines > 0 ? CodeDuplicationPercentage : DuplicationPercentage);
        }
    }

    /// <summary>
    ///     Gets the number of clusters reported after filtering.
    /// </summary>
    public required int TotalClusters { get; init; }

    /// <summary>
    ///     Gets the lines carrying code across the run.
    /// </summary>
    public int TotalCodeLines { get; init; }

    /// <summary>
    ///     Gets the duplicated lines carrying code.
    /// </summary>
    public int TotalDuplicateCodeLines { get; init; }

    /// <summary>
    ///     Gets the distinct lines belonging to at least one cluster.
    /// </summary>
    public required int TotalDuplicateLines { get; init; }

    /// <summary>
    ///     Gets the number of files analysed.
    /// </summary>
    public required int TotalFiles { get; init; }

    /// <summary>
    ///     Gets the physical lines across analysed files.
    /// </summary>
    public required int TotalLines { get; init; }
}
