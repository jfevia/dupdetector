using DupDetector.Core.Model.Reporting;

using DupDetector.Reporting.Documents;

using System.Text.Json;

namespace DupDetector.Reporting;

/// <summary>
///     Writes the report as JSON.
/// </summary>
public sealed class JsonReportWriter : IReportWriter
{
    private readonly bool _isIncludeRawSnippets;

    /// <summary>
    ///     
    /// </summary>
    public bool IsIncludeRawSnippets
    {
        get
        {
            return _isIncludeRawSnippets;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public JsonReportWriter()
        : this(true)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="includeRawSnippets"></param>
    public JsonReportWriter(bool includeRawSnippets)
    {
        _isIncludeRawSnippets = includeRawSnippets;
    }

    /// <summary>
    ///     
    /// </summary>
    public ReportFormat Format
    {
        get
        {
            return ReportFormat.Json;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    public string Write(DetectionReport report)
    {
        return JsonSerializer.Serialize(
            ReportDocuments.From(report, _isIncludeRawSnippets, Metadata),
            JsonReports.Standalone);
    }
}
