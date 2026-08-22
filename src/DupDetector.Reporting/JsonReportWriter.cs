using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
/// Writes the report as JSON.
/// </summary>
public sealed class JsonReportWriter(bool includeRawSnippets = true) : IReportWriter
{
    /// <summary>
    /// Options for output written to a file or stdout, where readable non-ASCII is wanted.
    /// </summary>
    public static JsonSerializerOptions Standalone { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    /// <summary>
    /// Options for JSON embedded inside markup.
    /// </summary>
    /// <remarks>
    /// This is a security control, not a formatting preference. The strict encoder escapes the
    /// characters that would otherwise let source content close the surrounding element, so it must
    /// not be relaxed to match <see cref="Standalone"/>.
    /// </remarks>
    public static JsonSerializerOptions EmbeddedInMarkup { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Default,
        WriteIndented = false,
    };

    public ReportFormat Format => ReportFormat.Json;

    public bool IncludeRawSnippets => includeRawSnippets;

    public MetadataDocument? Metadata { get; init; }

    public string Write(DetectionReport report) =>
        JsonSerializer.Serialize(ReportDocument.From(report, includeRawSnippets, Metadata), Standalone);

    /// <summary>Serializes a report for embedding inside markup.</summary>
    public static string WriteForMarkup(
        DetectionReport report,
        bool includeRawSnippets = false,
        MetadataDocument? metadata = null) =>
        JsonSerializer.Serialize(ReportDocument.From(report, includeRawSnippets, metadata), EmbeddedInMarkup);
}
