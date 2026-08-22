using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="ReportDocument" />.
/// </summary>
public static class ReportDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    /// <param name="includeRawSnippets"></param>
    public static ReportDocument From(DetectionReport report, bool includeRawSnippets)
    {
        return From(report, includeRawSnippets, metadata: null);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    /// <param name="includeRawSnippets"></param>
    /// <param name="metadata"></param>
    public static ReportDocument From(DetectionReport report, bool includeRawSnippets, MetadataDocument? metadata)
    {

        var clusters = new List<ClusterDocument>(report.Clusters.Count);
        foreach (var cluster in report.Clusters)
        {
            clusters.Add(ClusterDocuments.From(cluster, includeRawSnippets));
        }

        var fileScores = new List<FileScoreDocument>(report.FileScores.Count);
        foreach (var score in report.FileScores)
        {
            fileScores.Add(FileScoreDocuments.From(score));
        }

        var projectScores = new List<ProjectScoreDocument>(report.ProjectScores.Count);
        foreach (var score in report.ProjectScores)
        {
            projectScores.Add(ProjectScoreDocuments.From(score));
        }

        var reportDocument = new ReportDocument
        {
            Summary = SummaryDocuments.From(report.Summary),
            Clusters = clusters,
            FileScores = fileScores,
            ProjectScores = projectScores,
            Scope = report.Scope is null ? null : ScopeDocuments.From(report.Scope),
            Metadata = metadata,
        };
        return reportDocument;
    }
}
