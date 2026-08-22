using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
[Collection("msbuild")]
public class MicrosoftBuildSourceProviderTests
{

    private static readonly DetectionSettings Settings;
    private readonly MicrosoftBuildFixture _msbuild;

    static MicrosoftBuildSourceProviderTests()
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
    public MicrosoftBuildSourceProviderTests(MicrosoftBuildFixture msbuild)
    {
        _msbuild = msbuild;
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Handles_RecognisesSolutionAndProjectExtensions()
    {
        Assert.True(MicrosoftBuildSources.CanHandle("a/App.csproj"));
        Assert.True(MicrosoftBuildSources.CanHandle("a/App.SLN"));
        Assert.True(MicrosoftBuildSources.CanHandle("a/App.slnf"));
        Assert.False(MicrosoftBuildSources.CanHandle("a/App.cs"));
        Assert.Equal(3, MicrosoftBuildSources.Extensions.Count);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReadsProjectAndAssignsItsIdentity()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var microsoftBuildSourceProvider = new MicrosoftBuildSourceProvider();
        var result = microsoftBuildSourceProvider.Load(solution.ProjectPath, Settings, CancellationToken.None);

        Assert.Contains(result.Units, unit => unit.Path.EndsWith("AppCalculator.cs", StringComparison.Ordinal));
        Assert.All(result.Units, unit => Assert.True(unit.Project.IsKnown));
        Assert.Equal(DiscoveryMode.Workspace, result.Stats.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReadsSolutionFile()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var solution = new SolutionFixture();

        var microsoftBuildSourceProvider2 = new MicrosoftBuildSourceProvider();
        var result = microsoftBuildSourceProvider2.Load(solution.SolutionPath, Settings, CancellationToken.None);

        var names = new List<string>(result.Units.Count);
        foreach (var unit in result.Units)
        {
            names.Add(Path.GetFileName(unit.Path));
        }
        Assert.Contains("AppCalculator.cs", names);
        Assert.Contains("LibCalculator.cs", names);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsAnUnopenableProjectAsAnError()
    {
        Assert.True(_msbuild.IsAvailable, "MSBuild must be available; this test exercises real workspace loading.");

        using var tree = new TempTree();
        var broken = tree.Write("Broken.csproj", "<Project><NotAValidElement");

        var microsoftBuildSourceProvider4 = new MicrosoftBuildSourceProvider();
        var result = microsoftBuildSourceProvider4.Load(broken, Settings, CancellationToken.None);

        Assert.Empty(result.Units);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SourceDiagnosticSeverity.Error);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsMissingProject()
    {
        using var tree = new TempTree();
        var microsoftBuildSourceProvider3 = new MicrosoftBuildSourceProvider();
        var result = microsoftBuildSourceProvider3.Load(
            tree.Missing("Absent.csproj"),
            Settings,
            CancellationToken.None);

        Assert.Equal(SourceDiagnosticSeverity.Error, Assert.Single(result.Diagnostics).Severity);
    }
}
