using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

using DupDetector.Sources.Tests.Providers;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     
/// </summary>
public class SourceLoaderTests
{
    private static readonly DetectionSettings Settings;

    static SourceLoaderTests()
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
    public void Load_HonoursCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var sourceLoader = new SourceLoader();
        Assert.Throws<OperationCanceledException>(() =>
sourceLoader.Load(["anywhere"], Settings, cancellation.Token));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_MergesSeveralPaths()
    {
        using var tree = new TempTree();
        tree.Write("a/One.cs", "class One { }");
        tree.Write("b/Two.cs", "class Two { }");

        var sourceLoader2 = new SourceLoader();
        var result = sourceLoader2.Load(
            [Path.Combine(tree.Root, "a"), Path.Combine(tree.Root, "b")],
            Settings,
            CancellationToken.None);

        Assert.Equal(2, result.Units.Count);
        Assert.Equal(2, result.Stats.Discovered);
        Assert.Equal(DiscoveryMode.FileSystem, result.Stats.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsMixedWhenInputsAreReachedDifferentWays()
    {
        using var tree = new TempTree();
        tree.Write("a/One.cs", "class One { }");

        var loader = new SourceLoader(StubSourceProviders.ByPath);

        Assert.Equal(DiscoveryMode.Mixed, loader.Load(["workspace", "files"], Settings, CancellationToken.None).Stats.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsNoDiscoveryModeForNoInput()
    {
        var sourceLoader3 = new SourceLoader();
        Assert.Equal(DiscoveryMode.None, sourceLoader3.Load([], Settings, CancellationToken.None).Stats.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_ChoosesByExtension()
    {
        Assert.IsType<SolutionXmlSourceProvider>(SourceLoaders.Resolve("a/App.slnx"));
        Assert.IsType<MicrosoftBuildSourceProvider>(SourceLoaders.Resolve("a/App.csproj"));
        Assert.IsType<FileSystemSourceProvider>(SourceLoaders.Resolve("a/src"));
    }
}
