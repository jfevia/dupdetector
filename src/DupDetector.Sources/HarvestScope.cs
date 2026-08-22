using DupDetector.Core.Matching;

using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
///     What a harvest run needs to decide which files it keeps.
/// </summary>
public sealed record HarvestScope
{
    /// <summary>
    ///     Files matching any of these globs are skipped.
    /// </summary>
    public required GlobSet Excludes { get; init; }

    /// <summary>
    ///     The directory relative paths are measured from.
    /// </summary>
    public required string Root { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required DetectionSettings Settings { get; init; }
}
