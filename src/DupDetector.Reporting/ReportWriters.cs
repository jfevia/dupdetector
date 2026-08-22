using DupDetector.Reporting.Documents;

using DupDetector.Reporting.Sarif;

namespace DupDetector.Reporting;

/// <summary>
///     Chooses the writer for a format.
/// </summary>
public static class ReportWriters
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="format"></param>
    public static IReportWriter For(ReportFormat format)
    {
        return For(format, metadata: null);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="format"></param>
    /// <param name="metadata"></param>
    public static IReportWriter For(ReportFormat format, MetadataDocument? metadata)
    {
        var jsonReportWriter = new JsonReportWriter
        {
            Metadata = metadata
        };
        var hypertextReportWriter = new HypertextReportWriter
        {
            Metadata = metadata
        };
        var sarifReportWriter = new SarifReportWriter
        {
            Metadata = metadata
        };
        var yamlReportWriter = new YamlReportWriter
        {
            Metadata = metadata
        };
        return format switch
        {
            ReportFormat.Json => jsonReportWriter,
            ReportFormat.Html => hypertextReportWriter,
            ReportFormat.Sarif => sarifReportWriter,
            _ => yamlReportWriter,
        };
    }
}
