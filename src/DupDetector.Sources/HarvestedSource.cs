using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
///     The outcome of harvesting one file: the unit when it was kept, and anything worth reporting.
/// </summary>
public sealed record HarvestedSource
{
    /// <summary>
    ///     The outcome for a file that was skipped without comment.
    /// </summary>
    public static HarvestedSource Skipped { get; }

    /// <summary>
    ///     Something the caller should know about this file.
    /// </summary>
    public SourceDiagnostic? Diagnostic { get; init; }

    /// <summary>
    ///     The parsed unit, or <c>null</c> when the file was skipped.
    /// </summary>
    public SourceUnit? Unit { get; init; }

    static HarvestedSource()
    {
        Skipped = new();
    }
}
