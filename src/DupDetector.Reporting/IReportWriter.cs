using DupDetector.Core.Model;

namespace DupDetector.Reporting;

public enum ReportFormat
{
    Yaml,
    Json,
    Html,
    Sarif,
}

/// <summary>
/// Parses a format name, rejecting anything unrecognised instead of quietly falling back.
/// </summary>
public static class ReportFormats
{
    public static IReadOnlyList<string> Names { get; } = ["yaml", "json", "html", "sarif"];

    public static bool TryParse(string? value, out ReportFormat format)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "yaml":
                format = ReportFormat.Yaml;
                return true;
            case "json":
                format = ReportFormat.Json;
                return true;
            case "html":
                format = ReportFormat.Html;
                return true;
            case "sarif":
                format = ReportFormat.Sarif;
                return true;
            default:
                format = default;
                return false;
        }
    }
}

/// <summary>
/// Renders a report in one serialization format.
/// </summary>
public interface IReportWriter
{
    ReportFormat Format { get; }

    /// <summary>Provenance stamped into the output. Absent when the caller has none to give.</summary>
    MetadataDocument? Metadata { get; init; }

    string Write(DetectionReport report);
}

/// <summary>
/// Chooses the writer for a format.
/// </summary>
public static class ReportWriters
{
    public static IReportWriter For(ReportFormat format) => For(format, metadata: null);

    public static IReportWriter For(ReportFormat format, MetadataDocument? metadata) => format switch
    {
        ReportFormat.Json => new JsonReportWriter { Metadata = metadata },
        ReportFormat.Html => new HtmlReportWriter { Metadata = metadata },
        ReportFormat.Sarif => new SarifReportWriter { Metadata = metadata },
        _ => new YamlReportWriter { Metadata = metadata },
    };
}
