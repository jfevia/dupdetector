using Microsoft.Extensions.Logging;

namespace DupDetector.Cli.Tests;

/// <summary>
///     One captured log line.
/// </summary>
public sealed record LogEntry
{

    /// <summary>
    ///     
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    ///     
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="level">The level it was written at.</param>
    /// <param name="message">The rendered message.</param>
    public LogEntry(LogLevel level, string message)
    {
        Level = level;
        Message = message;
    }
}
