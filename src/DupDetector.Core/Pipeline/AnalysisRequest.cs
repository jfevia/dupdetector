using DupDetector.Core.Detection;
using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

namespace DupDetector.Core.Pipeline;

/// <summary>
///     Everything a run needs, other than its cancellation token.
/// </summary>
public sealed record AnalysisRequest
{
    /// <summary>
    ///     Gets the ceiling on clique enumeration work.
    /// </summary>
    public required CliqueBudget Budget { get; init; }

    /// <summary>
    ///     Gets how the files were located.
    /// </summary>
    public required DiscoveryStats Discovery { get; init; }

    /// <summary>
    ///     Gets the thresholds that decide what is reported.
    /// </summary>
    public required DetectionSettings Settings { get; init; }

    /// <summary>
    ///     Gets the parsed source files to analyse.
    /// </summary>
    public required IReadOnlyList<SourceUnit> Units { get; init; }
}
