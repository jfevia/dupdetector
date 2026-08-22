using DupDetector.Sources.Workspaces;
using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     A workspace host backed by an in-memory solution, so every loading rule and error path can be
///     exercised without an SDK.
/// </summary>
public sealed class StubWorkspaceHost : IWorkspaceHost
{
    private readonly Exception? _openFailure;
    private readonly AdhocWorkspace _workspace;
    private ProjectId? _current;

    /// <summary>
    ///     
    /// </summary>
    public List<SourceDiagnostic> Recorded { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="openFailure"></param>
    public StubWorkspaceHost()
        : this(null)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="openFailure"></param>
    public StubWorkspaceHost(Exception? openFailure)
    {

        Recorded = [];

        _workspace = new();
        _openFailure = openFailure;
    }

    /// <summary>
    ///     
    /// </summary>
    public void Dispose()
    {
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
        diagnostics.AddRange(Recorded);
        return _openFailure is null ? LoadedProjects : throw _openFailure;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="projectPath"></param>
    /// <param name="diagnostics"></param>
    /// <param name="cancellationToken"></param>
    public void OpenAdditional(string projectPath, List<SourceDiagnostic> diagnostics, CancellationToken cancellationToken)
    {
        diagnostics.AddRange(Recorded);
        Recorded.Clear();
    }

    /// <summary>
    ///     Adds a document to the project added most recently.
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="text"></param>
    public StubWorkspaceHost WithDocument(string path, string text)
    {
        var solution = _workspace.CurrentSolution.AddDocument(
            DocumentId.CreateNewId(_current!),
            Path.GetFileName(path),
            SourceText.From(text),
            filePath: path);

        Assert.True(_workspace.TryApplyChanges(solution));
        return this;
    }

    /// <summary>
    ///     Adds an empty project to the in-memory solution and makes it current.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    /// <param name="projectFilePath"></param>
    public StubWorkspaceHost WithProject(string name, string projectFilePath)
    {
        _current = ProjectId.CreateNewId(name);
        var info = ProjectInfo.Create(
            _current,
            VersionStamp.Default,
            name,
            name,
            LanguageNames.CSharp,
            filePath: projectFilePath);

        var solution = _workspace.CurrentSolution.AddProject(info);
        Assert.True(_workspace.TryApplyChanges(solution));
        return this;
    }
}
