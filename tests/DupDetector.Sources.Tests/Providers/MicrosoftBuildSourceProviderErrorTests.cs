using DupDetector.Core.Model;

using DupDetector.Sources.Providers;

using Xunit;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     
/// </summary>
public class MicrosoftBuildSourceProviderErrorTests
{
    private static readonly DetectionSettings Settings;

    static MicrosoftBuildSourceProviderErrorTests()
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
    public void Load_DoesNotAddAnEmptyWorkspaceErrorWhenSomethingElseAlreadyExplainedIt()
    {
        using var tree = new TempTree();
        var project = tree.Write("App.csproj", "<Project />");
        var host = new StubWorkspaceHost();
        host.Recorded.Add(SourceDiagnostics.Error("restore failed", null));

        var microsoftBuildSourceProvider = new MicrosoftBuildSourceProvider(() => host);
        var result = microsoftBuildSourceProvider.Load(project, Settings, CancellationToken.None);

        Assert.Equal("restore failed", Assert.Single(result.Diagnostics).Message);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReportsAnUnopenableInputAsAnError()
    {
        using var tree = new TempTree();
        var project = tree.Write("App.csproj", "<Project />");
        var invalidOperationException = new InvalidOperationException("no SDK");
        var host = new StubWorkspaceHost(invalidOperationException);

        var microsoftBuildSourceProvider2 = new MicrosoftBuildSourceProvider(() => host);
        var result = microsoftBuildSourceProvider2.Load(project, Settings, CancellationToken.None);

        Assert.Empty(result.Units);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(SourceDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("no SDK", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_ReturnsUnitsWhenTheWorkspaceHasProjects()
    {
        using var tree = new TempTree();
        var project = tree.Write("App.csproj", "<Project />");
        var source = tree.Write("One.cs", "class One { }");
        var stubWorkspaceHost9 = new StubWorkspaceHost();
        var host = stubWorkspaceHost9.WithProject("App", project)
            .WithDocument(source, "class One { }");

        var microsoftBuildSourceProvider3 = new MicrosoftBuildSourceProvider(() => host);
        var result = microsoftBuildSourceProvider3.Load(project, Settings, CancellationToken.None);

        Assert.Single(result.Units);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Load_TreatsEmptyWorkspaceAsErrorRatherThanCleanSolution()
    {
        using var tree = new TempTree();
        var project = tree.Write("App.csproj", "<Project />");
        var host = new StubWorkspaceHost();

        var microsoftBuildSourceProvider4 = new MicrosoftBuildSourceProvider(() => host);
        var result = microsoftBuildSourceProvider4.Load(project, Settings, CancellationToken.None);

        Assert.Empty(result.Units);
        Assert.Contains("produced no source files", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }
}
