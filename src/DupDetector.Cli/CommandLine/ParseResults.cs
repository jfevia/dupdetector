namespace DupDetector.Cli.CommandLine;

/// <summary>
///     Builds the three shapes a parse can end in.
/// </summary>
public static class ParseResults
{
    /// <summary>
    ///     Parsing failed.
    /// </summary>
    /// <returns></returns>
    /// <param name="error"></param>
    public static ParseResult Failed(string error)
    {
        var result = new ParseResult
        {
            Error = error
        };

        return result;
    }

    /// <summary>
    ///     Parsing produced options to run.
    /// </summary>
    /// <returns></returns>
    /// <param name="options"></param>
    public static ParseResult Parsed(CommandLineOptions options)
    {
        var result = new ParseResult
        {
            Options = options
        };

        return result;
    }

    /// <summary>
    ///     Parsing produced text to print instead of running.
    /// </summary>
    /// <returns></returns>
    /// <param name="message"></param>
    public static ParseResult Print(string message)
    {
        var result = new ParseResult
        {
            Message = message
        };

        return result;
    }
}
