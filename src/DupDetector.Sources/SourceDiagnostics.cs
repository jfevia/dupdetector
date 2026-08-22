namespace DupDetector.Sources;

/// <summary>
///     Factory helpers for <see cref="SourceDiagnostic" />.
/// </summary>
public static class SourceDiagnostics
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="message"></param>
    /// <param name="path"></param>
    public static SourceDiagnostic Error(string message, string? path)
    {
        var diagnostic = new SourceDiagnostic(SourceDiagnosticSeverity.Error, message, path);
        return diagnostic;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="message"></param>
    /// <param name="path"></param>
    public static SourceDiagnostic Warning(string message, string? path)
    {
        var diagnostic = new SourceDiagnostic(SourceDiagnosticSeverity.Warning, message, path);
        return diagnostic;
    }
}
