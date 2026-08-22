using System.Globalization;
using System.Net;
using System.Text;
using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
/// Renders a self-contained HTML report.
/// </summary>
/// <remarks>
/// The page embeds only what it displays. Verbatim source is never included, so sharing a report
/// does not also share the code it describes.
/// </remarks>
public sealed class HtmlReportWriter : IReportWriter
{
    private static readonly string Template = ReadAsset("report.html");
    private static readonly string Style = ReadAsset("report.css");
    private static readonly string Script = ReadAsset("report.js");

    public ReportFormat Format => ReportFormat.Html;

    public MetadataDocument? Metadata { get; init; }

    public string Write(DetectionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        // Raw snippets are deliberately excluded: the page never renders them.
        var payload = JsonReportWriter.WriteForMarkup(report, includeRawSnippets: false, Metadata);
        var summary = report.Summary;

        return new StringBuilder(Template)
            .Replace("{{STYLE}}", Style)
            .Replace("{{SCRIPT}}", Script)
            .Replace("{{DATA}}", payload)
            .Replace("{{SCORE}}", Number(summary.DuplicationPercentage))
            .Replace("{{CODE_SCORE}}", Number(summary.CodeDuplicationPercentage))
            .Replace("{{LABEL}}", WebUtility.HtmlEncode(summary.Label.ToString().ToUpperInvariant()))
            .Replace("{{LABEL_CLASS}}", summary.Label.ToString().ToLowerInvariant())
            .Replace("{{CLUSTERS}}", Number(summary.TotalClusters))
            .Replace("{{DUPLICATE_LINES}}", Number(summary.TotalDuplicateLines))
            .Replace("{{TOTAL_LINES}}", Number(summary.TotalLines))
            .Replace("{{DUPLICATE_CODE_LINES}}", Number(summary.TotalDuplicateCodeLines))
            .Replace("{{CODE_LINES}}", Number(summary.TotalCodeLines))
            .Replace("{{SUPPRESSED}}", Number(report.Scope?.Suppressed.Total ?? 0))
            .Replace("{{FILES}}", Number(summary.TotalFiles))
            .Replace("{{EXCLUDED_FILES}}", Number(summary.Discovery.Excluded))
            .ToString();
    }

    private static string Number(double value) =>
        WebUtility.HtmlEncode(value.ToString("0.##", CultureInfo.InvariantCulture));

    private static string Number(int value) =>
        WebUtility.HtmlEncode(value.ToString("N0", CultureInfo.InvariantCulture));

    internal static string ReadAsset(string name)
    {
        var assembly = typeof(HtmlReportWriter).Assembly;
        var resource = $"{assembly.GetName().Name}.Assets.{name}";
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded asset '{resource}' is missing from the assembly.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
