using DupDetector.Core.Model;
using DupDetector.Sources.Workspaces;

using Microsoft.CodeAnalysis;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Loads source from a <c>.slnx</c> solution file.
/// </summary>
public sealed class SolutionXmlSourceProvider : ISourceProvider
{
    private readonly WorkspaceHostFactory _createHost;

    /// <summary>
    ///     
    /// </summary>
    public SolutionXmlSourceProvider()
        : this(WorkspaceHosts.Create)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="createHost"></param>
    public SolutionXmlSourceProvider(WorkspaceHostFactory createHost)
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

        var root = Path.GetDirectoryName(full)!;
        var diagnostics = new List<SourceDiagnostic>();
        var declared = SolutionXmlSources.ReadProjectPaths(full, root, diagnostics);

        if (declared.Count == 0)
        {
            diagnostics.Add(SourceDiagnostics.Error($"No loadable projects were found in '{full}'.", full));
            return SourceLoadResult.Empty with
            {
                Diagnostics = diagnostics
            };
        }

        using var host = _createHost();
        var ordered = new List<string>(declared);
        ordered.Sort(static (left, right) => string.Compare(left, right, StringComparison.OrdinalIgnoreCase));

        foreach (var projectPath in ordered)
        {
            cancellationToken.ThrowIfCancellationRequested();
            host.OpenAdditional(projectPath, diagnostics, cancellationToken);
        }

        var matched = new List<Project>();
        foreach (var project in host.LoadedProjects)
        {
            if (project.FilePath is { } file && declared.Contains(file))
            {
                matched.Add(project);
            }
        }

        matched.Sort(static (left, right) =>
            string.Compare(left.FilePath, right.FilePath, StringComparison.OrdinalIgnoreCase));
        var projects = matched;

        var harvest = WorkspaceHarvester.Collect(projects, root, settings, cancellationToken);
        diagnostics.AddRange(harvest.Diagnostics);
        return harvest with
        {
            Diagnostics = diagnostics
        };
    }
}
