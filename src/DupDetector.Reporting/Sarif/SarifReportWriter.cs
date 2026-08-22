using DupDetector.Core.Model.Reporting;
using DupDetector.Reporting.Documents;
using System.Text.Json;

namespace DupDetector.Reporting.Sarif;

/// <summary>
///     Writes the report as SARIF 2.1.0 for code-scanning tooling.
/// </summary>
public sealed class SarifReportWriter : IReportWriter
{
    /// <summary>
    ///     Gets the format this writer produces.
    /// </summary>
    public ReportFormat Format
    {
        get
        {
            return ReportFormat.Sarif;
        }
    }

    /// <summary>
    ///     Gets the provenance stamped into the output.
    /// </summary>
    public MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     Renders the report as SARIF.
    /// </summary>
    /// <param name="report">The report to render.</param>
    /// <returns>The SARIF document.</returns>
    public string Write(DetectionReport report)
    {
        var log = SarifLog.Build(report, Metadata);
        var json = JsonSerializer.Serialize(log, JsonReports.Standalone);
        return json.Replace("\"schema\":", "\"$schema\":", StringComparison.Ordinal);
    }
}
