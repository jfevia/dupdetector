namespace DupDetector.Cli.CommandLine;

/// <summary>What the process returns.</summary>
public enum ExitCode
{
    /// <summary>Analysis completed and no gate was breached.</summary>
    Success = 0,

    /// <summary>The command line could not be understood.</summary>
    UsageError = 1,

    /// <summary>Analysis could not be completed.</summary>
    RuntimeError = 2,

    /// <summary>Analysis completed but duplication exceeded <c>--fail-on</c>.</summary>
    ThresholdExceeded = 3,

    /// <summary>
    /// Analysis completed but duplication appeared or spread since the baseline, under
    /// <c>--fail-on-new</c>. Distinct from <see cref="ThresholdExceeded"/> so a pipeline can tell a
    /// regression apart from a codebase that was already over its absolute limit.
    /// </summary>
    NewDuplication = 4,
}

/// <summary>How an option consumes the command line.</summary>
public enum OptionArity
{
    /// <summary>Present or absent, with no value.</summary>
    None,

    /// <summary>Takes one value and replaces any earlier value.</summary>
    SingleValue,

    /// <summary>Takes one value and accumulates across repetitions.</summary>
    Repeatable,
}

/// <summary>
/// One command-line option.
/// </summary>
/// <remarks>
/// Help text is generated from this table, so a documented default cannot drift away from the
/// default the parser actually applies.
/// </remarks>
public sealed record OptionDefinition(
    string Name,
    OptionArity Arity,
    string ValueName,
    string Description,
    string? Default = null)
{
    public string Display => Arity == OptionArity.None ? Name : $"{Name} <{ValueName}>";

    public string HelpText => Default is null ? Description : $"{Description} (default: {Default})";
}
