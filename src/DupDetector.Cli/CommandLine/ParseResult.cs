namespace DupDetector.Cli.CommandLine;

/// <summary>
///     The outcome of parsing, which is either options, a message to print, or an error.
/// </summary>
public sealed record ParseResult
{
    /// <summary>
    ///     The message explaining why parsing failed, or <c>null</c> when it did not.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    ///     Text to print instead of running, such as help, or <c>null</c> when there is none.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     The parsed options, or <c>null</c> when there is nothing to run.
    /// </summary>
    public CommandLineOptions? Options { get; init; }
}
