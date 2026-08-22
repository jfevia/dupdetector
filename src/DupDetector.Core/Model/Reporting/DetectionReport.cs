using DupDetector.Core.Pipeline;

namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     The complete result of an analysis run.
/// </summary>
public sealed record DetectionReport
{
    /// <summary>
    ///     Gets the duplicate clusters, most severe first.
    /// </summary>
    public required IReadOnlyList<DuplicateCluster> Clusters { get; init; }

    /// <summary>
    ///     Gets the per-file duplication, densest first.
    /// </summary>
    public required IReadOnlyList<FileScore> FileScores { get; init; }

    /// <summary>
    ///     Gets the per-project duplication, densest first.
    /// </summary>
    public required IReadOnlyList<ProjectScore> ProjectScores { get; init; }

    /// <summary>
    ///     Gets what the run measured and what it withheld.
    /// </summary>
    public AnalysisScope? Scope { get; init; }

    /// <summary>
    ///     Gets the run-level totals.
    /// </summary>
    public required ReportSummary Summary { get; init; }
}
