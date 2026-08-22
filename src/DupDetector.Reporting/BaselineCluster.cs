namespace DupDetector.Reporting;

/// <summary>
///     Identity and size of one cluster as it stood in a previous run.
/// </summary>
public sealed class BaselineCluster
{
    /// <summary>
    ///     Gets the identity that survives copies being added, which the comparison keys on.
    /// </summary>
    public required string ContentKey { get; init; }

    /// <summary>
    ///     Gets the cluster identifier at the time the baseline was written.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     Gets the number of copies at that time.
    /// </summary>
    public required int Occurrences { get; init; }

    /// <summary>
    ///     Gets the removable lines at that time.
    /// </summary>
    public required int RemovableLines { get; init; }
}
