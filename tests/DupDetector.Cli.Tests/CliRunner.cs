namespace DupDetector.Cli.Tests;

/// <summary>
///     Runs the command line in-process and captures everything it produced.
/// </summary>
public static class CliRunner
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    public static CliRun Run(IReadOnlyList<string> args)
    {
        var sink = new RecordingSink();
        var logger = new RecordingLogger();
        var host = new CliHost(logger, sink);
        var code = host.Run(args, "9.9.9", CancellationToken.None);
        var run = new CliRun(code, sink, logger);
        return run;
    }
}
