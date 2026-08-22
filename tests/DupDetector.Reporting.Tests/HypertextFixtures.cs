using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     Helpers for the hypertext report tests.
/// </summary>
public static class HypertextFixtures
{
    /// <summary>
    ///     How many times a substring appears.
    /// </summary>
    /// <returns></returns>
    /// <param name="haystack"></param>
    /// <param name="needle"></param>
    public static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    /// <summary>
    ///     Renders a report as hypertext.
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    public static string Render(DetectionReport report)
    {
        var writer = new HypertextReportWriter();
        return writer.Write(report);
    }
}
