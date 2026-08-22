using DupDetector.Core.Model.Reporting;

using DupDetector.Reporting.Documents;

using System.Text.Encodings.Web;

using System.Text.Json;

using System.Text.Json.Serialization;

namespace DupDetector.Reporting;

/// <summary>
///     Serialisation options and markup embedding for the JSON report.
/// </summary>
public static class JsonReports
{
    /// <summary>
    ///     Options for JSON embedded inside markup.
    /// </summary>
    public static JsonSerializerOptions EmbeddedInMarkup { get; }

    /// <summary>
    ///     Options for output written to a file or stdout, where readable non-ASCII is wanted.
    /// </summary>
    public static JsonSerializerOptions Standalone { get; }

    static JsonReports()
    {
        EmbeddedInMarkup = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.Default,
            WriteIndented = false,
        };

        Standalone = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        };
    }

    /// <summary>
    ///     Serializes a report for embedding inside markup.
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    /// <param name="includeRawSnippets"></param>
    /// <param name="metadata"></param>
    public static string WriteForMarkup(
        DetectionReport report,
        bool includeRawSnippets,
        MetadataDocument? metadata)
    {
        return JsonSerializer.Serialize(
            ReportDocuments.From(report, includeRawSnippets, metadata),
            EmbeddedInMarkup);
    }
}
