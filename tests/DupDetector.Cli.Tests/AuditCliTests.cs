using System.Globalization;
using System.Text.Json;
using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
/// Covers the command-line surfaces added after the report audit.
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

    private readonly string _root = Directory.CreateTempSubdirectory("dupdetector-audit").FullName;

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

    private (ExitCode Code, RecordingSink Sink, RecordingLogger Logger) Run(params string[] extra)
    {
        var sink = new RecordingSink();
        var logger = new RecordingLogger();
        string[] args = [_root, "--min-file-spread", "2", "--min-project-spread", "1", .. extra];

        return (new CliHost(logger, sink).Run(args, "9.9.9"), sink, logger);
    }

    [Fact]
    public void SarifIsWrittenWhenRequested()
    {
        WriteCopies(3);

        var (code, sink, _) = Run("--format", "sarif");

        Assert.Equal(ExitCode.Success, code);
        using var document = JsonDocument.Parse(sink.Report.ToString());
        Assert.Equal("2.1.0", document.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public void ABaselineIsWrittenAndThenComparedAgainst()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var (writeCode, sink, _) = Run("--format", "json", "--write-baseline", baseline);
        Assert.Equal(ExitCode.Success, writeCode);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        var (unchanged, _, unchangedLog) = Run("--format", "json", "--baseline", baseline);
        Assert.Equal(ExitCode.Success, unchanged);
        Assert.True(unchangedLog.Contains(Microsoft.Extensions.Logging.LogLevel.Information, "Against baseline"));
    }

    [Fact]
    public void AGrownClusterFailsTheRunAndIsReportedAsSpreadRatherThanNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var (_, sink, _) = Run("--format", "json", "--write-baseline", baseline);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        WriteCopies(5);
        var (code, _, logger) = Run("--format", "json", "--baseline", baseline, "--fail-on-new");

        Assert.Equal(ExitCode.NewDuplication, code);
        Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "spread to 5 copies"));
        Assert.False(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "New duplication"));
    }

    [Fact]
    public void ABaselineRegressionIsReportedButDoesNotFailWithoutFailOnNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var (_, sink, _) = Run("--format", "json", "--write-baseline", baseline);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        WriteCopies(5);
        var (code, _, logger) = Run("--format", "json", "--baseline", baseline);

        Assert.Equal(ExitCode.Success, code);
        Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "spread to 5 copies"));
    }

    [Fact]
    public void ANewClusterFailsTheRunAndIsReportedAsNew()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var (_, sink, _) = Run("--format", "json", "--write-baseline", baseline);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        for (var index = 0; index < 2; index++)
        {
            File.WriteAllText(
                Path.Combine(_root, $"Other{index}.cs"),
                $"namespace O{index};\n\ninternal sealed class Other\n{{\n    public int Sum(int a)\n    {{\n        var t = a;\n        t -= 3;\n        t /= 2;\n        return t;\n    }}\n}}\n");
        }

        var (code, _, logger) = Run("--format", "json", "--baseline", baseline, "--fail-on-new");

        Assert.Equal(ExitCode.NewDuplication, code);
        Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Warning, "New duplication"));
    }

    [Fact]
    public void TheFailOnThresholdStillTakesPrecedenceOverABaselineComparison()
    {
        WriteCopies(3);
        var baseline = Path.Combine(_root, "baseline.json");

        var (_, sink, _) = Run("--format", "json", "--write-baseline", baseline);
        File.WriteAllText(baseline, sink.Saved[baseline]);

        var (code, _, logger) = Run("--format", "json", "--baseline", baseline, "--fail-on-new", "--fail-on", "1");

        Assert.Equal(ExitCode.ThresholdExceeded, code);
        Assert.True(logger.Contains(Microsoft.Extensions.Logging.LogLevel.Error, "--fail-on threshold"));
    }

    [Theory]
    [InlineData("types", DetectionKind.Types)]
    [InlineData("methods,types", DetectionKind.Methods | DetectionKind.Types)]
    public void TypesIsSelectableFromTheCommandLine(string value, DetectionKind expected)
    {
        var parsed = ArgumentParser.Parse([_root, "--detect", value], "9.9.9");

        Assert.Equal(expected, parsed.Options!.Settings.Kinds);
    }

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

    [Fact]
    public void FailOnNewDefaultsToOff()
    {
        Assert.False(ArgumentParser.Parse([_root], "9.9.9").Options!.FailOnNew);
        Assert.True(ArgumentParser.Parse([_root, "--fail-on-new"], "9.9.9").Options!.FailOnNew);
    }

    [Fact]
    public void TheGeneratedTimestampComesFromTheInjectedClock()
    {
        WriteCopies(3);
        var clock = new FakeTimeProvider(new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero));
        var sink = new RecordingSink();

        new CliHost(new RecordingLogger(), sink, loader: null, clock)
            .Run([_root, "--min-file-spread", "2", "--min-project-spread", "1", "--format", "json"], "9.9.9");

        using var document = JsonDocument.Parse(sink.Report.ToString());
        var metadata = document.RootElement.GetProperty("metadata");

        Assert.Equal("2024-05-06T07:08:09.0000000Z", metadata.GetProperty("generatedAtUtc").GetString());
        Assert.Equal("9.9.9", metadata.GetProperty("toolVersion").GetString());
        Assert.Equal("1.0", metadata.GetProperty("schemaVersion").GetString());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Directory.Delete(_root, recursive: true);
    }
}

/// <summary>A clock that never moves, so a generated timestamp is assertable.</summary>
internal sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
