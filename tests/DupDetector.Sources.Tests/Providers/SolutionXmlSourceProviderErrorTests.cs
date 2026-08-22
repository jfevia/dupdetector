using DupDetector.Core.Model;

using DupDetector.Sources.Providers;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
public class SolutionXmlSourceProviderErrorTests
{
    private static readonly DetectionSettings Settings;

    static SolutionXmlSourceProviderErrorTests()
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
        using var tree = new TempTree();
        tree.Write("App/App.csproj", "<Project />");
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project Path=\"App/App.csproj\" /></Solution>");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var solutionXmlSourceProvider2 = new SolutionXmlSourceProvider(StubWorkspaceHosts.Create);
        Assert.Throws<OperationCanceledException>(() =>
solutionXmlSourceProvider2.Load(solutionXml, Settings, cancellation.Token));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OnlyDeclaredProjectsAreHarvested()
    {
        using var tree = new TempTree();
        var app = tree.Write("App/App.csproj", "<Project />");
        var other = tree.Write("Other/Other.csproj", "<Project />");
        var appSource = tree.Write("App/AppCalc.cs", "class AppCalc { }");
        var otherSource = tree.Write("Other/OtherCalc.cs", "class OtherCalc { }");
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project Path=\"App/App.csproj\" /></Solution>");

        var stubWorkspaceHost11 = new StubWorkspaceHost();
        var host = stubWorkspaceHost11.WithProject("App", app)
            .WithDocument(appSource, "class AppCalc { }")
            .WithProject("Other", other)
            .WithDocument(otherSource, "class OtherCalc { }");

        var solutionXmlSourceProvider3 = new SolutionXmlSourceProvider(() => host);
        var result = solutionXmlSourceProvider3.Load(solutionXml, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.EndsWith("AppCalc.cs", result.Units[0].Path, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ProjectLoadWarningsAreSurfaced()
    {
        using var tree = new TempTree();
        var app = tree.Write("App/App.csproj", "<Project />");
        var appSource = tree.Write("App/AppCalc.cs", "class AppCalc { }");
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project Path=\"App/App.csproj\" /></Solution>");

        var stubWorkspaceHost12 = new StubWorkspaceHost();
        var host = stubWorkspaceHost12.WithProject("App", app)
            .WithDocument(appSource, "class AppCalc { }");
        host.Recorded.Add(SourceDiagnostics.Warning("a reference was skipped", null));

        var solutionXmlSourceProvider4 = new SolutionXmlSourceProvider(() => host);
        var result = solutionXmlSourceProvider4.Load(solutionXml, Settings, CancellationToken.None);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "a reference was skipped");
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TransitivelyLoadedProjectIsStillHarvested()
    {
        using var tree = new TempTree();
        var app = tree.Write("App/App.csproj", "<Project />");
        var lib = tree.Write("Lib/Lib.csproj", "<Project />");
        var appSource = tree.Write("App/AppCalc.cs", "class AppCalc { }");
        var libSource = tree.Write("Lib/LibCalc.cs", "class LibCalc { }");
        var solutionXml = tree.Write("Sample.slnx", "<Solution><Project Path=\"App/App.csproj\" /><Project Path=\"Lib/Lib.csproj\" /></Solution>");

        var stubWorkspaceHost10 = new StubWorkspaceHost();
        var host = stubWorkspaceHost10.WithProject("App", app)
            .WithDocument(appSource, "class AppCalc { }")
            .WithProject("Lib", lib)
            .WithDocument(libSource, "class LibCalc { }");

        var solutionXmlSourceProvider = new SolutionXmlSourceProvider(() => host);
        var result = solutionXmlSourceProvider.Load(solutionXml, Settings, CancellationToken.None);

        Assert.Equal(2, result.Units.Count);
        Assert.Contains(result.Units, unit => unit.Path.EndsWith("LibCalc.cs", StringComparison.Ordinal));
    }
}
