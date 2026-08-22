using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using DupDetector.Sources;
using DupDetector.Sources.Providers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     
/// </summary>
public class CliHostTests
{

    private const string Sample = """
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
        """;

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnUnexpectedFailureIsReportedWithItsFullDetail()
    {
        var logger = new RecordingLogger();
        var throwingProvider = new ThrowingProvider();
        var loader = new SourceLoader(_ => throwingProvider);

        var recordingSink = new RecordingSink();
        var cliHost = new CliHost(logger, recordingSink, loader, null);
        var code = cliHost.Run(["./anywhere"], "9.9.9", CancellationToken.None);

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.CanContains(LogLevel.Error, "Analysis failed"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Cancellation_IsReportedAsRuntimeError()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var logger = new RecordingLogger();
        var recordingSink2 = new RecordingSink();
        var cliHost2 = new CliHost(logger, recordingSink2);
        var code = cliHost2.Run([workspace.Root], "9.9.9", cancellation.Token);

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.CanContains(LogLevel.Error, "Cancelled"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DirectoryScan_ReportsDuplicationOnStandardOutput()
    {
        using var workspace = new Workspace();

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--format", "json"]);
        var code = run.Code;
        var sink = run.Sink;

        Assert.Equal(ExitCode.Success, code);

        using var document = JsonDocument.Parse(sink.Report.ToString());
        Assert.Equal(1, document.RootElement.GetProperty("clusters").GetArrayLength());
        Assert.True(document.RootElement.GetProperty("summary").GetProperty("duplicationPercentage").GetDouble() > 0);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EachFormatIsRendered()
    {
        using var workspace = new Workspace();

        Assert.Contains("summary:", CliRunner.Run([workspace.Root, "--format", "yaml"]).Sink.Report.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("{", CliRunner.Run([workspace.Root, "--format", "json"]).Sink.Report.ToString().TrimStart(), StringComparison.Ordinal);
        Assert.StartsWith("<!DOCTYPE markup>", CliRunner.Run([workspace.Root, "--format", "markup"]).Sink.Report.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExcludeFilters_AreApplied()
    {
        using var workspace = new Workspace();

        var withoutFilter = CliRunner.Run([workspace.Root, "--min-project-spread", "1"]).Sink.Report.ToString();
        var withFilter = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--exclude", "Lib/**"]).Sink.Report.ToString();

        Assert.Contains("dup-", withoutFilter, StringComparison.Ordinal);
        Assert.DoesNotContain("dup-", withFilter, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FailOn_ExitsWithDedicatedCodeWhenBreached()
    {
        using var workspace = new Workspace();

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--fail-on", "1"]);
        var code = run.Code;
        var sink = run.Sink;
        var logger = run.Logger;

        Assert.Equal(ExitCode.ThresholdExceeded, code);
        Assert.NotEmpty(sink.Report.ToString());
        Assert.True(logger.CanContains(LogLevel.Error, "fail-on"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FailOn_SucceedsWhenDuplicationIsBelowTheThreshold()
    {
        using var workspace = new Workspace();

        Assert.Equal(ExitCode.Success, CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--fail-on", "99"]).Code);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Help_PrintsAndSucceeds()
    {
        var run = CliRunner.Run(["--help"]);
        var code = run.Code;
        var sink = run.Sink;

        Assert.Equal(ExitCode.Success, code);
        Assert.Contains("Usage: dupdetector", sink.Messages.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MissingPath_IsRuntimeError()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var run = CliRunner.Run([missing]);
        var code = run.Code;
        var logger = run.Logger;

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.CanContains(LogLevel.Error, "does not exist"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NoArguments_ExitsWithUsageError()
    {
        Assert.Equal(ExitCode.UsageError, CliRunner.Run([]).Code);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OutputPath_WritesToTheSinkInsteadOfStandardOutput()
    {
        using var workspace = new Workspace();

        var run = CliRunner.Run([workspace.Root, "--output", "report.yaml", "--verbose"]);
        var code = run.Code;
        var sink = run.Sink;
        var logger = run.Logger;

        Assert.Equal(ExitCode.Success, code);
        Assert.Empty(sink.Report.ToString());
        Assert.Contains("report.yaml", sink.Saved.Keys);
        Assert.True(logger.CanContains(LogLevel.Information, "Report written"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ParseWarningsAreSurfaced()
    {
        using var workspace = new Workspace();
        File.WriteAllText(Path.Combine(workspace.Root, "Broken.cs"), "class C { void M( }");

        var run = CliRunner.Run([workspace.Root]);
        var code = run.Code;
        var logger = run.Logger;

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.CanContains(LogLevel.Warning, "parse error"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void PipelineNotesAreSurfaced()
    {
        using var loose = new TempDirectory();
        File.WriteAllText(Path.Combine(loose.Root, "One.cs"), Sample);
        File.WriteAllText(Path.Combine(loose.Root, "Two.cs"), Sample);

        var run = CliRunner.Run([loose.Root, "--min-project-spread", "2"]);
        var code = run.Code;
        var logger = run.Logger;

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.CanContains(LogLevel.Warning, "min-project-spread"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void RawSnippetsAreIncludedByDefaultAndCanBeSuppressed()
    {
        using var workspace = new Workspace();

        Assert.Contains("rawSnippets", CliRunner.Run([workspace.Root, "--min-project-spread", "1"]).Sink.Report.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            "rawSnippets",
            CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--no-raw-snippets"]).Sink.Report.ToString(),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TypoExitsNonZeroInsteadOfRunningWithDefaults()
    {
        var run = CliRunner.Run(["./src", "--min-lnes", "10"]);
        var code = run.Code;
        var sink = run.Sink;
        var logger = run.Logger;

        Assert.Equal(ExitCode.UsageError, code);
        Assert.Empty(sink.Report.ToString());
        Assert.True(logger.CanContains(LogLevel.Error, "Unknown option"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Verbose_ReportsProgress()
    {
        using var workspace = new Workspace();

        var run = CliRunner.Run([workspace.Root, "--verbose"]);
        var logger = run.Logger;

        Assert.True(logger.CanContains(LogLevel.Information, "Analysing"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Version_PrintsAndSucceeds()
    {
        var run = CliRunner.Run(["--version"]);
        var code = run.Code;
        var sink = run.Sink;

        Assert.Equal(ExitCode.Success, code);
        Assert.Equal("9.9.9", sink.Messages.ToString());
    }

    private sealed class TempDirectory : IDisposable
    {

        public string Root { get; }

        public TempDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "dupdetector-loose-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            _ = CanTryDelete();
        }

        private bool CanTryDelete()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }

    private sealed class ThrowingProvider : ISourceProvider
    {
        public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken)
        {
            var failure = new InvalidOperationException("the disk melted");
            throw failure;
        }
    }
}
