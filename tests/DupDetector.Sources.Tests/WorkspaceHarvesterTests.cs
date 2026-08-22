using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Sources.Workspaces;
using Microsoft.CodeAnalysis;

using Microsoft.CodeAnalysis.Text;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     
/// </summary>
public class WorkspaceHarvesterTests
{
    private static readonly DetectionSettings Settings;

    static WorkspaceHarvesterTests()
    {
        Settings = new()
        {
            MinLines = 1
        };
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_CanExcludeTestFilesEntirely()
    {
        var stubWorkspaceHost = new StubWorkspaceHost();
        using var host = stubWorkspaceHost.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }")
            .WithProject("App.Tests", TestPaths.At("App.Tests.csproj"))
            .WithDocument(TestPaths.At("OneTests.cs"), "class OneTests { }");

        var included = WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, CancellationToken.None);
        Assert.Equal(2, included.Units.Count);
        Assert.Single(included.Units, unit => unit.IsTestFile);

        var excluded = WorkspaceHarvester.Collect(
            host.LoadedProjects,
            TestPaths.Root,
            Settings with
            {
                IsExcludeTestFiles = true
            },
            CancellationToken.None);

        Assert.Single(excluded.Units);
        Assert.Equal(1, excluded.Stats.Excluded);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_HonoursCancellation()
    {
        var stubWorkspaceHost2 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost2.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, cancellation.Token));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_IgnoresDocumentsWithoutPath()
    {
        var stubWorkspaceHost3 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost3.WithProject("App", TestPaths.At("App.csproj"));
        var projectId = host.LoadedProjects[0].Id;
        var solution = host.LoadedProjects[0].Solution.AddDocument(
            DocumentId.CreateNewId(projectId),
            "InMemory.cs",
            SourceText.From("class InMemory { }"));

        var result = WorkspaceHarvester.Collect(
            [.. solution.Projects],
            TestPaths.Root,
            Settings,
            CancellationToken.None);

        Assert.Empty(result.Units);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_ReadsDocumentsAndAssignsProjectIdentity()
    {
        var stubWorkspaceHost4 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost4.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }");

        var result = WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, CancellationToken.None);

        var unit = Assert.Single(result.Units);
        Assert.Equal(ProjectIdentities.Named("App"), unit.Project);
        Assert.Equal("One.cs", unit.RelativePath);
        Assert.Equal(DiscoveryMode.Workspace, result.Stats.Mode);
        Assert.Equal(1, result.Stats.Discovered);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_ReadsEachFileOnceEvenWhenSeveralProjectsShareIt()
    {
        var stubWorkspaceHost5 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost5.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }")
            .WithProject("App(net9.0)", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }");

        var result = WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Equal(1, result.Stats.Discovered);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_ReportsParseFailuresWithoutDroppingTheFile()
    {
        var stubWorkspaceHost6 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost6.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("Bad.cs"), "class C { void M( }");

        var result = WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Equal(SourceDiagnosticSeverity.Warning, Assert.Single(result.Diagnostics).Severity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_SkipsArtifactsAndExcludedGlobs()
    {
        var stubWorkspaceHost7 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost7.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }")
            .WithDocument(TestPaths.At("obj/Gen.cs"), "class Gen { }")
            .WithDocument(TestPaths.At("skip/Two.cs"), "class Two { }");

        var result = WorkspaceHarvester.Collect(
            host.LoadedProjects,
            TestPaths.Root,
            Settings with
            {
                ExcludeFileGlobs = ["skip/**"]
            },
            CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Equal(2, result.Stats.Excluded);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Collect_SkipsGeneratedFiles()
    {
        var stubWorkspaceHost8 = new StubWorkspaceHost();
        using var host = stubWorkspaceHost8.WithProject("App", TestPaths.At("App.csproj"))
            .WithDocument(TestPaths.At("One.cs"), "class One { }")
            .WithDocument(TestPaths.At("Two.cs"), "// <auto-generated />\nclass Two { }");

        var result = WorkspaceHarvester.Collect(host.LoadedProjects, TestPaths.Root, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Equal(1, result.Stats.Excluded);
    }
}
