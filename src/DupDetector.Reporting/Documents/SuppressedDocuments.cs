using DupDetector.Core.Detection;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="SuppressedDocument" />.
/// </summary>
public static class SuppressedDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="counts"></param>
    public static SuppressedDocument From(SuppressionCounts counts)
    {

        var suppressedDocument = new SuppressedDocument
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
        return suppressedDocument;
    }
}
