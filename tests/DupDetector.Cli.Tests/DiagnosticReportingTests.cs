using DupDetector.Sources;

using Microsoft.Extensions.Logging;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Covers how loader diagnostics reach the log.
/// </summary>
public class DiagnosticReportingTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EverySeverityIsReportedAtItsOwnLevel()
    {
        var info = new SourceDiagnostic(SourceDiagnosticSeverity.Info, "just so you know", null);
        var warning = SourceDiagnostics.Warning("careful", null);
        var logger = DiagnosticRuns.With([info, warning]);

        Assert.True(logger.CanContains(LogLevel.Information, "just so you know"));
        Assert.True(logger.CanContains(LogLevel.Warning, "careful"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void PathIsAppendedOnlyWhenThereIsOne()
    {
        var withoutPath = SourceDiagnostics.Warning("no location", null);
        var withPath = SourceDiagnostics.Warning("with location", "/repo/a.cs");
        var logger = DiagnosticRuns.With([withoutPath, withPath]);

        Assert.Contains(logger.Entries, entry => entry.Message == "no location");
        Assert.Contains(logger.Entries, entry => entry.Message == "with location [/repo/a.cs]");
    }
}
