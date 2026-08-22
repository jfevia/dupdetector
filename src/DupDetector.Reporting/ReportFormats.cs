namespace DupDetector.Reporting;

/// <summary>
///     Parses a format name, rejecting anything unrecognised instead of quietly falling back.
/// </summary>
public static class ReportFormats
{
    /// <summary>
    ///     
    /// </summary>
    public static IReadOnlyList<string> Names { get; }

    static ReportFormats()
    {
        Names = ["yaml", "json", "markup", "sarif"];
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="format"></param>
    /// <param name="value"></param>
    public static bool CanTryParse(string? value, out ReportFormat format)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "yaml":
                format = ReportFormat.Yaml;
                return true;
            case "json":
                format = ReportFormat.Json;
                return true;
            case "markup":
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
