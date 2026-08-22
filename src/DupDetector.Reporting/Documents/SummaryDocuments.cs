using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="SummaryDocument" />.
/// </summary>
public static class SummaryDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="summary"></param>
    public static SummaryDocument From(ReportSummary summary)
    {

        var summaryDocument = new SummaryDocument
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
        return summaryDocument;
    }
}
