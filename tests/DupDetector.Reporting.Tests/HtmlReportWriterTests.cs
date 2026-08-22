using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Reporting.Tests;

public class HtmlReportWriterTests
{
    private static string Render(DetectionReport report) => new HtmlReportWriter().Write(report);

    [Fact]
    public void Write_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => new HtmlReportWriter().Write(null!));

    [Fact]
    public void Format_IsHtml() => Assert.Equal(ReportFormat.Html, new HtmlReportWriter().Format);

    [Fact]
    public void Write_ProducesASelfContainedDocument()
    {
        var html = Render(Reports.Sample());

        Assert.StartsWith("<!DOCTYPE html>", html, StringComparison.Ordinal);
        Assert.Contains("</html>", html, StringComparison.Ordinal);
        // No placeholder survives, and no external resource is referenced.
        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", html, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_EmbedsTheSummary()
    {
        var html = Render(Reports.Sample());

        Assert.Contains("25%", html, StringComparison.Ordinal);
        Assert.Contains("CRITICAL", html, StringComparison.Ordinal);
        Assert.Contains("dup-abc123abc123", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_NeverEmbedsVerbatimSource()
    {
        // The page does not display raw snippets, so it must not carry them either.
        var html = Render(Reports.Sample(rawSnippet: "var secret = ConnectionStrings.Production;"));

        Assert.DoesNotContain("rawSnippets", html, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings.Production", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_EscapesContentThatCouldCloseTheScriptBlock()
    {
        var html = Render(Reports.Sample(normalizedSnippet: "</script><h1>injected</h1>"));

        Assert.DoesNotContain("<h1>injected</h1>", html, StringComparison.Ordinal);
        Assert.Contains("\\u003C", html, StringComparison.Ordinal);
        // Exactly the two script elements the template declares.
        Assert.Equal(2, CountOccurrences(html, "</script>"));
    }

    [Fact]
    public void Write_HandlesAnEmptyReport()
    {
        var html = Render(Reports.Empty());

        Assert.Contains("0%", html, StringComparison.Ordinal);
        Assert.Contains("LOW", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_IsDeterministic() =>
        Assert.Equal(Render(Reports.Sample()), Render(Reports.Sample()));

    [Fact]
    public void Write_UsesInvariantNumberFormatting()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Contains("25%", Render(Reports.Sample()), StringComparison.Ordinal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    [Fact]
    public void HtmlIsSelectableThroughTheFormatRegistry()
    {
        Assert.True(ReportFormats.TryParse("html", out var format));
        Assert.Equal(ReportFormat.Html, format);
        Assert.IsType<HtmlReportWriter>(ReportWriters.For(ReportFormat.Html));
        Assert.Contains("html", ReportFormats.Names);
    }

    [Fact]
    public void ReadAsset_FailsLoudlyWhenAnAssetIsMissing()
    {
        var error = Assert.Throws<InvalidOperationException>(() => HtmlReportWriter.ReadAsset("not-packaged.css"));
        Assert.Contains("not-packaged.css", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadAsset_ReturnsEachEmbeddedAsset()
    {
        Assert.Contains("<!DOCTYPE html>", HtmlReportWriter.ReadAsset("report.html"), StringComparison.Ordinal);
        Assert.Contains("body", HtmlReportWriter.ReadAsset("report.css"), StringComparison.Ordinal);
        Assert.Contains("report-data", HtmlReportWriter.ReadAsset("report.js"), StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
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
}
