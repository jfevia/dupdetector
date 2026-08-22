using System.Text;
using DupDetector.Core.Model;
using DupDetector.Sources;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DupDetector.Cli.Tests;

public class DiagnosticReportingTests
{
    /// <summary>Returns whatever diagnostics the test supplies, without touching a disk.</summary>
    private sealed class DiagnosticProvider(params SourceDiagnostic[] diagnostics) : ISourceProvider
    {
        public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default) =>
            new([], new DiscoveryStats(0, 0, DiscoveryMode.FileSystem), diagnostics);
    }

    private static RecordingLogger Run(params SourceDiagnostic[] diagnostics)
    {
        var logger = new RecordingLogger();
        var loader = new SourceLoader(_ => new DiagnosticProvider(diagnostics));
        new CliHost(logger, new RecordingSink(), loader).Run(["./anywhere"], "9.9.9");
        return logger;
    }

    [Fact]
    public void EverySeverityIsReportedAtItsOwnLevel()
    {
        var logger = Run(
            new SourceDiagnostic(SourceDiagnosticSeverity.Info, "just so you know"),
            SourceDiagnostic.Warning("careful"));

        Assert.True(logger.Contains(LogLevel.Information, "just so you know"));
        Assert.True(logger.Contains(LogLevel.Warning, "careful"));
    }

    [Fact]
    public void APathIsAppendedOnlyWhenThereIsOne()
    {
        var logger = Run(
            SourceDiagnostic.Warning("no location"),
            SourceDiagnostic.Warning("with location", "/repo/a.cs"));

        Assert.Contains(logger.Entries, entry => entry.Message == "no location");
        Assert.Contains(logger.Entries, entry => entry.Message == "with location [/repo/a.cs]");
    }
}

public class ConsoleOutputSinkTests
{
    [Fact]
    public void WriteReport_GoesToStandardOutput()
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            new ConsoleOutputSink().WriteReport("report body");
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal("report body", captured.ToString());
    }

    [Fact]
    public void WriteMessage_GoesToStandardOutput()
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            new ConsoleOutputSink().WriteMessage("help text");
        }
        finally
        {
            Console.SetOut(original);
        }

        Assert.Equal("help text", captured.ToString());
    }

    [Fact]
    public void Save_WritesUtf8WithoutAByteOrderMark()
    {
        var path = Path.Combine(Path.GetTempPath(), "dupdetector-sink-" + Guid.NewGuid().ToString("N") + ".yaml");
        try
        {
            new ConsoleOutputSink().Save(path, "summary: ok");

            var bytes = File.ReadAllBytes(path);
            Assert.Equal("summary: ok", Encoding.UTF8.GetString(bytes));
            // A byte-order mark would confuse downstream parsers on Linux.
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_CreatesMissingDirectories()
    {
        var root = Path.Combine(Path.GetTempPath(), "dupdetector-sink-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "nested", "deeper", "report.yaml");
        try
        {
            new ConsoleOutputSink().Save(path, "summary: ok");

            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Save_AcceptsAPathWithNoDirectoryComponent()
    {
        var name = "dupdetector-sink-" + Guid.NewGuid().ToString("N") + ".yaml";
        var original = Directory.GetCurrentDirectory();
        var scratch = Path.Combine(Path.GetTempPath(), "dupdetector-cwd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);

        try
        {
            Directory.SetCurrentDirectory(scratch);
            new ConsoleOutputSink().Save(name, "summary: ok");

            Assert.True(File.Exists(Path.Combine(scratch, name)));
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(scratch, recursive: true);
        }
    }
}
