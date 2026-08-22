namespace DupDetector.Sources;

/// <summary>
///     Something the loader needs to tell the caller about.
/// </summary>
public sealed record SourceDiagnostic
{

    /// <summary>
    ///     
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     
    /// </summary>
    public string? Path { get; }

    /// <summary>
    ///     
    /// </summary>
    public SourceDiagnosticSeverity Severity { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="severity">How serious the finding is.</param>
    /// <param name="message">What happened.</param>
    /// <param name="path">The file it happened to, when there is one.</param>
    public SourceDiagnostic(SourceDiagnosticSeverity severity, string message, string? path)
    {
        Severity = severity;
        Message = message;
        Path = path;
    }
}
