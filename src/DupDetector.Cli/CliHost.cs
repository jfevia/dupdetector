using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model.Reporting;
using DupDetector.Core.Pipeline;
using DupDetector.Reporting;
using DupDetector.Reporting.Documents;
using DupDetector.Reporting.Sarif;

using DupDetector.Sources;

using Microsoft.Extensions.Logging;

using System.Globalization;

namespace DupDetector.Cli;

/// <summary>
///     Runs one invocation end to end.
/// </summary>
public sealed class CliHost
{
    private readonly SourceLoader _loader;
    private readonly ILogger _logger;
    private readonly IOutputSink _output;
    private readonly TimeProvider _time;
    private IReadOnlyList<string> _args;
    private string _version;

    /// <summary>
    ///     
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="output"></param>
    public CliHost(ILogger logger, IOutputSink output)
        : this(logger, output, null, null)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="output"></param>
    /// <param name="loader"></param>
    /// <param name="time"></param>
    public CliHost(ILogger logger, IOutputSink output, SourceLoader? loader, TimeProvider? time)
    {
        _logger = logger;
        _output = output;
        var fallbackLoader = new SourceLoader();
        _loader = loader ?? fallbackLoader;
        _time = time ?? TimeProvider.System;
        _args = [];
        _version = "0.0.0";
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    public ExitCode Run(IReadOnlyList<string> args, string version, CancellationToken cancellationToken)
    {

        _version = version;
        _args = args;
        var parsed = ArgumentParser.Parse(args, version);

        if (parsed.Message is { } message)
        {
            _output.WriteMessage(message);
            return ExitCode.Success;
        }

        if (parsed.Error is { } error)
        {
            Log.Failure(_logger, error);
            return ExitCode.UsageError;
        }

        return Execute(parsed.Options!, cancellationToken);
    }

    /// <summary>
    ///     True when duplication is new or has grown since the baseline.
    /// </summary>
    private bool CanCompareToBaseline(CommandLineOptions options, DetectionReport report)
    {
        if (options.BaselinePath is not { } path)
        {
            return false;
        }

        var delta = BaselineDeltas.Between(Baselines.Parse(File.ReadAllText(path)), report);
        Log.BaselineCompared(_logger, delta.Added.Count, delta.Grown.Count, delta.Removed.Count, delta.PercentagePointChange);

        foreach (var cluster in delta.Added)
        {
            Log.NewDuplication(
                _logger,
                cluster.Id,
                cluster.Metrics.Occurrences,
                cluster.Metrics.RemovableLines,
                cluster.Instances[0].FilePath,
                cluster.Instances[0].Lines.Start);
        }

        foreach (var cluster in delta.Grown)
        {
            Log.SpreadingDuplication(_logger, cluster.Id, cluster.Metrics.Occurrences);
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

        var jsonReportWriter = new JsonReportWriter(options.IsIncludeRawSnippets)
        {
            Metadata = metadata
        };
        var hypertextReportWriter = new HypertextReportWriter
        {
            Metadata = metadata
        };
        var sarifReportWriter = new SarifReportWriter
        {
            Metadata = metadata
        };
        var yamlReportWriter = new YamlReportWriter(options.IsIncludeRawSnippets)
        {
            Metadata = metadata
        };
        IReportWriter writer = options.Format switch
        {
            ReportFormat.Json => jsonReportWriter,
            ReportFormat.Html => hypertextReportWriter,
            ReportFormat.Sarif => sarifReportWriter,
            _ => yamlReportWriter,
        };

        var content = writer.Write(report);

        if (options.OutputPath is { } path)
        {
            _output.Save(path, content);
            Log.ReportWritten(_logger, path);
        }
        else
        {
            _output.WriteReport(content);
        }
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

            Log.Analysing(_logger, loaded.Units.Count, loaded.Stats.Excluded);

            var result = AnalysisPipeline.Run(loaded.Units, options.Settings, loaded.Stats, cancellationToken);
            foreach (var note in result.Notes)
            {
                Log.Warning(_logger, note.Message);
            }

            Emit(options, result.Report);

            if (options.WriteBaselinePath is { } baselineOut)
            {
                _output.Save(baselineOut, Baselines.From(result.Report, _time).ToJson());
                Log.ReportWritten(_logger, baselineOut);
            }

            var regressed = CanCompareToBaseline(options, result.Report);

            var percentage = result.Report.Summary.DuplicationPercentage;
            if (options.FailOn is { } threshold && percentage >= threshold)
            {
                Log.ThresholdExceeded(_logger, percentage, threshold);
                return ExitCode.ThresholdExceeded;
            }

            return regressed && options.IsFailOnNew ? ExitCode.NewDuplication : ExitCode.Success;
        }
        catch (OperationCanceledException)
        {
            Log.Failure(_logger, "Cancelled.");
            return ExitCode.RuntimeError;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Crashed(_logger, exception);
            return ExitCode.RuntimeError;
        }
    }

    private void Report(SourceDiagnostic diagnostic)
    {
        var detail = diagnostic.Path is null ? diagnostic.Message : $"{diagnostic.Message} [{diagnostic.Path}]";

        if (diagnostic.Severity == SourceDiagnosticSeverity.Error)
        {
            Log.Failure(_logger, detail);
        }
        else if (diagnostic.Severity == SourceDiagnosticSeverity.Warning)
        {
            Log.Warning(_logger, detail);
        }
        else
        {
            Log.Info(_logger, detail);
        }
    }
}
