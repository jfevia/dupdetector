using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

namespace DupDetector.Sources;

/// <summary>
///     Everything a load produced: the files, how many were seen and skipped, and what went wrong.
/// </summary>
public sealed record SourceLoadResult
{
    /// <summary>
    ///     A result that loaded nothing.
    /// </summary>
    public static SourceLoadResult Empty { get; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SourceDiagnostic> Diagnostics { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required DiscoveryStats Stats { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<SourceUnit> Units { get; init; }

    static SourceLoadResult()
    {
        Empty = new()
        {
            Units = [],
            Stats = DiscoveryStats.Empty,
            Diagnostics = [],
        };
    }
}
