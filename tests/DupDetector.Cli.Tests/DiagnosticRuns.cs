using DupDetector.Sources;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Runs the command line against a provider that only reports diagnostics.
/// </summary>
public static class DiagnosticRuns
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="diagnostics"></param>
    public static RecordingLogger With(IReadOnlyList<SourceDiagnostic> diagnostics)
    {
        var logger = new RecordingLogger();
        var provider = new DiagnosticProvider(diagnostics);
        var loader = new SourceLoader(_ => provider);
        var recordingSink = new RecordingSink();
        var cliHost = new CliHost(logger, recordingSink, loader, null);
        cliHost.Run(["./anywhere"], "9.9.9", CancellationToken.None);
        return logger;
    }
}
