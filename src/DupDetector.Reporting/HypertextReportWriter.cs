using DupDetector.Core.Model.Reporting;
using DupDetector.Reporting.Documents;
using System.Text;

namespace DupDetector.Reporting;

/// <summary>
///     Renders a self-contained HTML report.
/// </summary>
public sealed class HypertextReportWriter : IReportWriter
{
    /// <summary>
    ///     Gets the format this writer produces.
    /// </summary>
    public ReportFormat Format
    {
        get
        {
            return ReportFormat.Html;
        }
    }

    /// <summary>
    ///     Gets the provenance stamped into the output.
    /// </summary>
    public MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     Renders the report as a self-contained page.
    /// </summary>
    /// <param name="report">The report to render.</param>
    /// <returns>The HTML document.</returns>
    public string Write(DetectionReport report)
    {
        var payload = JsonReports.WriteForMarkup(report, false, Metadata);
        var builder = new StringBuilder(ReportTemplate.Text);
        _ = report.Summary;

        builder.Replace("{{STYLE}}", ReportStyle.Text);
        builder.Replace("{{SCRIPT}}", ReportScript.Text);
        builder.Replace("{{DATA}}", payload);

        foreach (var entry in ReportPlaceholders.For(report))
        {
            builder.Replace(entry.Key, entry.Value);
        }

        return builder.ToString();
    }
}
