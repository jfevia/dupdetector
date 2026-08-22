using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     A source provider that reports one discovered file in a fixed discovery mode.
/// </summary>
public sealed class StubSourceProvider : ISourceProvider
{
    private readonly DiscoveryMode _mode;

    /// <summary>
    ///     
    /// </summary>
    /// <param name="mode">The discovery mode to report.</param>
    public StubSourceProvider(DiscoveryMode mode)
    {
        _mode = mode;
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
        var stats = new DiscoveryStats
        {
            Discovered = 1,
            Excluded = 0,
            Mode = _mode,
        };

        var result = new SourceLoadResult
        {
            Units = [],
            Stats = stats,
            Diagnostics = [],
        };

        return result;
    }
}
