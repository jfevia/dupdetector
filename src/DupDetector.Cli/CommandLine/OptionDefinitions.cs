namespace DupDetector.Cli.CommandLine;

/// <summary>
///     Builds option definitions so the option table stays declarative.
/// </summary>
public static class OptionDefinitions
{
    /// <summary>
    ///     An option that takes no value.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public static OptionDefinition Flag(string name, string description)
    {
        var option = new OptionDefinition
        {
            Name = name,
            Arity = OptionArity.None,
            ValueName = string.Empty,
            Description = description,
        };

        return option;
    }

    /// <summary>
    ///     An option that may be supplied more than once.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    /// <param name="valueName"></param>
    /// <param name="description"></param>
    public static OptionDefinition Repeatable(string name, string valueName, string description)
    {
        var option = new OptionDefinition
        {
            Name = name,
            Arity = OptionArity.Repeatable,
            ValueName = valueName,
            Description = description,
        };

        return option;
    }

    /// <summary>
    ///     An option that takes one value.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    /// <param name="valueName"></param>
    /// <param name="description"></param>
    public static OptionDefinition Value(string name, string valueName, string description)
    {
        var option = new OptionDefinition
        {
            Name = name,
            Arity = OptionArity.SingleValue,
            ValueName = valueName,
            Description = description,
        };

        return option;
    }

    /// <summary>
    ///     An option that takes one value and shows a default in help.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    /// <param name="valueName"></param>
    /// <param name="description"></param>
    /// <param name="fallback"></param>
    public static OptionDefinition Value(string name, string valueName, string description, string fallback)
    {
        var option = Value(name, valueName, description) with
        {
            Default = fallback
        };

        return option;
    }
}
