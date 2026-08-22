using DupDetector.Cli.CommandLine;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Everything one command-line invocation produced.
/// </summary>
public sealed record CliRun
{

    /// <summary>
    ///     
    /// </summary>
    public ExitCode Code { get; }

    /// <summary>
    ///     
    /// </summary>
    public RecordingLogger Logger { get; }

    /// <summary>
    ///     The report the run produced.
    /// </summary>
    public string Report
    {
        get
        {
            return Sink.Report.ToString();
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public RecordingSink Sink { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="code">The exit code.</param>
    /// <param name="sink">What the run wrote.</param>
    /// <param name="logger">What the run logged.</param>
    public CliRun(ExitCode code, RecordingSink sink, RecordingLogger logger)
    {
        Code = code;
        Sink = sink;
        Logger = logger;
    }
}
