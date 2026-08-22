using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;

namespace DupDetector.Cli;

/// <summary>
/// Composition root. Wiring only: every decision lives in <see cref="CliHost"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Wiring only; all behaviour lives in CliHost, which is covered by the CLI suite.")]
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        RegisterMsBuild();

        var verbose = args.Contains("--verbose", StringComparer.Ordinal);

        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(verbose ? LogLevel.Information : LogLevel.Warning)
            .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));

        var host = new CliHost(loggerFactory.CreateLogger("dupdetector"), new ConsoleOutputSink());
        var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        return (int)host.Run(args, version);
    }

    /// <summary>
    /// MSBuild must be located before any workspace type loads. Only solution and project inputs
    /// need it, so a machine without it can still scan directories.
    /// </summary>
    private static void RegisterMsBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        try
        {
            MSBuildLocator.RegisterDefaults();
        }
        catch (InvalidOperationException)
        {
            // Directory scanning does not need MSBuild.
        }
    }
}
