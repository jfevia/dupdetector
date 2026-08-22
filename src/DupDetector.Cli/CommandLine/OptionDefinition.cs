namespace DupDetector.Cli.CommandLine;

/// <summary>
///     One command-line option.
/// </summary>
public sealed record OptionDefinition
{
    /// <summary>
    ///     
    /// </summary>
    public required OptionArity Arity { get; init; }

    /// <summary>
    ///     The default shown in help, or <c>null</c> when the option has none.
    /// </summary>
    public string? Default { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public string Display
    {
        get
        {
            return Arity == OptionArity.None ? Name : $"{Name} <{ValueName}>";
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public string HelpText
    {
        get
        {
            return Default is null ? Description : $"{Description} (default: {Default})";
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string ValueName { get; init; }
}
