using System.Globalization;

using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     
/// </summary>
public class HypertextReportWriterTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EachEmbeddedAssetCarriesItsExpectedMarker()
    {
        Assert.Contains("<!DOCTYPE markup>", ReportTemplate.Text, StringComparison.Ordinal);
        Assert.Contains("body", ReportStyle.Text, StringComparison.Ordinal);
        Assert.Contains("report-data", ReportScript.Text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Format_IsHypertext()
    {
        var hypertextReportWriter = new HypertextReportWriter();
        Assert.Equal(ReportFormat.Html, hypertextReportWriter.Format);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void HypertextIsSelectableThroughTheFormatRegistry()
    {
        Assert.True(ReportFormats.CanTryParse("markup", out var format));
        Assert.Equal(ReportFormat.Html, format);
        Assert.IsType<HypertextReportWriter>(ReportWriters.For(ReportFormat.Html));
        Assert.Contains("markup", ReportFormats.Names);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_EmbedsTheSummary()
    {
        var markup = HypertextFixtures.Render(Reports.Sample());

        Assert.Contains("25%", markup, StringComparison.Ordinal);
        Assert.Contains("CRITICAL", markup, StringComparison.Ordinal);
        Assert.Contains("dup-abc123abc123", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_EscapesContentThatCouldCloseTheScriptBlock()
    {
        var markup = HypertextFixtures.Render(Reports.Sample("public void M() { }", "</script><h1>injected</h1>"));

        Assert.DoesNotContain("<h1>injected</h1>", markup, StringComparison.Ordinal);
        Assert.Contains("\\u003C", markup, StringComparison.Ordinal);
        Assert.Equal(2, HypertextFixtures.CountOccurrences(markup, "</script>"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_HandlesAnEmptyReport()
    {
        var markup = HypertextFixtures.Render(Reports.Empty());

        Assert.Contains("0%", markup, StringComparison.Ordinal);
        Assert.Contains("LOW", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_IsDeterministic()
    {
        Assert.Equal(HypertextFixtures.Render(Reports.Sample()), HypertextFixtures.Render(Reports.Sample()));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_NeverEmbedsVerbatimSource()
    {
        var markup = HypertextFixtures.Render(Reports.Sample("var secret = ConnectionStrings.Production;", "public void var0 ( ) { }"));

        Assert.DoesNotContain("rawSnippets", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings.Production", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_ProducesSelfContainedDocument()
    {
        var markup = HypertextFixtures.Render(Reports.Sample());

        Assert.StartsWith("<!DOCTYPE markup>", markup, StringComparison.Ordinal);
        Assert.Contains("</markup>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("https://", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_UsesInvariantNumberFormatting()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Assert.Contains("25%", HypertextFixtures.Render(Reports.Sample()), StringComparison.Ordinal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }
}
