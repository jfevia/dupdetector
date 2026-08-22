using System.Xml;
using System.Xml.Linq;
using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
/// Loads source from a <c>.slnx</c> solution file.
/// </summary>
// .slnx is the XML solution format, not a solution filter.
// A transitively referenced project must still be harvested, or every file it owns is dropped silently.
public sealed class SlnxSourceProvider : ISourceProvider
{
    private readonly Func<IWorkspaceHost> _createHost;

    public SlnxSourceProvider()
        : this(static () => new MsBuildWorkspaceHost())
    {
    }

    internal SlnxSourceProvider(Func<IWorkspaceHost> createHost) => _createHost = createHost;

    public static bool Handles(string path) =>
        Path.GetExtension(path).Equals(".slnx", StringComparison.OrdinalIgnoreCase);

    public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(settings);

        var full = Path.GetFullPath(path);
        if (!File.Exists(full))
        {
            return SourceLoadResult.Empty with
            {
                Diagnostics = [SourceDiagnostic.Error($"Path does not exist: {full}", full)],
            };
        }

        var root = Path.GetDirectoryName(full)!;
        var diagnostics = new List<SourceDiagnostic>();
        var declared = ReadProjectPaths(full, root, diagnostics);

        if (declared.Count == 0)
        {
            diagnostics.Add(SourceDiagnostic.Error($"No loadable projects were found in '{full}'.", full));
            return SourceLoadResult.Empty with { Diagnostics = diagnostics };
        }

        using var host = _createHost();
        foreach (var projectPath in declared.Order(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            host.OpenAdditional(projectPath, diagnostics, cancellationToken);
        }

        // Harvest from the whole workspace, so a project already present as someone else's
        // reference still contributes its files.
        var projects = host.LoadedProjects
            .Where(project => project.FilePath is { } file && declared.Contains(file))
            .OrderBy(project => project.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var harvest = WorkspaceHarvester.Collect(projects, root, settings, cancellationToken);
        diagnostics.AddRange(harvest.Diagnostics);
        return harvest with { Diagnostics = diagnostics };
    }

    /// <summary>
    /// Reads the declared project paths, warning about each missing project individually rather
    /// than only when every one of them is absent.
    /// </summary>
    internal static HashSet<string> ReadProjectPaths(string slnxPath, string root, List<SourceDiagnostic> diagnostics)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        XDocument document;
        try
        {
            document = XDocument.Load(slnxPath);
        }
        catch (XmlException exception)
        {
            diagnostics.Add(SourceDiagnostic.Error($"'{slnxPath}' is not valid XML: {exception.Message}", slnxPath));
            return paths;
        }

        foreach (var declared in document.Descendants("Project").Select(element => element.Attribute("Path")?.Value))
        {
            if (string.IsNullOrWhiteSpace(declared))
            {
                continue;
            }

            var resolved = Path.GetFullPath(Path.Combine(root, declared));
            if (File.Exists(resolved))
            {
                paths.Add(resolved);
            }
            else
            {
                diagnostics.Add(SourceDiagnostic.Warning("Project referenced by the solution is missing.", resolved));
            }
        }

        return paths;
    }
}
