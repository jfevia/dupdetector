using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Sources;

using DupDetector.Sources.Providers;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Returns whatever diagnostics the test supplies, without touching a disk.
/// </summary>
public sealed class DiagnosticProvider : ISourceProvider
{
    private readonly IReadOnlyList<SourceDiagnostic> _diagnostics;

    /// <summary>
    ///     
    /// </summary>
    /// <param name="diagnostics">The diagnostics to report.</param>
    public DiagnosticProvider(IReadOnlyList<SourceDiagnostic> diagnostics)
    {
        _diagnostics = diagnostics;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken)
    {
        var result = new SourceLoadResult
        {
            Units = [],
            Stats = DiscoveryStats.Empty,
            Diagnostics = _diagnostics,
        };

        return result;
    }
}
