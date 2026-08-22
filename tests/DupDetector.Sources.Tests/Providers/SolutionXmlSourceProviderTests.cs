using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Core.Pipeline;

using DupDetector.Sources.Providers;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
[Collection("msbuild")]
public class SolutionXmlSourceProviderTests
{

    private static readonly DetectionSettings Settings;
    private readonly MicrosoftBuildFixture _msbuild;

    static SolutionXmlSourceProviderTests()
    {
        Settings = new()
        {
            MinLines = 1
        };
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="msbuild"></param>
    public SolutionXmlSourceProviderTests(MicrosoftBuildFixture msbuild)
    {
        _msbuild = msbuild;
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Handles_RecognisesTheExtension()
    {
        Assert.True(SolutionXmlSources.CanHandle("a/b/Sample.SLNX"));
        Assert.False(SolutionXmlSources.CanHandle("a/b/Sample.sln"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsMissingSolution()
    {
        using var tree = new TempTree();
        var solutionXmlSourceProvider3 = new SolutionXmlSourceProvider();
        var result = solutionXmlSourceProvider3.Load(
            tree.Missing("Absent.slnx"),
            Settings,
            CancellationToken.None);

        Assert.Equal(SourceDiagnosticSeverity.Error, Assert.Single(result.Diagnostics).Severity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsWhenNoProjectIsLoadable()
    {
        using var tree = new TempTree();
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project Path=\"Gone/Gone.csproj\" /></Solution>");

        var solutionXmlSourceProvider4 = new SolutionXmlSourceProvider();
        var result = solutionXmlSourceProvider4.Load(solutionXml, Settings, CancellationToken.None);

        Assert.Empty(result.Units);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SourceDiagnosticSeverity.Error);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ProjectThatCannotBeOpenedIsReportedAndTheRestStillLoad()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var solutionXmlSourceProvider = new SolutionXmlSourceProvider();
        var result = solutionXmlSourceProvider.Load(solution.SolutionXmlWithBrokenPath, Settings, CancellationToken.None);

        Assert.Contains(result.Units, unit => unit.Path.EndsWith("AppCalculator.cs", StringComparison.Ordinal));
        Assert.NotEmpty(result.Diagnostics);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ReadProjectPaths_IgnoresBlankPathAttributes()
    {
        using var tree = new TempTree();
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project /><Project Path=\"  \" /></Solution>");

        Assert.Empty(SolutionXmlSources.ReadProjectPaths(solutionXml, tree.Root, []));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ReadProjectPaths_ReportsMalformedXmlPlainly()
    {
        using var tree = new TempTree();
        var solutionXml = tree.Write("Broken.slnx", "<Solution><Project Path=");

        var diagnostics = new List<SourceDiagnostic>();

        Assert.Empty(SolutionXmlSources.ReadProjectPaths(solutionXml, tree.Root, diagnostics));
        Assert.Contains("not valid XML", Assert.Single(diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ReadProjectPaths_WarnsAboutEachMissingProjectIndividually()
    {
        using var tree = new TempTree();
        var present = tree.Write("Kept/Kept.csproj", "<Project />");
        var solutionXml = tree.Write(
            "Sample.slnx",
            "<Solution><Project Path=\"Kept/Kept.csproj\" /><Project Path=\"Gone/Gone.csproj\" /></Solution>");

        var diagnostics = new List<SourceDiagnostic>();
        var paths = SolutionXmlSources.ReadProjectPaths(solutionXml, tree.Root, diagnostics);

        Assert.Equal([present], paths);
        Assert.Single(diagnostics);
        Assert.Contains("missing", diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheSolutionPathFindsTheSameDuplicateAsDirectoryScan()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();
        var permissive = Settings with
        {
            MinFileSpread = 1,
            MinProjectSpread = 1
        };

        var solutionXmlSourceProvider5 = new SolutionXmlSourceProvider();
        var viaSolution = AnalysisPipeline.Run(
solutionXmlSourceProvider5.Load(solution.SolutionXmlPath, permissive, CancellationToken.None).Units,
            permissive,
            DiscoveryStats.Empty);

        var fileSystemSourceProvider = new FileSystemSourceProvider();
        var viaDirectory = AnalysisPipeline.Run(
fileSystemSourceProvider.Load(solution.Root, permissive, CancellationToken.None).Units,
            permissive,
            DiscoveryStats.Empty);

        Assert.NotEmpty(viaDirectory.Report.Clusters);
        Assert.Equal(viaDirectory.Report.Clusters.Count, viaSolution.Report.Clusters.Count);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TransitivelyLoadedProjectStillContributesItsFiles()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var solutionXmlSourceProvider2 = new SolutionXmlSourceProvider();
        var result = solutionXmlSourceProvider2.Load(solution.SolutionXmlPath, Settings, CancellationToken.None);
        var files = new List<string>(result.Units.Count);
        foreach (var unit in result.Units)
        {
            files.Add(Path.GetFileName(unit.Path));
        }

        files.Sort(StringComparer.Ordinal);

        Assert.Contains("AppCalculator.cs", files);
        Assert.Contains("LibCalculator.cs", files);
        Assert.Equal(DiscoveryMode.Workspace, result.Stats.Mode);
    }
}
