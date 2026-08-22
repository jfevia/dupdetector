using System.Globalization;
using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.Reporting;
using DupDetector.Sources;
using Microsoft.Extensions.Logging;

namespace DupDetector.Cli;

/// <summary>
/// Where the program's output goes. Injected so a run can be observed without a console.
/// </summary>
public interface IOutputSink
{
    void WriteReport(string content);

    void WriteMessage(string message);

    void Save(string path, string content);
}

/// <summary>
/// Runs one invocation end to end.
/// </summary>
// The entry point delegates here immediately, so the whole program is reachable from a test.
public sealed class CliHost(ILogger logger, IOutputSink output, SourceLoader? loader = null, TimeProvider? time = null)
{
    private readonly SourceLoader _loader = loader ?? new SourceLoader();
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private string _version = "0.0.0";
    private IReadOnlyList<string> _args = [];

    public ExitCode Run(IReadOnlyList<string> args, string version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        _version = version;
        _args = args;
        var parsed = ArgumentParser.Parse(args, version);

        if (parsed.Message is { } message)
        {
            output.WriteMessage(message);
            return ExitCode.Success;
        }

        if (parsed.Error is { } error)
        {
            Log.Failure(logger, error);
            return ExitCode.UsageError;
        }

        return Execute(parsed.Options!, cancellationToken);
    }

    private ExitCode Execute(CommandLineOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var loaded = _loader.Load(options.InputPaths, options.Settings, cancellationToken);
            var failed = false;

            foreach (var diagnostic in loaded.Diagnostics)
            {
                failed |= diagnostic.Severity == SourceDiagnosticSeverity.Error;
                Report(diagnostic);
            }

            if (failed)
            {
                return ExitCode.RuntimeError;
            }

            Log.Analysing(logger, loaded.Units.Count, loaded.Stats.Excluded);

            var result = AnalysisPipeline.Run(loaded.Units, options.Settings, loaded.Stats, cancellationToken);
            foreach (var note in result.Notes)
            {
                Log.Warning(logger, note.Message);
            }

            Emit(options, result.Report);

            if (options.WriteBaselinePath is { } baselineOut)
            {
                output.Save(baselineOut, Baseline.From(result.Report, _time).ToJson());
                Log.ReportWritten(logger, baselineOut);
            }

            var regressed = CompareToBaseline(options, result.Report);

            var percentage = result.Report.Summary.DuplicationPercentage;
            if (options.FailOn is { } threshold && percentage >= threshold)
            {
                Log.ThresholdExceeded(logger, percentage, threshold);
                return ExitCode.ThresholdExceeded;
            }

            // Reported either way; only --fail-on-new turns a regression into a failure.
            return regressed && options.FailOnNew ? ExitCode.NewDuplication : ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            Log.Failure(logger, "Cancelled.");
            return ExitCode.RuntimeError;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // The whole exception, not only its message: an inner cause is usually the real story.
            Log.Crashed(logger, exception);
            return ExitCode.RuntimeError;
        }
    }

    /// <summary>
    /// Reports what changed against a baseline. Returns true when new or grown duplication appeared,
    /// which is the condition a gate should fail on even when the overall percentage improved.
    /// </summary>
    private bool CompareToBaseline(CommandLineOptions options, DetectionReport report)
    {
        if (options.BaselinePath is not { } path)
        {
            return false;
        }

        var delta = BaselineDelta.Between(Baseline.Parse(File.ReadAllText(path)), report);
        Log.BaselineCompared(logger, delta.Added.Count, delta.Grown.Count, delta.Removed.Count, delta.PercentagePointChange);

        foreach (var cluster in delta.Added)
        {
            Log.NewDuplication(
                logger,
                cluster.Id,
                cluster.Metrics.Occurrences,
                cluster.Metrics.RemovableLines,
                cluster.Instances[0].FilePath,
                cluster.Instances[0].Lines.Start);
        }

        foreach (var cluster in delta.Grown)
        {
            Log.SpreadingDuplication(logger, cluster.Id, cluster.Metrics.Occurrences);
        }

        return delta.IsRegression;
    }

    private void Emit(CommandLineOptions options, DetectionReport report)
    {
        var metadata = new MetadataDocument
        {
            ToolVersion = _version,
            GeneratedAtUtc = _time.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            TargetPath = string.Join(", ", options.InputPaths),
            Commit = Environment.GetEnvironmentVariable("GITHUB_SHA"),
            CommandLine = string.Join(' ', _args),
        };

        IReportWriter writer = options.Format switch
        {
            ReportFormat.Json => new JsonReportWriter(options.IncludeRawSnippets) { Metadata = metadata },
            ReportFormat.Html => new HtmlReportWriter { Metadata = metadata },
            ReportFormat.Sarif => new SarifReportWriter { Metadata = metadata },
            _ => new YamlReportWriter(options.IncludeRawSnippets) { Metadata = metadata },
        };

        var content = writer.Write(report);

        if (options.OutputPath is { } path)
        {
            output.Save(path, content);
            Log.ReportWritten(logger, path);
        }
        else
        {
            output.WriteReport(content);
        }
    }

    private void Report(SourceDiagnostic diagnostic)
    {
        var detail = diagnostic.Path is null ? diagnostic.Message : $"{diagnostic.Message} [{diagnostic.Path}]";

        Action<ILogger, string> write = diagnostic.Severity switch
        {
            SourceDiagnosticSeverity.Error => Log.Failure,
            SourceDiagnosticSeverity.Warning => Log.Warning,
            _ => Log.Info,
        };

        write(logger, detail);
    }
}
