using Microsoft.CodeAnalysis;

namespace DupDetector.Sources.Workspaces;

/// <summary>
///     Opens projects and solutions. All MSBuild coupling lives behind this seam, so the loading rules
///     above it can be exercised without an SDK.
/// </summary>
public interface IWorkspaceHost : IDisposable
{
    /// <summary>
    ///     Projects currently loaded, including those pulled in as references.
    /// </summary>
    IReadOnlyList<Project> LoadedProjects { get; }

    /// <summary>
    ///     Opens a project or solution. Throws when the input cannot be opened at all.
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="diagnostics"></param>
    /// <param name="cancellationToken"></param>
    IReadOnlyList<Project> Open(string path, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken);

    /// <summary>
    ///     Adds a project to the workspace unless it is already present.
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="diagnostics"></param>
    /// <param name="cancellationToken"></param>
    void OpenAdditional(string projectPath, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken);
}
