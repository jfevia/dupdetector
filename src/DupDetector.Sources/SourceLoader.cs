using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

namespace DupDetector.Sources;

/// <summary>
///     Loads every input path and merges the results.
/// </summary>
public sealed class SourceLoader
{
    private readonly SourceProviderFactory _providerFactory;

    /// <summary>
    ///     
    /// </summary>
    public SourceLoader()
        : this(SourceLoaders.Resolve)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="providerFactory"></param>
    public SourceLoader(SourceProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    /// <summary>
    ///     Loads all paths, combining discovery counts and reporting the discovery mode as mixed when
    ///     the inputs were reached different ways.
    /// </summary>
    /// <param name="paths">The files, directories, projects or solutions to load.</param>
    /// <param name="settings">The settings that decide which files are skipped.</param>
    /// <param name="cancellationToken">Cancels the load between paths.</param>
    /// <returns>The loaded units, discovery counts and any diagnostics.</returns>
    public SourceLoadResult Load(
        IReadOnlyList<string> paths,
        DetectionSettings settings,
        CancellationToken cancellationToken)
    {
        var units = new List<SourceUnit>();
        var diagnostics = new List<SourceDiagnostic>();
        var modes = new HashSet<DiscoveryMode>();
        var discovered = 0;
        var excluded = 0;

        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = _providerFactory(path).Load(path, settings, cancellationToken);
            units.AddRange(result.Units);
            diagnostics.AddRange(result.Diagnostics);
            discovered += result.Stats.Discovered;
            excluded += result.Stats.Excluded;

            if (result.Stats.Mode != DiscoveryMode.None)
            {
                modes.Add(result.Stats.Mode);
            }
        }

        var mode = modes.Count switch
        {
            0 => DiscoveryMode.None,
            1 => FirstMode(modes),
            _ => DiscoveryMode.Mixed,
        };

        static DiscoveryMode FirstMode(HashSet<DiscoveryMode> found)
        {
            using var values = found.GetEnumerator();
            return values.MoveNext() ? values.Current : DiscoveryMode.None;
        }

        var discoveryStats = new DiscoveryStats
        {
            Discovered = discovered,
            Excluded = excluded,
            Mode = mode
        };

        var sourceLoadResult = new SourceLoadResult
        {
            Units = units,
            Stats = discoveryStats,
            Diagnostics = diagnostics,
        };
        return sourceLoadResult;
    }
}
