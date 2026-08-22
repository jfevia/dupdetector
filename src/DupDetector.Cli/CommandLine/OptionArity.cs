
namespace DupDetector.Cli.CommandLine;

/// <summary>
///     How an option consumes the command line.
/// </summary>
public enum OptionArity
{
    /// <summary>
    ///     Present or absent, with no value.
    /// </summary>
    None,

    /// <summary>
    ///     Takes one value and replaces any earlier value.
    /// </summary>
    SingleValue,

    /// <summary>
    ///     Takes one value and accumulates across repetitions.
    /// </summary>
    Repeatable,
}
