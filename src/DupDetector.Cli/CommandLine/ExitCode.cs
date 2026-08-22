
namespace DupDetector.Cli.CommandLine;

/// <summary>
///     What the process returns.
/// </summary>
public enum ExitCode
{
    /// <summary>
    ///     Analysis completed and no gate was breached.
    /// </summary>
    Success = 0,

    /// <summary>
    ///     The command line could not be understood.
    /// </summary>
    UsageError = 1,

    /// <summary>
    ///     Analysis could not be completed.
    /// </summary>
    RuntimeError = 2,

    /// <summary>
    ///     Analysis completed but duplication exceeded <c>--fail-on</c>.
    /// </summary>
    ThresholdExceeded = 3,

    /// <summary>
    ///     Duplication appeared or spread since the baseline, under <c>--fail-on-new</c>.
    /// </summary>
    NewDuplication = 4,
}
