using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Sources.Providers;

using System.Text;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
public class FileSystemSourceProviderTests
{
    private static readonly DetectionSettings Settings;

    static FileSystemSourceProviderTests()
    {
        Settings = new()
        {
            MinLines = 1
        };
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("obj/Debug/A.cs", true)]
    [InlineData("bin/Release/A.cs", true)]
    [InlineData("src/OBJ/A.cs", true)]
    [InlineData("bindings/A.cs", false)]
    [InlineData("src/A.cs", false)]
    public void IsArtifact_MatchesWholeSegmentsOnly(string relativePath, bool expected)
    {
        Assert.Equal(expected, FileSystemSources.IsArtifact(relativePath));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_AppliesExcludeGlobs()
    {
        using var tree = new TempTree();
        tree.Write("src/Service.cs", "class Service { }");
        tree.Write("gen/Model.cs", "class Model { }");

        var fileSystemSourceProvider = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider.Load(
            tree.Root,
            Settings with
            {
                ExcludeFileGlobs = ["gen/**"]
            }, CancellationToken.None);

        var unit = Assert.Single(result.Units);
        Assert.EndsWith("Service.cs", unit.Path, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ClassifiesTestFilesAndCanExcludeThemEntirely()
    {
        using var tree = new TempTree();
        tree.Write("src/Service.cs", "class Service { }");
        tree.Write("tests/ServiceTests.cs", "class ServiceTests { }");
        tree.Write("src/Latest.cs", "class Latest { }");

        var fileSystemSourceProvider2 = new FileSystemSourceProvider();
        var included = fileSystemSourceProvider2.Load(tree.Root, Settings, CancellationToken.None);
        Assert.Equal(3, included.Units.Count);
        Assert.Single(included.Units, unit => unit.IsTestFile);

        var fileSystemSourceProvider3 = new FileSystemSourceProvider();
        var excluded = fileSystemSourceProvider3.Load(tree.Root, Settings with
        {
            IsExcludeTestFiles = true
        }, CancellationToken.None);
        Assert.Equal(2, excluded.Units.Count);
        Assert.DoesNotContain(excluded.Units, unit => unit.IsTestFile);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_HonoursCancellation()
    {
        using var tree = new TempTree();
        tree.Write("Service.cs", "class Service { }");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var fileSystemSourceProvider4 = new FileSystemSourceProvider();
        Assert.Throws<OperationCanceledException>(() =>
fileSystemSourceProvider4.Load(tree.Root, Settings, cancellation.Token));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReadsSingleFile()
    {
        using var tree = new TempTree();
        var file = tree.Write("App/Service.cs", "class Service { }");
        tree.Write("App/App.csproj", "<Project />");

        var fileSystemSourceProvider5 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider5.Load(file, Settings, CancellationToken.None);

        var unit = Assert.Single(result.Units);
        Assert.Equal(file, unit.Path);
        Assert.Equal(ProjectIdentities.Named("App"), unit.Project);
        Assert.Equal(DiscoveryMode.FileSystem, result.Stats.Mode);
        Assert.Equal(1, result.Stats.Discovered);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReadsUtf16FilesWithoutCorruption()
    {
        using var tree = new TempTree();
        const string Text = "class Widget { public int Value { get; set; } }";
        tree.WriteBytes("Widget.cs", Encoding.Unicode.GetBytes(Text));

        var fileSystemSourceProvider6 = new FileSystemSourceProvider();
        var unit = Assert.Single(fileSystemSourceProvider6.Load(tree.Root, Settings, CancellationToken.None).Units);

        Assert.Equal(Text, unit.Text);
        Assert.DoesNotContain('\0', unit.Text);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsAnUnreadableFileWithoutAbortingTheScan()
    {
        using var tree = new TempTree();
        tree.Write("Readable.cs", "class Readable { }");
        var locked = tree.Write("Locked.cs", "class Locked { }");

        var fileStream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None);
        using (fileStream)
        {
            var fileSystemSourceProvider8 = new FileSystemSourceProvider();
            var result = fileSystemSourceProvider8.Load(tree.Root, Settings, CancellationToken.None);

            Assert.Single(result.Units);
            Assert.EndsWith("Readable.cs", result.Units[0].Path, StringComparison.Ordinal);

            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(SourceDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(locked, diagnostic.Path);
            Assert.Equal(1, result.Stats.Excluded);
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsMissingPathAsError()
    {
        var fileSystemSourceProvider7 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider7.Load(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Settings,
            CancellationToken.None);

        Assert.Empty(result.Units);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(SourceDiagnosticSeverity.Error, diagnostic.Severity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsParseFailuresWithoutDiscardingTheFile()
    {
        using var tree = new TempTree();
        tree.Write("Broken.cs", "class C { void M( }");

        var fileSystemSourceProvider9 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider9.Load(tree.Root, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(SourceDiagnosticSeverity.Warning, diagnostic.Severity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_SkipsBuildArtifacts()
    {
        using var tree = new TempTree();
        tree.Write("Service.cs", "class Service { }");
        tree.Write("obj/Debug/Generated.cs", "class Generated { }");
        tree.Write("bin/Release/Other.cs", "class Other { }");
        tree.Write("bindings/Real.cs", "class Real { }");

        var fileSystemSourceProvider10 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider10.Load(tree.Root, Settings, CancellationToken.None);

        Assert.Equal(2, result.Units.Count);
        Assert.Equal(2, result.Stats.Excluded);
        Assert.Contains(result.Units, unit => unit.Path.EndsWith("Real.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_SkipsGeneratedFiles()
    {
        using var tree = new TempTree();
        tree.Write("Service.cs", "class Service { }");
        tree.Write("Model.g.cs", "class Model { }");
        tree.Write("Header.cs", "// <auto-generated />\nclass Header { }");

        var fileSystemSourceProvider11 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider11.Load(tree.Root, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Equal(2, result.Stats.Excluded);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_SurvivesAnInaccessibleSubdirectory()
    {
        using var tree = new TempTree();
        tree.Write("Top.cs", "class Top { }");
        tree.Write("allowed/Allowed.cs", "class Allowed { }");

        tree.AddDirectory("denied");
        tree.Write("denied/Hidden.cs", "class Hidden { }");

        var fileSystemSourceProvider12 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider12.Load(tree.Root, Settings, CancellationToken.None);

        Assert.Contains(result.Units, unit => unit.Path.EndsWith("Top.cs", StringComparison.Ordinal));
        Assert.Contains(result.Units, unit => unit.Path.EndsWith("Allowed.cs", StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_WalksDirectoryAndResolvesProjects()
    {
        using var tree = new TempTree();
        tree.Write("App/App.csproj", "<Project />");
        tree.Write("App/Service.cs", "class Service { }");
        tree.Write("Lib/Lib.csproj", "<Project />");
        tree.Write("Lib/Helper.cs", "class Helper { }");

        var fileSystemSourceProvider13 = new FileSystemSourceProvider();
        var result = fileSystemSourceProvider13.Load(tree.Root, Settings, CancellationToken.None);

        Assert.Equal(2, result.Units.Count);
        Assert.Contains(result.Units, unit => unit.Project == ProjectIdentities.Named("App"));
        Assert.Contains(result.Units, unit => unit.Project == ProjectIdentities.Named("Lib"));
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Relative_ProducesForwardSlashPathsBelowTheRoot()
    {
        var root = Path.GetFullPath(Path.Combine("C:", "repo"));
        Assert.Equal("src/App/Service.cs", FileSystemSources.Relative(root, Path.Combine(root, "src", "App", "Service.cs")));
    }
}
