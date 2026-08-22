using DupDetector.Core.Pipeline;

namespace DupDetector.Core.Model;

/// <summary>
/// Share of a file's lines that participate in at least one duplicate cluster.
/// </summary>
public sealed record FileScore(
    string Path,
    ProjectIdentity Project,
    int DuplicateLines,
    int TotalLines,
    double Percentage,
    bool IsTestFile,
    int ClusterCount,
    int WidestClusterSpread)
{
    /// <summary>Lines carrying code, excluding blanks and comments.</summary>
    public int CodeLines { get; init; }

    /// <summary>Duplicated lines that carry code.</summary>
    public int DuplicateCodeLines { get; init; }
}

/// <summary>
/// Share of a project's lines that participate in at least one duplicate cluster.
/// </summary>
public sealed record ProjectScore(
    ProjectIdentity Project,
    int DuplicateLines,
    int TotalLines,
    double Percentage);

/// <summary>
/// How source files were located for a run.
/// </summary>
public enum DiscoveryMode
{
    None,
    FileSystem,
    Workspace,
    Mixed,
}

/// <summary>
/// File discovery counts for a run.
/// </summary>
public sealed record DiscoveryStats(int Discovered, int Excluded, DiscoveryMode Mode)
{
    public static DiscoveryStats Empty { get; } = new(0, 0, DiscoveryMode.None);
}

/// <summary>
/// Run-level totals.
/// </summary>
public sealed record ReportSummary(
    int TotalFiles,
    int TotalClusters,
    int TotalDuplicateLines,
    int TotalLines,
    double DuplicationPercentage,
    DiscoveryStats Discovery)
{
    /// <summary>Lines carrying code across the run, excluding blanks and comments.</summary>
    public int TotalCodeLines { get; init; }

    /// <summary>Duplicated lines carrying code.</summary>
    public int TotalDuplicateCodeLines { get; init; }

    /// <summary>
    /// Duplication over analysable lines. Always at least <see cref="DuplicationPercentage"/>,
    /// and the figure comparable with tools that report against NCLOC.
    /// </summary>
    public double CodeDuplicationPercentage { get; init; }

    /// <summary>
    /// Severity band, read from <see cref="CodeDuplicationPercentage"/> rather than the physical
    /// figure, because blanks and comments cannot be duplicated and only dilute the rate. Falls back
    /// to the physical figure when no analysable-line count was measured, so a summary built without
    /// one is not silently labelled <see cref="ScoreLabel.Low"/>.
    /// </summary>
    public ScoreLabel Label =>
        ScoreLabels.For(TotalCodeLines > 0 ? CodeDuplicationPercentage : DuplicationPercentage);
}

/// <summary>
/// The complete result of an analysis run.
/// </summary>
public sealed record DetectionReport(
    ReportSummary Summary,
    IReadOnlyList<DuplicateCluster> Clusters,
    IReadOnlyList<FileScore> FileScores,
    IReadOnlyList<ProjectScore> ProjectScores)
{
    /// <summary>What the run measured and what it withheld.</summary>
    public AnalysisScope? Scope { get; init; }
}
