using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.MSBuild;

namespace DupDetector.Sources.Workspaces;

/// <summary>
///     The real MSBuild-backed host.
/// </summary>
public sealed class MicrosoftBuildWorkspaceHost : IWorkspaceHost
{
    private readonly List<SourceDiagnostic> _diagnostics;
    private readonly WorkspaceEventRegistration _failures;
    private readonly MSBuildWorkspace _workspace;

    /// <summary>
    ///     
    /// </summary>
    public MicrosoftBuildWorkspaceHost()
    {
        _diagnostics = [];
        _workspace = MSBuildWorkspace.Create();

        _failures = _workspace.RegisterWorkspaceFailedHandler(failure =>
        {
            if (WorkspaceDiagnostics.Describe(failure.Diagnostic) is { } diagnostic)
            {
                _diagnostics.Add(diagnostic);
            }
        });
    }

    /// <summary>
    ///     
    /// </summary>
    public void Dispose()
    {
        _failures.Dispose();
        _workspace.Dispose();
    }

    /// <summary>
    ///     
    /// </summary>
    public IReadOnlyList<Project> LoadedProjects
    {
        get
        {
            return [.. _workspace.CurrentSolution.Projects];
        }
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="diagnostics"></param>
    /// <param name="cancellationToken"></param>
    public IReadOnlyList<Project> Open(string path, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        IReadOnlyList<Project> projects = Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            ? [_workspace.OpenProjectAsync(path, cancellationToken: cancellationToken).GetAwaiter().GetResult()]
            : [.. _workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken).GetAwaiter().GetResult().Projects];

        Drain(diagnostics);
        return projects;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="diagnostics"></param>
    /// <param name="cancellationToken"></param>
    public void OpenAdditional(string projectPath, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        foreach (var loaded in _workspace.CurrentSolution.Projects)
        {
            if (string.Equals(loaded.FilePath, projectPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        try
        {
            _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or NotSupportedException)
        {
            diagnostics.Add(SourceDiagnostics.Warning($"Could not load project: {exception.Message}", projectPath));
        }

        Drain(diagnostics);
    }

    private void Drain(List<SourceDiagnostic> diagnostics)
    {
        diagnostics.AddRange(_diagnostics);
        _diagnostics.Clear();
    }
}
