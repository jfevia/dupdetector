using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.Core.Scoring;

namespace DupDetector.Reporting;

/// <summary>
/// The shape written to YAML and JSON.
/// </summary>
// A dedicated shape keeps the on-disk schema stable when the domain model changes.
public sealed class ReportDocument
{
    public required SummaryDocument Summary { get; init; }

    public required IReadOnlyList<ClusterDocument> Clusters { get; init; }

    public required IReadOnlyList<FileScoreDocument> FileScores { get; init; }

    public required IReadOnlyList<ProjectScoreDocument> ProjectScores { get; init; }

    /// <summary>What the run measured, and what it found but did not report.</summary>
    public ScopeDocument? Scope { get; init; }

    /// <summary>Identifies the run that produced this report.</summary>
    public MetadataDocument? Metadata { get; init; }

    public static ReportDocument From(DetectionReport report, bool includeRawSnippets) =>
        From(report, includeRawSnippets, metadata: null);

    public static ReportDocument From(DetectionReport report, bool includeRawSnippets, MetadataDocument? metadata)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new ReportDocument
        {
            Summary = SummaryDocument.From(report.Summary),
            Clusters = [.. report.Clusters.Select(cluster => ClusterDocument.From(cluster, includeRawSnippets))],
            FileScores = [.. report.FileScores.Select(FileScoreDocument.From)],
            ProjectScores = [.. report.ProjectScores.Select(ProjectScoreDocument.From)],
            Scope = report.Scope is null ? null : ScopeDocument.From(report.Scope),
            Metadata = metadata,
        };
    }
}

/// <summary>
/// Provenance for a report, so a stale file cannot be mistaken for a current one.
/// </summary>
public sealed class MetadataDocument
{
    /// <summary>Incremented whenever the output shape changes in a way consumers must handle.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    public required string ToolVersion { get; init; }

    public required string GeneratedAtUtc { get; init; }

    public required string TargetPath { get; init; }

    /// <summary>Commit the analysed tree was on, when it could be determined.</summary>
    public string? Commit { get; init; }

    /// <summary>The command that produced this report.</summary>
    public required string CommandLine { get; init; }
}

/// <summary>
/// The thresholds a run applied and the clusters they withheld.
/// </summary>
public sealed class ScopeDocument
{
    public required int MinLines { get; init; }

    public required int MinTypeLines { get; init; }

    public required int MinFileSpread { get; init; }

    public required int MinProjectSpread { get; init; }

    public required int MaxFileSpread { get; init; }

    public required int MaxOccurrences { get; init; }

    public required double Similarity { get; init; }

    public required string Kinds { get; init; }

    public required bool ExcludeTestFiles { get; init; }

    public required SuppressedDocument Suppressed { get; init; }

    /// <summary>Plain-language statements of what this run did not measure.</summary>
    public required IReadOnlyList<string> Limitations { get; init; }

    public static ScopeDocument From(AnalysisScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return new ScopeDocument
        {
            MinLines = scope.Settings.MinLines,
            MinTypeLines = scope.Settings.MinTypeLines,
            MinFileSpread = scope.Settings.MinFileSpread,
            MinProjectSpread = scope.Settings.MinProjectSpread,
            MaxFileSpread = scope.Settings.MaxFileSpread,
            MaxOccurrences = scope.Settings.MaxOccurrences,
            Similarity = scope.Settings.Similarity,
            Kinds = scope.Settings.Kinds.ToString().ToLowerInvariant(),
            ExcludeTestFiles = scope.Settings.ExcludeTestFiles,
            Suppressed = SuppressedDocument.From(scope.Suppressed),
            Limitations = scope.Limitations,
        };
    }
}

/// <summary>
/// Clusters that were detected but withheld, by reason.
/// </summary>
public sealed class SuppressedDocument
{
    public required int Total { get; init; }

    public required int BelowFileSpread { get; init; }

    public required int BelowProjectSpread { get; init; }

    public required int AboveFileSpread { get; init; }

    public required int AboveOccurrences { get; init; }

    public required int ContainedInLargerCluster { get; init; }

    public required int ExcludedBySnippetPattern { get; init; }

    public required int ExcludedByFileGlob { get; init; }

    public required int ExcludedByProjectPattern { get; init; }

    public static SuppressedDocument From(SuppressionCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        return new SuppressedDocument
        {
            Total = counts.Total,
            BelowFileSpread = counts.BelowFileSpread,
            BelowProjectSpread = counts.BelowProjectSpread,
            AboveFileSpread = counts.AboveFileSpread,
            AboveOccurrences = counts.AboveOccurrences,
            ContainedInLargerCluster = counts.ContainedInLargerCluster,
            ExcludedBySnippetPattern = counts.ExcludedBySnippetPattern,
            ExcludedByFileGlob = counts.ExcludedByFileGlob,
            ExcludedByProjectPattern = counts.ExcludedByProjectPattern,
        };
    }
}

public sealed class SummaryDocument
{
    public required int TotalFiles { get; init; }

    public required int TotalClusters { get; init; }

    public required int TotalDuplicateLines { get; init; }

    public required int TotalLines { get; init; }

    public required double DuplicationPercentage { get; init; }

    /// <summary>Lines carrying code, excluding blanks and comments.</summary>
    public required int TotalCodeLines { get; init; }

    /// <summary>Duplicated lines carrying code.</summary>
    public required int TotalDuplicateCodeLines { get; init; }

    /// <summary>Duplication over analysable lines, comparable with tools that report against NCLOC.</summary>
    public required double CodeDuplicationPercentage { get; init; }

    public required string Label { get; init; }

    public required int DiscoveredFiles { get; init; }

    public required int ExcludedFiles { get; init; }

    public required string DiscoveryMode { get; init; }

    public static SummaryDocument From(ReportSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new SummaryDocument
        {
            TotalFiles = summary.TotalFiles,
            TotalClusters = summary.TotalClusters,
            TotalDuplicateLines = summary.TotalDuplicateLines,
            TotalLines = summary.TotalLines,
            DuplicationPercentage = summary.DuplicationPercentage,
            TotalCodeLines = summary.TotalCodeLines,
            TotalDuplicateCodeLines = summary.TotalDuplicateCodeLines,
            CodeDuplicationPercentage = summary.CodeDuplicationPercentage,
            Label = summary.Label.ToString().ToLowerInvariant(),
            DiscoveredFiles = summary.Discovery.Discovered,
            ExcludedFiles = summary.Discovery.Excluded,
            DiscoveryMode = summary.Discovery.Mode.ToString().ToLowerInvariant(),
        };
    }
}

public sealed class ClusterDocument
{
    public required string Id { get; init; }

    public required int Lines { get; init; }

    public required int Occurrences { get; init; }

    public required int FileSpread { get; init; }

    public required int ProjectSpread { get; init; }

    public required bool ProjectSpreadKnown { get; init; }

    /// <summary>Lines that disappear if every copy but one is removed.</summary>
    public required int RemovableLines { get; init; }

    /// <summary>
    /// Priority ranking that weighs removable lines against how far the copies have spread.
    /// </summary>
    public required double Score { get; init; }

    public required bool IsExact { get; init; }

    /// <summary>False when the grouping budget was exhausted and members may not all resemble one another.</summary>
    public required bool IsCohesive { get; init; }

    public required bool IsProductionDuplicate { get; init; }

    public required string NormalizedSnippet { get; init; }

    public required IReadOnlyList<InstanceDocument> Instances { get; init; }

    /// <summary>Verbatim source. Omitted unless explicitly requested, because it leaks real code.</summary>
    public IReadOnlyList<string>? RawSnippets { get; init; }

    public static ClusterDocument From(DuplicateCluster cluster, bool includeRawSnippets)
    {
        ArgumentNullException.ThrowIfNull(cluster);

        return new ClusterDocument
        {
            Id = cluster.Id,
            Lines = cluster.Metrics.Lines,
            Occurrences = cluster.Metrics.Occurrences,
            FileSpread = cluster.Metrics.FileSpread,
            ProjectSpread = cluster.Metrics.ProjectSpread,
            ProjectSpreadKnown = cluster.Metrics.ProjectSpreadKnown,
            RemovableLines = cluster.Metrics.RemovableLines,
            Score = ClusterScore.For(cluster.Metrics),
            IsExact = cluster.IsExact,
            IsCohesive = cluster.IsCohesive,
            IsProductionDuplicate = cluster.IsProductionDuplicate,
            NormalizedSnippet = cluster.NormalizedSnippet,
            Instances = [.. cluster.Instances.Select(InstanceDocument.From)],
            RawSnippets = includeRawSnippets ? cluster.RawSnippets : null,
        };
    }
}

public sealed class InstanceDocument
{
    public required string File { get; init; }

    public required string Project { get; init; }

    public required string Member { get; init; }

    public required int StartLine { get; init; }

    public required int EndLine { get; init; }

    public required bool IsTestFile { get; init; }

    public required string Hash { get; init; }

    public static InstanceDocument From(CodeInstance instance) => new()
    {
        File = instance.FilePath,
        Project = instance.Project.ToString(),
        Member = instance.MemberName,
        StartLine = instance.Lines.Start,
        EndLine = instance.Lines.End,
        IsTestFile = instance.IsTestFile,
        Hash = instance.Hash,
    };
}

public sealed class FileScoreDocument
{
    public required string File { get; init; }

    public required string Project { get; init; }

    public required int DuplicateLines { get; init; }

    public required int TotalLines { get; init; }

    public required double Percentage { get; init; }

    public required bool IsTestFile { get; init; }

    public required int ClusterCount { get; init; }

    public required int WidestClusterSpread { get; init; }

    /// <summary>Lines carrying code, excluding blanks and comments.</summary>
    public required int CodeLines { get; init; }

    /// <summary>Duplicated lines carrying code.</summary>
    public required int DuplicateCodeLines { get; init; }

    public static FileScoreDocument From(FileScore score)
    {
        ArgumentNullException.ThrowIfNull(score);

        return new FileScoreDocument
        {
            File = score.Path,
            Project = score.Project.ToString(),
            DuplicateLines = score.DuplicateLines,
            TotalLines = score.TotalLines,
            Percentage = score.Percentage,
            IsTestFile = score.IsTestFile,
            ClusterCount = score.ClusterCount,
            WidestClusterSpread = score.WidestClusterSpread,
            CodeLines = score.CodeLines,
            DuplicateCodeLines = score.DuplicateCodeLines,
        };
    }
}

public sealed class ProjectScoreDocument
{
    public required string Project { get; init; }

    public required int DuplicateLines { get; init; }

    public required int TotalLines { get; init; }

    public required double Percentage { get; init; }

    public static ProjectScoreDocument From(ProjectScore score) => new()
    {
        Project = score.Project.ToString(),
        DuplicateLines = score.DuplicateLines,
        TotalLines = score.TotalLines,
        Percentage = score.Percentage,
    };
}
