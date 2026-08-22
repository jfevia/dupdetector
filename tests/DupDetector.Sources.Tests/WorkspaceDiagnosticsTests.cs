using DupDetector.Sources.Workspaces;

using Microsoft.CodeAnalysis;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     
/// </summary>
public class WorkspaceDiagnosticsTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Describe_DropsTransitiveDuplicateNotices()
    {
        var workspaceDiagnostic = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Warning, "Project X is already part of the workspace.");
        Assert.Null(WorkspaceDiagnostics.Describe(
workspaceDiagnostic));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Describe_MapsFailureToError()
    {
        var workspaceDiagnostic2 = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, "boom");
        var described = WorkspaceDiagnostics.Describe(workspaceDiagnostic2);

        Assert.NotNull(described);
        Assert.Equal(SourceDiagnosticSeverity.Error, described.Severity);
        Assert.Equal("boom", described.Message);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Describe_MapsWarningToWarning()
    {
        var workspaceDiagnostic3 = new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Warning, "careful");
        var described = WorkspaceDiagnostics.Describe(workspaceDiagnostic3);

        Assert.NotNull(described);
        Assert.Equal(SourceDiagnosticSeverity.Warning, described.Severity);
    }
}
