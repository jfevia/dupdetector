using DupDetector.Core.Model;

using DupDetector.Sources.Providers;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
public class ProjectNameResolverTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FileSystemProbe_ReturnsNullForMissingDirectory()
    {
        using var tree = new TempTree();
        Assert.Null(FileSystemDirectoryProbe.Instance.FindProjectFile(tree.Missing("Absent")));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_FindsTheNearestProjectFile()
    {
        var root = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [root] = Path.Combine(root, "App.csproj"),
        };
        var probe = new StubDirectoryProbe(dictionary);

        var resolver = new ProjectNameResolver(probe);

        Assert.Equal(ProjectIdentities.Named("App"), resolver.Resolve(Path.Combine(root, "Service.cs")));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_ProbesEachDirectoryOnlyOnce()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var dictionary2 = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        };
        var probe = new StubDirectoryProbe(dictionary2);
        var resolver = new ProjectNameResolver(probe);

        for (var index = 0; index < 50; index++)
        {
            resolver.Resolve(Path.Combine(app, $"File{index}.cs"));
        }

        Assert.Equal(1, probe.Calls);
        Assert.Equal(1, resolver.CachedDirectoryCount);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_ReportsUnknownWhenNoProjectExists()
    {
        var probe = new StubDirectoryProbe([]);
        var resolver = new ProjectNameResolver(probe);

        Assert.Equal(ProjectIdentity.Unknown, resolver.Resolve(Path.GetFullPath(Path.Combine("C:", "loose", "File.cs"))));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_ReturnsUnknownForRootPath()
    {
        var stubProbe = new StubDirectoryProbe([]);
        var projectNameResolver = new ProjectNameResolver(stubProbe);
        Assert.Equal(ProjectIdentity.Unknown, projectNameResolver.Resolve(Path.GetPathRoot(Path.GetFullPath("."))!));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_ReusesAnAncestorResultForSiblingDirectories()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var dictionary3 = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        };
        var probe = new StubDirectoryProbe(dictionary3);
        var resolver = new ProjectNameResolver(probe);

        resolver.Resolve(Path.Combine(app, "A", "One.cs"));
        var before = probe.Calls;
        resolver.Resolve(Path.Combine(app, "B", "Two.cs"));

        Assert.Equal(before + 1, probe.Calls);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Resolve_WalksUpwardsUntilItFindsProject()
    {
        var app = Path.GetFullPath(Path.Combine("C:", "repo", "src", "App"));
        var nested = Path.Combine(app, "Domain", "Orders");
        var dictionary4 = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [app] = Path.Combine(app, "App.csproj"),
        };
        var probe = new StubDirectoryProbe(dictionary4);

        var projectNameResolver2 = new ProjectNameResolver(probe);
        Assert.Equal(ProjectIdentities.Named("App"), projectNameResolver2.Resolve(Path.Combine(nested, "Order.cs")));
    }
}
