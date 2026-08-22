using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DupDetector.Cli;

/// <summary>
///     Composition root. Wiring only: every decision lives in <see cref="CliHost"/>.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Wiring only; all behaviour lives in CliHost, which is covered by the CLI suite.")]
public static class Program
{

    private static bool CanTryRegisterDefaults()
    {
        try
        {
            MSBuildLocator.RegisterDefaults();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        RegisterMicrosoftBuild();

        var verbose = args.Contains("--verbose", StringComparer.Ordinal);

        using var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(verbose ? LogLevel.Information : LogLevel.Warning)
            .AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace));

        var consoleOutputSink = new ConsoleOutputSink();
        var host = new CliHost(loggerFactory.CreateLogger("dupdetector"), consoleOutputSink);

        return (int)host.Run(args, ToolVersion.Value, CancellationToken.None);
    }

    /// <summary>
    ///     MSBuild must be located before any workspace type loads. Only solution and project inputs
    ///     need it, so a machine without it can still scan directories.
    /// </summary>
    private static void RegisterMicrosoftBuild()
    {
        if (MSBuildLocator.IsRegistered)
        {
            return;
        }

        _ = CanTryRegisterDefaults();
    }
}
