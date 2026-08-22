using DupDetector.Core.Model.Reporting;
using System.Globalization;
using System.Net;

namespace DupDetector.Reporting;

/// <summary>
///     The values substituted into the HTML template.
/// </summary>
public static class ReportPlaceholders
{
    /// <summary>
    ///     Builds the substitutions for one report.
    /// </summary>
    /// <param name="report">The report to describe.</param>
    /// <returns>The placeholder names mapped to their encoded values.</returns>
    public static Dictionary<string, string> For(DetectionReport report)
    {
        var summary = report.Summary;
        var label = summary.Label.ToString();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{SCORE}}"] = Number(summary.DuplicationPercentage),
            ["{{CODE_SCORE}}"] = Number(summary.CodeDuplicationPercentage),
            ["{{LABEL}}"] = WebUtility.HtmlEncode(label.ToUpperInvariant()),
            ["{{LABEL_CLASS}}"] = label.ToLowerInvariant(),
            ["{{CLUSTERS}}"] = Number(summary.TotalClusters),
            ["{{DUPLICATE_LINES}}"] = Number(summary.TotalDuplicateLines),
            ["{{TOTAL_LINES}}"] = Number(summary.TotalLines),
            ["{{DUPLICATE_CODE_LINES}}"] = Number(summary.TotalDuplicateCodeLines),
            ["{{CODE_LINES}}"] = Number(summary.TotalCodeLines),
            ["{{SUPPRESSED}}"] = Number(report.Scope?.Suppressed.Total ?? 0),
            ["{{FILES}}"] = Number(summary.TotalFiles),
            ["{{EXCLUDED_FILES}}"] = Number(summary.Discovery.Excluded),
        };

        return values;
    }

    private static string Number(double value)
    {
        return WebUtility.HtmlEncode(value.ToString("0.##", CultureInfo.InvariantCulture));
    }

    private static string Number(int value)
    {
        return WebUtility.HtmlEncode(value.ToString("N0", CultureInfo.InvariantCulture));
    }
}
