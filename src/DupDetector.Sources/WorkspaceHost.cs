using DupDetector.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DupDetector.Sources;

/// <summary>
/// Opens projects and solutions. All MSBuild coupling lives behind this seam, so the loading rules
/// above it can be exercised without an SDK.
/// </summary>
internal interface IWorkspaceHost : IDisposable
{
    /// <summary>Projects currently loaded, including those pulled in as references.</summary>
    IReadOnlyList<Project> LoadedProjects { get; }

    /// <summary>Opens a project or solution. Throws when the input cannot be opened at all.</summary>
    IReadOnlyList<Project> Open(string path, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken);

    /// <summary>Adds a project to the workspace unless it is already present.</summary>
    void OpenAdditional(string projectPath, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken);
}

/// <summary>
/// The real MSBuild-backed host.
/// </summary>
internal sealed class MsBuildWorkspaceHost : IWorkspaceHost
{
    private readonly MSBuildWorkspace _workspace = MSBuildWorkspace.Create();
    private readonly List<SourceDiagnostic> _diagnostics = [];
    private readonly WorkspaceEventRegistration _failures;

    internal MsBuildWorkspaceHost() =>
        _failures = _workspace.RegisterWorkspaceFailedHandler(failure =>
        {
            if (WorkspaceDiagnostics.Describe(failure.Diagnostic) is { } diagnostic)
            {
                _diagnostics.Add(diagnostic);
            }
        });

    public IReadOnlyList<Project> LoadedProjects => [.. _workspace.CurrentSolution.Projects];

    public IReadOnlyList<Project> Open(string path, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        IReadOnlyList<Project> projects = Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            ? [_workspace.OpenProjectAsync(path, cancellationToken: cancellationToken).GetAwaiter().GetResult()]
            : [.. _workspace.OpenSolutionAsync(path, cancellationToken: cancellationToken).GetAwaiter().GetResult().Projects];

        Drain(diagnostics);
        return projects;
    }

    public void OpenAdditional(string projectPath, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var alreadyLoaded = _workspace.CurrentSolution.Projects.Any(project =>
            string.Equals(project.FilePath, projectPath, StringComparison.OrdinalIgnoreCase));

        if (alreadyLoaded)
        {
            return;
        }

        try
        {
            _workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).GetAwaiter().GetResult();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or NotSupportedException)
        {
            diagnostics.Add(SourceDiagnostic.Warning($"Could not load project: {exception.Message}", projectPath));
        }

        Drain(diagnostics);
    }

    public void Dispose()
    {
        _failures.Dispose();
        _workspace.Dispose();
    }

    private void Drain(List<SourceDiagnostic> diagnostics)
    {
        diagnostics.AddRange(_diagnostics);
        _diagnostics.Clear();
    }
}

/// <summary>
/// Translates workspace diagnostics into loader diagnostics.
/// </summary>
internal static class WorkspaceDiagnostics
{
    /// <summary>
    /// Returns <c>null</c> for notices that carry no information, such as a project appearing twice
    /// through transitive references; documents are deduplicated by path regardless.
    /// </summary>
    internal static SourceDiagnostic? Describe(WorkspaceDiagnostic diagnostic)
    {
        if (diagnostic.Message.Contains("already part of the workspace", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new SourceDiagnostic(
            diagnostic.Kind == WorkspaceDiagnosticKind.Failure
                ? SourceDiagnosticSeverity.Error
                : SourceDiagnosticSeverity.Warning,
            diagnostic.Message);
    }
}

/// <summary>
/// Turns loaded projects into source units.
/// </summary>
internal static class WorkspaceHarvester
{
    internal static SourceLoadResult Collect(
        IReadOnlyList<Project> projects,
        string root,
        DetectionSettings settings,
        CancellationToken cancellationToken)
    {
        var excludes = Core.Matching.GlobSet.Parse(settings.ExcludeFileGlobs);
        var units = new List<SourceUnit>();
        var diagnostics = new List<SourceDiagnostic>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var discovered = 0;
        var excluded = 0;

        foreach (var project in projects)
        {
            var identity = ProjectIdentity.Named(project.Name);

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.FilePath is not { } documentPath || !seen.Add(documentPath))
                {
                    continue;
                }

                discovered++;

                var relative = FileSystemSourceProvider.Relative(root, documentPath);
                if (FileSystemSourceProvider.IsArtifact(relative) || excludes.IsMatch(documentPath))
                {
                    excluded++;
                    continue;
                }

                var isTestFile = Core.Matching.TestFileClassifier.IsTestFile(relative, identity);
                if (settings.ExcludeTestFiles && isTestFile)
                {
                    excluded++;
                    continue;
                }

                var text = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
                if (GeneratedFileDetector.IsGenerated(documentPath, text))
                {
                    excluded++;
                    continue;
                }

                var tree = SourceParser.Parse(text, documentPath);
                if (SourceParser.DescribeParseFailures(tree, documentPath) is { } failure)
                {
                    diagnostics.Add(failure);
                }

                units.Add(new SourceUnit(documentPath, relative, text, tree, identity, isTestFile));
            }
        }

        return new SourceLoadResult(
            units,
            new DiscoveryStats(discovered, excluded, DiscoveryMode.Workspace),
            diagnostics);
    }
}
