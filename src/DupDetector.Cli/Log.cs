using Microsoft.Extensions.Logging;

namespace DupDetector.Cli;

/// <summary>
/// Source-generated log messages, so formatting is skipped entirely when a level is disabled.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "{Detail}")]
    internal static partial void Failure(ILogger logger, string detail);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "{Detail}")]
    internal static partial void Warning(ILogger logger, string detail);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "{Detail}")]
    internal static partial void Info(ILogger logger, string detail);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Analysis failed.")]
    internal static partial void Crashed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Analysing {Files} file(s); {Excluded} excluded.")]
    internal static partial void Analysing(ILogger logger, int files, int excluded);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Report written to {Path}.")]
    internal static partial void ReportWritten(ILogger logger, string path);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "Against baseline: {Added} new, {Grown} grown, {Resolved} resolved; duplication changed by {Change} percentage point(s).")]
    internal static partial void BaselineCompared(ILogger logger, int added, int grown, int resolved, double change);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "New duplication {Id}: {Occurrences} copies, {RemovableLines} removable lines, first at {File}:{Line}.")]
    internal static partial void NewDuplication(
        ILogger logger,
        string id,
        int occurrences,
        int removableLines,
        string file,
        int line);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Duplication {Id} spread to {Occurrences} copies.")]
    internal static partial void SpreadingDuplication(ILogger logger, string id, int occurrences);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Error,
        Message = "Duplication is {Percentage}%, which reaches the --fail-on threshold of {Threshold}%.")]
    internal static partial void ThresholdExceeded(ILogger logger, double percentage, double threshold);
}
