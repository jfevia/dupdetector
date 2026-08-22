using DupDetector.Core.Model;

namespace DupDetector.Sources;

public enum SourceDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>
/// Something the loader needs to tell the caller about.
/// </summary>
/// <remarks>
/// Diagnostics are returned as data rather than written to a console, so loading stays usable as a
/// library and testable without capturing output.
/// </remarks>
public sealed record SourceDiagnostic(SourceDiagnosticSeverity Severity, string Message, string? Path = null)
{
    public static SourceDiagnostic Warning(string message, string? path = null) =>
        new(SourceDiagnosticSeverity.Warning, message, path);

    public static SourceDiagnostic Error(string message, string? path = null) =>
        new(SourceDiagnosticSeverity.Error, message, path);
}

/// <summary>
/// Everything a load produced: the files, how many were seen and skipped, and what went wrong.
/// </summary>
public sealed record SourceLoadResult(
    IReadOnlyList<SourceUnit> Units,
    DiscoveryStats Stats,
    IReadOnlyList<SourceDiagnostic> Diagnostics)
{
    public static SourceLoadResult Empty { get; } = new([], DiscoveryStats.Empty, []);
}

/// <summary>
/// Loads source files from one input path.
/// </summary>
public interface ISourceProvider
{
    SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default);
}
