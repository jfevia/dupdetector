using DupDetector.Core.Model;

using DupDetector.Sources.Workspaces;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Loads source from a solution or project via MSBuild, so files carry real project identity.
/// </summary>
public sealed class MicrosoftBuildSourceProvider : ISourceProvider
{
    private readonly WorkspaceHostFactory _createHost;

    /// <summary>
    ///     
    /// </summary>
    public MicrosoftBuildSourceProvider()
        : this(WorkspaceHosts.Create)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="createHost"></param>
    public MicrosoftBuildSourceProvider(WorkspaceHostFactory createHost)
    {
        _createHost = createHost;
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

        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            return SourceLoadResult.Empty with
            {
                Diagnostics = [SourceDiagnostics.Error($"Path does not exist: {full}", full)],
            };
        }

        var diagnostics = new List<SourceDiagnostic>();
        using var host = _createHost();

        try
        {
            var projects = host.Open(full, diagnostics, cancellationToken);
            var harvest = WorkspaceHarvester.Collect(projects, Path.GetDirectoryName(full)!, settings, cancellationToken);
            diagnostics.AddRange(harvest.Diagnostics);

            if (harvest.Units.Count == 0 && !diagnostics.Exists(MicrosoftBuildSources.IsError))
            {
                diagnostics.Add(SourceDiagnostics.Error(
                    $"'{full}' produced no source files. This usually means the SDK or a package restore is " +
                    "missing; an empty report would otherwise look like a clean solution.",
                    full));
            }

            return harvest with
            {
                Diagnostics = diagnostics
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or NotSupportedException)
        {
            diagnostics.Add(SourceDiagnostics.Error($"Could not open '{full}': {exception.Message}", full));
            return SourceLoadResult.Empty with
            {
                Diagnostics = diagnostics
            };
        }
    }
}
