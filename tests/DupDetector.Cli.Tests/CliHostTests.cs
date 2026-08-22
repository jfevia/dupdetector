using System.Text.Json;
using DupDetector.Cli.CommandLine;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DupDetector.Cli.Tests;

public class CliHostTests
{
    private static (ExitCode Code, RecordingSink Sink, RecordingLogger Logger) Run(params string[] args)
    {
        var sink = new RecordingSink();
        var logger = new RecordingLogger();
        var code = new CliHost(logger, sink).Run(args, "9.9.9");
        return (code, sink, logger);
    }

    [Fact]
    public void Run_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new CliHost(new RecordingLogger(), new RecordingSink()).Run(null!, "1"));

    [Fact]
    public void Help_PrintsAndSucceeds()
    {
        var (code, sink, _) = Run("--help");

        Assert.Equal(ExitCode.Success, code);
        Assert.Contains("Usage: dupdetector", sink.Messages.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Version_PrintsAndSucceeds()
    {
        var (code, sink, _) = Run("--version");

        Assert.Equal(ExitCode.Success, code);
        Assert.Equal("9.9.9", sink.Messages.ToString());
    }

    [Fact]
    public void ATypoExitsNonZeroInsteadOfRunningWithDefaults()
    {
        var (code, sink, logger) = Run("./src", "--min-lnes", "10");

        Assert.Equal(ExitCode.UsageError, code);
        Assert.Empty(sink.Report.ToString());
        Assert.True(logger.Contains(LogLevel.Error, "Unknown option"));
    }

    [Fact]
    public void NoArguments_ExitsWithAUsageError() =>
        Assert.Equal(ExitCode.UsageError, Run().Code);

    [Fact]
    public void AMissingPath_IsARuntimeError()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var (code, _, logger) = Run(missing);

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.Contains(LogLevel.Error, "does not exist"));
    }

    [Fact]
    public void ADirectoryScan_ReportsDuplicationOnStandardOutput()
    {
        using var workspace = new Workspace();

        var (code, sink, _) = Run(workspace.Root, "--min-project-spread", "1", "--format", "json");

        Assert.Equal(ExitCode.Success, code);

        using var document = JsonDocument.Parse(sink.Report.ToString());
        Assert.Equal(1, document.RootElement.GetProperty("clusters").GetArrayLength());
        Assert.True(document.RootElement.GetProperty("summary").GetProperty("duplicationPercentage").GetDouble() > 0);
    }

    [Fact]
    public void EachFormatIsRendered()
    {
        using var workspace = new Workspace();

        Assert.Contains("summary:", Run(workspace.Root, "--format", "yaml").Sink.Report.ToString(), StringComparison.Ordinal);
        Assert.StartsWith("{", Run(workspace.Root, "--format", "json").Sink.Report.ToString().TrimStart(), StringComparison.Ordinal);
        Assert.StartsWith("<!DOCTYPE html>", Run(workspace.Root, "--format", "html").Sink.Report.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void RawSnippetsAreIncludedByDefaultAndCanBeSuppressed()
    {
        using var workspace = new Workspace();

        Assert.Contains("rawSnippets", Run(workspace.Root, "--min-project-spread", "1").Sink.Report.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(
            "rawSnippets",
            Run(workspace.Root, "--min-project-spread", "1", "--no-raw-snippets").Sink.Report.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void OutputPath_WritesToTheSinkInsteadOfStandardOutput()
    {
        using var workspace = new Workspace();

        var (code, sink, logger) = Run(workspace.Root, "--output", "report.yaml", "--verbose");

        Assert.Equal(ExitCode.Success, code);
        Assert.Empty(sink.Report.ToString());
        Assert.Contains("report.yaml", sink.Saved.Keys);
        Assert.True(logger.Contains(LogLevel.Information, "Report written"));
    }

    [Fact]
    public void FailOn_ExitsWithADedicatedCodeWhenBreached()
    {
        using var workspace = new Workspace();

        var (code, sink, logger) = Run(workspace.Root, "--min-project-spread", "1", "--fail-on", "1");

        Assert.Equal(ExitCode.ThresholdExceeded, code);
        // The report is still produced, so a failing gate remains diagnosable.
        Assert.NotEmpty(sink.Report.ToString());
        Assert.True(logger.Contains(LogLevel.Error, "fail-on"));
    }

    [Fact]
    public void FailOn_SucceedsWhenDuplicationIsBelowTheThreshold()
    {
        using var workspace = new Workspace();

        Assert.Equal(ExitCode.Success, Run(workspace.Root, "--min-project-spread", "1", "--fail-on", "99").Code);
    }

    [Fact]
    public void ExcludeFilters_AreApplied()
    {
        using var workspace = new Workspace();

        var withoutFilter = Run(workspace.Root, "--min-project-spread", "1").Sink.Report.ToString();
        var withFilter = Run(workspace.Root, "--min-project-spread", "1", "--exclude", "Lib/**").Sink.Report.ToString();

        Assert.Contains("dup-", withoutFilter, StringComparison.Ordinal);
        Assert.DoesNotContain("dup-", withFilter, StringComparison.Ordinal);
    }

    [Fact]
    public void Verbose_ReportsProgress()
    {
        using var workspace = new Workspace();

        var (_, _, logger) = Run(workspace.Root, "--verbose");

        Assert.True(logger.Contains(LogLevel.Information, "Analysing"));
    }

    [Fact]
    public void ParseWarningsAreSurfaced()
    {
        using var workspace = new Workspace();
        File.WriteAllText(Path.Combine(workspace.Root, "Broken.cs"), "class C { void M( }");

        var (code, _, logger) = Run(workspace.Root);

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.Contains(LogLevel.Warning, "parse error"));
    }

    [Fact]
    public void PipelineNotesAreSurfaced()
    {
        using var loose = new TempDirectory();
        File.WriteAllText(Path.Combine(loose.Root, "One.cs"), Sample);
        File.WriteAllText(Path.Combine(loose.Root, "Two.cs"), Sample);

        // No project file anywhere, so the project-spread minimum cannot be evaluated.
        var (code, _, logger) = Run(loose.Root, "--min-project-spread", "2");

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.Contains(LogLevel.Warning, "min-project-spread"));
    }

    [Fact]
    public void Cancellation_IsReportedAsARuntimeError()
    {
        using var workspace = new Workspace();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var logger = new RecordingLogger();
        var code = new CliHost(logger, new RecordingSink()).Run([workspace.Root], "9.9.9", cancellation.Token);

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.Contains(LogLevel.Error, "Cancelled"));
    }

    [Fact]
    public void AnUnexpectedFailureIsReportedWithItsFullDetail()
    {
        var logger = new RecordingLogger();
        var loader = new Sources.SourceLoader(_ => new ThrowingProvider());

        var code = new CliHost(logger, new RecordingSink(), loader).Run(["./anywhere"], "9.9.9");

        Assert.Equal(ExitCode.RuntimeError, code);
        Assert.True(logger.Contains(LogLevel.Error, "Analysis failed"));
    }

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

    private sealed class ThrowingProvider : Sources.ISourceProvider
    {
        public Sources.SourceLoadResult Load(
            string path,
            Core.Model.DetectionSettings settings,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("the disk melted");
    }

    private sealed class TempDirectory : IDisposable
    {
        internal TempDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "dupdetector-loose-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        internal string Root { get; }

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
}
