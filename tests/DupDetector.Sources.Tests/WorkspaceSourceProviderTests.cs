using DupDetector.Core.Model;
using Microsoft.Build.Locator;
using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
/// Registers MSBuild once per test process. Loading a solution needs a real SDK.
/// </summary>
public sealed class MsBuildFixture
{
    public MsBuildFixture()
    {
        if (!MSBuildLocator.IsRegistered && MSBuildLocator.QueryVisualStudioInstances().Any())
        {
            MSBuildLocator.RegisterDefaults();
        }

        IsAvailable = MSBuildLocator.IsRegistered;
    }

    public bool IsAvailable { get; }
}

[CollectionDefinition("msbuild")]
public sealed class MsBuildCollection : ICollectionFixture<MsBuildFixture>;

/// <summary>
/// A temporary solution with two projects, where App references Lib.
/// </summary>
public sealed class SolutionFixture : IDisposable
{
    private const string Duplicated = """
        namespace Sample;

        public class Calculator
        {
            public int Total(Order order)
            {
                var running = order.Price;
                var adjusted = running;
                var final = adjusted;
                return final;
            }
        }

        public class Order
        {
            public int Price { get; set; }
        }
        """;

    public SolutionFixture()
    {
        Root = Path.Combine(Path.GetTempPath(), "dupdetector-sln-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "App"));
        Directory.CreateDirectory(Path.Combine(Root, "Lib"));

        File.WriteAllText(
            Path.Combine(Root, "Lib", "Lib.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");

        File.WriteAllText(
            Path.Combine(Root, "App", "App.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>" +
            "<ItemGroup><ProjectReference Include=\"..\\Lib\\Lib.csproj\" /></ItemGroup></Project>");

        File.WriteAllText(Path.Combine(Root, "Lib", "LibCalculator.cs"), Duplicated);
        File.WriteAllText(Path.Combine(Root, "App", "AppCalculator.cs"), Duplicated);

        // App is listed first, so Lib is already in the workspace as a transitive reference by the
        // time it is reached. This is the arrangement that previously lost every Lib file.
        SlnxPath = Path.Combine(Root, "Sample.slnx");
        File.WriteAllText(
            SlnxPath,
            "<Solution>\n  <Project Path=\"App/App.csproj\" />\n  <Project Path=\"Lib/Lib.csproj\" />\n</Solution>");

        ProjectPath = Path.Combine(Root, "App", "App.csproj");

        // An existing file that is not a project at all: MSBuild rejects the extension outright.
        BrokenProjectPath = Path.Combine(Root, "Broken", "NotAProject.txt");
        Directory.CreateDirectory(Path.Combine(Root, "Broken"));
        File.WriteAllText(BrokenProjectPath, "not a project");

        SlnxWithBrokenPath = Path.Combine(Root, "Broken.slnx");
        File.WriteAllText(
            SlnxWithBrokenPath,
            "<Solution>\n  <Project Path=\"App/App.csproj\" />\n  <Project Path=\"Broken/NotAProject.txt\" />\n</Solution>");

        SlnPath = Path.Combine(Root, "Sample.sln");
        File.WriteAllText(
            SlnPath,
            """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Lib", "Lib\Lib.csproj", "{11111111-1111-1111-1111-111111111111}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "App", "App\App.csproj", "{22222222-2222-2222-2222-222222222222}"
            EndProject
            Global
            	GlobalSection(SolutionConfigurationPlatforms) = preSolution
            		Debug|Any CPU = Debug|Any CPU
            	EndGlobalSection
            EndGlobal
            """);
    }

    public string SlnPath { get; }

    public string SlnxWithBrokenPath { get; }

    public string BrokenProjectPath { get; }

    public string Root { get; }

    public string SlnxPath { get; }

    public string ProjectPath { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }
}

[Collection("msbuild")]
public class SlnxSourceProviderTests(MsBuildFixture msbuild)
{
    private static readonly DetectionSettings Settings = new() { MinLines = 1 };

    [Fact]
    public void Handles_RecognisesTheExtension()
    {
        Assert.True(SlnxSourceProvider.Handles("a/b/Sample.SLNX"));
        Assert.False(SlnxSourceProvider.Handles("a/b/Sample.sln"));
    }

    [Fact]
    public void Load_RejectsNullArguments()
    {
        var provider = new SlnxSourceProvider();
        Assert.Throws<ArgumentNullException>(() => provider.Load(null!, Settings));
        Assert.Throws<ArgumentNullException>(() => provider.Load("x.slnx", null!));
    }

    [Fact]
    public void Load_ReportsAMissingSolution()
    {
        var result = new SlnxSourceProvider().Load(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".slnx"),
            Settings);

        Assert.Equal(SourceDiagnosticSeverity.Error, Assert.Single(result.Diagnostics).Severity);
    }

    [Fact]
    public void ReadProjectPaths_WarnsAboutEachMissingProjectIndividually()
    {
        using var tree = new TempTree();
        var present = tree.Write("Kept/Kept.csproj", "<Project />");
        var slnx = tree.Write(
            "Sample.slnx",
            "<Solution><Project Path=\"Kept/Kept.csproj\" /><Project Path=\"Gone/Gone.csproj\" /></Solution>");

        var diagnostics = new List<SourceDiagnostic>();
        var paths = SlnxSourceProvider.ReadProjectPaths(slnx, tree.Root, diagnostics);

        Assert.Equal([present], paths);
        // The old behaviour warned only when every project was missing.
        Assert.Single(diagnostics);
        Assert.Contains("missing", diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadProjectPaths_ReportsMalformedXmlPlainly()
    {
        using var tree = new TempTree();
        var slnx = tree.Write("Broken.slnx", "<Solution><Project Path=");

        var diagnostics = new List<SourceDiagnostic>();

        Assert.Empty(SlnxSourceProvider.ReadProjectPaths(slnx, tree.Root, diagnostics));
        Assert.Contains("not valid XML", Assert.Single(diagnostics).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadProjectPaths_IgnoresBlankPathAttributes()
    {
        using var tree = new TempTree();
        var slnx = tree.Write("Sample.slnx", "<Solution><Project /><Project Path=\"  \" /></Solution>");

        Assert.Empty(SlnxSourceProvider.ReadProjectPaths(slnx, tree.Root, []));
    }

    [Fact]
    public void Load_ReportsWhenNoProjectIsLoadable()
    {
        using var tree = new TempTree();
        var slnx = tree.Write("Sample.slnx", "<Solution><Project Path=\"Gone/Gone.csproj\" /></Solution>");

        var result = new SlnxSourceProvider().Load(slnx, Settings);

        Assert.Empty(result.Units);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SourceDiagnosticSeverity.Error);
    }

    [Fact]
    public void ATransitivelyLoadedProjectStillContributesItsFiles()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var result = new SlnxSourceProvider().Load(solution.SlnxPath, Settings);
        var files = result.Units.Select(unit => Path.GetFileName(unit.Path)).OrderBy(name => name).ToArray();

        // Lib is reached as a reference of App. Both files must still be analysed.
        Assert.Contains("AppCalculator.cs", files);
        Assert.Contains("LibCalculator.cs", files);
        Assert.Equal(DiscoveryMode.Workspace, result.Stats.Mode);
    }

    [Fact]
    public void AProjectThatCannotBeOpenedIsReportedAndTheRestStillLoad()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var result = new SlnxSourceProvider().Load(solution.SlnxWithBrokenPath, Settings);

        // The healthy project still contributes its files.
        Assert.Contains(result.Units, unit => unit.Path.EndsWith("AppCalculator.cs", StringComparison.Ordinal));
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void TheSolutionPathFindsTheSameDuplicateAsADirectoryScan()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();
        var permissive = Settings with { MinFileSpread = 1, MinProjectSpread = 1 };

        var viaSolution = Core.Pipeline.AnalysisPipeline.Run(
            new SlnxSourceProvider().Load(solution.SlnxPath, permissive).Units,
            permissive,
            DiscoveryStats.Empty);

        var viaDirectory = Core.Pipeline.AnalysisPipeline.Run(
            new FileSystemSourceProvider().Load(solution.Root, permissive).Units,
            permissive,
            DiscoveryStats.Empty);

        Assert.NotEmpty(viaDirectory.Report.Clusters);
        Assert.Equal(viaDirectory.Report.Clusters.Count, viaSolution.Report.Clusters.Count);
    }
}

[Collection("msbuild")]
public class MsBuildSourceProviderTests(MsBuildFixture msbuild)
{
    private static readonly DetectionSettings Settings = new() { MinLines = 1 };

    [Fact]
    public void Handles_RecognisesSolutionAndProjectExtensions()
    {
        Assert.True(MsBuildSourceProvider.Handles("a/App.csproj"));
        Assert.True(MsBuildSourceProvider.Handles("a/App.SLN"));
        Assert.True(MsBuildSourceProvider.Handles("a/App.slnf"));
        Assert.False(MsBuildSourceProvider.Handles("a/App.cs"));
        Assert.Equal(3, MsBuildSourceProvider.Extensions.Count);
    }

    [Fact]
    public void Load_RejectsNullArguments()
    {
        var provider = new MsBuildSourceProvider();
        Assert.Throws<ArgumentNullException>(() => provider.Load(null!, Settings));
        Assert.Throws<ArgumentNullException>(() => provider.Load("x.csproj", null!));
    }

    [Fact]
    public void Load_ReportsAMissingProject()
    {
        var result = new MsBuildSourceProvider().Load(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csproj"),
            Settings);

        Assert.Equal(SourceDiagnosticSeverity.Error, Assert.Single(result.Diagnostics).Severity);
    }

    [Fact]
    public void Load_ReportsAnUnopenableProjectAsAnError()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var tree = new TempTree();
        var broken = tree.Write("Broken.csproj", "<Project><NotAValidElement");

        var result = new MsBuildSourceProvider().Load(broken, Settings);

        Assert.Empty(result.Units);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SourceDiagnosticSeverity.Error);
    }

    [Fact]
    public void Load_ReadsAProjectAndAssignsItsIdentity()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var result = new MsBuildSourceProvider().Load(solution.ProjectPath, Settings);

        Assert.Contains(result.Units, unit => unit.Path.EndsWith("AppCalculator.cs", StringComparison.Ordinal));
        Assert.All(result.Units, unit => Assert.True(unit.Project.IsKnown));
        Assert.Equal(DiscoveryMode.Workspace, result.Stats.Mode);
    }

    [Fact]
    public void Load_ReadsASolutionFile()
    {
        Assert.True(msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var result = new MsBuildSourceProvider().Load(solution.SlnPath, Settings);

        var names = result.Units.Select(unit => Path.GetFileName(unit.Path)).ToArray();
        Assert.Contains("AppCalculator.cs", names);
        Assert.Contains("LibCalculator.cs", names);
    }
}

public class SourceLoaderTests
{
    private static readonly DetectionSettings Settings = new() { MinLines = 1 };

    [Fact]
    public void Resolve_ChoosesByExtension()
    {
        Assert.IsType<SlnxSourceProvider>(SourceLoader.Resolve("a/App.slnx"));
        Assert.IsType<MsBuildSourceProvider>(SourceLoader.Resolve("a/App.csproj"));
        Assert.IsType<FileSystemSourceProvider>(SourceLoader.Resolve("a/src"));
        Assert.Throws<ArgumentNullException>(() => SourceLoader.Resolve(null!));
    }

    [Fact]
    public void Load_RejectsNullArguments()
    {
        var loader = new SourceLoader();
        Assert.Throws<ArgumentNullException>(() => loader.Load(null!, Settings));
        Assert.Throws<ArgumentNullException>(() => loader.Load([], null!));
    }

    [Fact]
    public void Load_ReportsNoDiscoveryModeForNoInput() =>
        Assert.Equal(DiscoveryMode.None, new SourceLoader().Load([], Settings).Stats.Mode);

    [Fact]
    public void Load_MergesSeveralPaths()
    {
        using var tree = new TempTree();
        tree.Write("a/One.cs", "class One { }");
        tree.Write("b/Two.cs", "class Two { }");

        var result = new SourceLoader().Load(
            [Path.Combine(tree.Root, "a"), Path.Combine(tree.Root, "b")],
            Settings);

        Assert.Equal(2, result.Units.Count);
        Assert.Equal(2, result.Stats.Discovered);
        Assert.Equal(DiscoveryMode.FileSystem, result.Stats.Mode);
    }

    [Fact]
    public void Load_ReportsMixedWhenInputsAreReachedDifferentWays()
    {
        using var tree = new TempTree();
        tree.Write("a/One.cs", "class One { }");

        var loader = new SourceLoader(path => new StubProvider(
            path.EndsWith("workspace", StringComparison.Ordinal) ? DiscoveryMode.Workspace : DiscoveryMode.FileSystem));

        Assert.Equal(DiscoveryMode.Mixed, loader.Load(["workspace", "files"], Settings).Stats.Mode);
    }

    [Fact]
    public void Load_HonoursCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new SourceLoader().Load(["anywhere"], Settings, cancellation.Token));
    }

    private sealed class StubProvider(DiscoveryMode mode) : ISourceProvider
    {
        public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default) =>
            new([], new DiscoveryStats(1, 0, mode), []);
    }
}
