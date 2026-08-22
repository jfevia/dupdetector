using DupDetector.Core.Model.Reporting;

using DupDetector.Reporting.Sarif.Model;

namespace DupDetector.Reporting.Sarif;

/// <summary>
///     Builds the SARIF property bags that disclose what a run measured.
/// </summary>
public static class SarifProperties
{
    /// <summary>
    ///     The thresholds the run applied, so a narrow report is not mistaken for a clean one.
    /// </summary>
    /// <param name="report">The report to describe.</param>
    /// <returns>The settings bag, or <c>null</c> when the run recorded no scope.</returns>
    public static SarifSettings? Settings(DetectionReport report)
    {
        if (report.Scope is not { } scope)
        {
            return null;
        }

        var settings = new SarifSettings
        {
            MinLines = scope.Settings.MinLines,
            MinTypeLines = scope.Settings.MinTypeLines,
            MinFileSpread = scope.Settings.MinFileSpread,
            MinProjectSpread = scope.Settings.MinProjectSpread,
            MaxFileSpread = scope.Settings.MaxFileSpread,
            MaxOccurrences = scope.Settings.MaxOccurrences,
            Similarity = scope.Settings.Similarity,
            Kinds = scope.Settings.Kinds.ToString().ToLowerInvariant(),
            IsExcludeTestFiles = scope.Settings.IsExcludeTestFiles,
        };

        return settings;
    }

    /// <summary>
    ///     Run totals and what the thresholds withheld.
    /// </summary>
    /// <param name="report">The report to describe.</param>
    /// <returns>The summary bag.</returns>
    public static SarifSummary Summary(DetectionReport report)
    {
        var summary = new SarifSummary
        {
            DuplicationPercentage = report.Summary.DuplicationPercentage,
            CodeDuplicationPercentage = report.Summary.CodeDuplicationPercentage,
            Label = report.Summary.Label.ToString().ToLowerInvariant(),
            TotalClusters = report.Summary.TotalClusters,
            SuppressedClusters = report.Scope?.Suppressed.Total ?? 0,
            Limitations = report.Scope?.Limitations,
        };

        return summary;
    }
}
