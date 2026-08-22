using Microsoft.Extensions.Logging;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Captures log entries without a console.
/// </summary>
public sealed class RecordingLogger : ILogger
{

    /// <summary>
    ///     
    /// </summary>
    public List<LogEntry> Entries { get; }

    /// <summary>
    ///     
    /// </summary>
    public RecordingLogger()
    {
        Entries = [];
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="state"></param>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="logLevel"></param>
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="state"></param>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var entry = new LogEntry(logLevel, formatter(state, exception));
        Entries.Add(entry);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="level"></param>
    /// <param name="fragment"></param>
    public bool CanContains(LogLevel level, string fragment)
    {
        return Entries.Exists(entry => entry.Level == level && entry.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
