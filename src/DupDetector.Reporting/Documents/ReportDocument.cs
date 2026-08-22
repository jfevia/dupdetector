namespace DupDetector.Reporting.Documents;

/// <summary>
///     The shape written to YAML and JSON.
/// </summary>
public sealed class ReportDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<ClusterDocument> Clusters { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<FileScoreDocument> FileScores { get; init; }

    /// <summary>
    ///     Identifies the run that produced this report.
    /// </summary>
    public MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<ProjectScoreDocument> ProjectScores { get; init; }

    /// <summary>
    ///     What the run measured, and what it found but did not report.
    /// </summary>
    public ScopeDocument? Scope { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SummaryDocument Summary { get; init; }
}
