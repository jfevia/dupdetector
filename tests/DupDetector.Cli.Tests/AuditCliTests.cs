using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using DupDetector.TestKit;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Covers the command-line surfaces added after the report audit.
/// </summary>
public sealed class AuditCliTests : IDisposable
{
    private const string Duplicated = """
        namespace N__INDEX__;

        internal sealed class Repeated
        {
            public int Compute(int a)
            {
                var total = a;
                total += 1;
                total *= 2;
                return total;
            }
        }
        """;
    private readonly string _root;

    /// <summary>
    ///     
    /// </summary>
    public AuditCliTests()
    {
        _root = Directory.CreateTempSubdirectory("dupdetector-audit").FullName;
    }

    /// <summary>
    ///     
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_root, recursive: true);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BaselineIsWrittenAndThenComparedAgainst()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var run = Run(["--format", "json", "--write-baseline", baseline]);
        var writeCode = run.Code;
        var sink = run.Sink;
        Assert.Equal(ExitCode.Success, writeCode);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        var run2 = Run(["--format", "json", "--baseline", baseline]);
        var unchanged = run2.Code;
        var unchangedLog = run2.Logger;
        Assert.Equal(ExitCode.Success, unchanged);
        Assert.True(unchangedLog.CanContains(LogLevel.Information, "Against baseline"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BaselineRegressionIsReportedButDoesNotFailWithoutFailOnNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var run = Run(["--format", "json", "--write-baseline", baseline]);
        var sink = run.Sink;
        File.WriteAllText(baseline, sink.Saved[baseline]);

        WriteCopies(5);
        var run2 = Run(["--format", "json", "--baseline", baseline]);
        var code = run2.Code;
        var logger = run2.Logger;

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.CanContains(LogLevel.Warning, "spread to 5 copies"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FailOnNewDefaultsToOff()
    {
        Assert.False(ArgumentParser.Parse([_root], "9.9.9").Options!.IsFailOnNew);
        Assert.True(ArgumentParser.Parse([_root, "--fail-on-new"], "9.9.9").Options!.IsFailOnNew);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void GrownClusterFailsTheRunAndIsReportedAsSpreadRatherThanNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var run = Run(["--format", "json", "--write-baseline", baseline]);
        var sink = run.Sink;
        File.WriteAllText(baseline, sink.Saved[baseline]);

        WriteCopies(5);
        var run2 = Run(["--format", "json", "--baseline", baseline, "--fail-on-new"]);
        var code = run2.Code;
        var logger = run2.Logger;

        Assert.Equal(ExitCode.NewDuplication, code);
        Assert.True(logger.CanContains(LogLevel.Warning, "spread to 5 copies"));
        Assert.False(logger.CanContains(LogLevel.Warning, "New duplication"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void HelpListsTheOptionsAddedAfterTheAudit()
    {
        var help = ArgumentParser.HelpText("9.9.9");

        Assert.Contains("--min-type-lines", help, StringComparison.Ordinal);
        Assert.Contains("--baseline", help, StringComparison.Ordinal);
        Assert.Contains("--write-baseline", help, StringComparison.Ordinal);
        Assert.Contains("--fail-on-new", help, StringComparison.Ordinal);
        Assert.Contains("sarif", help, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NewClusterFailsTheRunAndIsReportedAsNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var run = Run(["--format", "json", "--write-baseline", baseline]);
        var sink = run.Sink;
        File.WriteAllText(baseline, sink.Saved[baseline]);

        for (var index = 0; index < 2; index++)
        {
            File.WriteAllText(
                Path.Combine(_root, $"Other{index}.cs"),
                $"namespace O{index};\n\ninternal sealed class Other\n{{\n    public int Sum(int a)\n    {{\n        var t = a;\n        t -= 3;\n        t /= 2;\n        return t;\n    }}\n}}\n");
        }

        var run2 = Run(["--format", "json", "--baseline", baseline, "--fail-on-new"]);
        var code = run2.Code;
        var logger = run2.Logger;

        Assert.Equal(ExitCode.NewDuplication, code);
        Assert.True(logger.CanContains(LogLevel.Warning, "New duplication"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifIsWrittenWhenRequested()
    {
        WriteCopies(3);

        var run = Run(["--format", "sarif"]);
        var code = run.Code;
        var sink = run.Sink;

        Assert.Equal(ExitCode.Success, code);
        using var document = JsonDocument.Parse(sink.Report.ToString());
        Assert.Equal("2.1.0", document.RootElement.GetProperty("version").GetString());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheFailOnThresholdStillTakesPrecedenceOverBaselineComparison()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var run = Run(["--format", "json", "--write-baseline", baseline]);
        var sink = run.Sink;
        File.WriteAllText(baseline, sink.Saved[baseline]);

        var run2 = Run(["--format", "json", "--baseline", baseline, "--fail-on-new", "--fail-on", "1"]);
        var code = run2.Code;
        var logger = run2.Logger;

        Assert.Equal(ExitCode.ThresholdExceeded, code);
        Assert.True(logger.CanContains(LogLevel.Error, "--fail-on threshold"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheGeneratedTimestampComesFromTheInjectedClock()
    {
        WriteCopies(3);
        var dateTimeOffset = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero);
        var clock = new FixedTimeProvider(dateTimeOffset);
        var sink = new RecordingSink();

        var recordingLogger = new RecordingLogger();
        var cliHost = new CliHost(recordingLogger, sink, loader: null, clock);
        cliHost.Run([_root, "--min-file-spread", "2", "--min-project-spread", "1", "--format", "json"], "9.9.9", CancellationToken.None);

        using var document = JsonDocument.Parse(sink.Report.ToString());
        var metadata = document.RootElement.GetProperty("metadata");

        Assert.Equal("2024-05-06T07:08:09.0000000Z", metadata.GetProperty("generatedAtUtc").GetString());
        Assert.Equal("9.9.9", metadata.GetProperty("toolVersion").GetString());
        Assert.Equal("1.0", metadata.GetProperty("schemaVersion").GetString());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheTypeMinimumIsSettableAndDefaulted()
    {
        Assert.Equal(
            12,
            ArgumentParser.Parse([_root, "--min-type-lines", "12"], "9.9.9").Options!.Settings.MinTypeLines);

        Assert.Equal(
            DetectionSettings.Default.MinTypeLines,
            ArgumentParser.Parse([_root], "9.9.9").Options!.Settings.MinTypeLines);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("types", DetectionKind.Types)]
    [InlineData("methods,types", DetectionKind.Methods | DetectionKind.Types)]
    public void TypesIsSelectableFromTheCommandLine(string value, DetectionKind expected)
    {
        var parsed = ArgumentParser.Parse([_root, "--detect", value], "9.9.9");

        Assert.Equal(expected, parsed.Options!.Settings.Kinds);
    }

    private CliRun Run(IReadOnlyList<string> extra)
    {
        var sink = new RecordingSink();
        var logger = new RecordingLogger();
        string[] args = [_root, "--min-file-spread", "2", "--min-project-spread", "1", .. extra];

        var cliHost2 = new CliHost(logger, sink);
        var code = cliHost2.Run(args, "9.9.9", CancellationToken.None);
        var run = new CliRun(code, sink, logger);
        return run;
    }

    private void WriteCopies(int count)
    {
        foreach (var path in Directory.GetFiles(_root, "*.cs"))
        {
            File.Delete(path);
        }

        for (var index = 0; index < count; index++)
        {
            File.WriteAllText(
                Path.Combine(_root, $"File{index}.cs"),
                Duplicated.Replace("__INDEX__", index.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
        }
    }
}
