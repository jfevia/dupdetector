using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
/// Loads every input path and merges the results.
/// </summary>
public sealed class SourceLoader(Func<string, ISourceProvider>? providerFactory = null)
{
    private readonly Func<string, ISourceProvider> _providerFactory = providerFactory ?? Resolve;

    /// <summary>Chooses the provider for a path by its extension.</summary>
    public static ISourceProvider Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (SlnxSourceProvider.Handles(path))
        {
            return new SlnxSourceProvider();
        }

        return MsBuildSourceProvider.Handles(path) ? new MsBuildSourceProvider() : new FileSystemSourceProvider();
    }

    /// <summary>
    /// Loads all paths, combining discovery counts and reporting the discovery mode as mixed when
    /// the inputs were reached different ways.
    /// </summary>
    public SourceLoadResult Load(
        IReadOnlyList<string> paths,
        DetectionSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(settings);

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
            1 => modes.First(),
            _ => DiscoveryMode.Mixed,
        };

        return new SourceLoadResult(units, new DiscoveryStats(discovered, excluded, mode), diagnostics);
    }
}
