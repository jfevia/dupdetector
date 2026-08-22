namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     File discovery counts for a run.
/// </summary>
public sealed record DiscoveryStats
{

    /// <summary>
    ///     Gets the stats for a run that discovered nothing.
    /// </summary>
    public static DiscoveryStats Empty { get; }

    /// <summary>
    ///     Gets the number of files seen before exclusions.
    /// </summary>
    public required int Discovered { get; init; }

    /// <summary>
    ///     Gets the number of files skipped by any rule.
    /// </summary>
    public required int Excluded { get; init; }

    /// <summary>
    ///     Gets how the files were located.
    /// </summary>
    public required DiscoveryMode Mode { get; init; }

    static DiscoveryStats()
    {
        var empty = new DiscoveryStats
        {
            Discovered = 0,
            Excluded = 0,
            Mode = DiscoveryMode.None,
        };

        Empty = empty;
    }
}
