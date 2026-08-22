using System.Globalization;
using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     
/// </summary>
public class YamlReportWriterTests
{
    /// <summary>
    ///
    /// </summary>
    [Fact]
    public void Write_EmitsAnEmptySequenceRatherThanNull()
    {
        var yamlReportWriter = new YamlReportWriter();
        var parsed = YamlFixtures.Parse(yamlReportWriter.Write(Reports.Empty()));

        Assert.Empty(Assert.IsType<IList<object>>(parsed["clusters"], exactMatch: false));
        Assert.Empty(Assert.IsType<IList<object>>(parsed["fileScores"], exactMatch: false));
        Assert.Empty(Assert.IsType<IList<object>>(parsed["projectScores"], exactMatch: false));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_IncludesRawSnippetsByDefault()
    {
        var yamlReportWriter2 = new YamlReportWriter();
        Assert.True(yamlReportWriter2.IsIncludeRawSnippets);
        var yamlReportWriter3 = new YamlReportWriter();
        Assert.Contains("rawSnippets", yamlReportWriter3.Write(Reports.Sample()), StringComparison.Ordinal);
        var yamlReportWriter4 = new YamlReportWriter(includeRawSnippets: false);
        Assert.DoesNotContain("rawSnippets", yamlReportWriter4.Write(Reports.Sample()), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_IsDeterministic()
    {
        var yamlReportWriter5 = new YamlReportWriter();
        var yamlReportWriter6 = new YamlReportWriter();
        Assert.Equal(yamlReportWriter5.Write(Reports.Sample()), yamlReportWriter6.Write(Reports.Sample()));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_ProducesYamlAnIndependentParserAccepts()
    {
        var yamlReportWriter7 = new YamlReportWriter();
        var yaml = yamlReportWriter7.Write(Reports.Sample());
        var parsed = YamlFixtures.Parse(yaml);

        Assert.Contains("summary", parsed.Keys);
        Assert.Contains("clusters", parsed.Keys);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_QuotesSnippetsContainingStructuralIndicators()
    {
        const string Snippet = "void var0 ( ) { var1 = NUM ; } # not a comment";
        var yamlReportWriter8 = new YamlReportWriter();
        var parsed = YamlFixtures.Parse(yamlReportWriter8.Write(Reports.Sample("public void M() { }", Snippet)));

        Assert.Equal(Snippet, YamlFixtures.Cluster(parsed, 0)["normalizedSnippet"]);
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
            var yamlReportWriter9 = new YamlReportWriter();
            var yaml = yamlReportWriter9.Write(Reports.Sample());

            Assert.DoesNotContain("25,0", yaml, StringComparison.Ordinal);
            var summary = Assert.IsType<IDictionary<object, object>>(YamlFixtures.Parse(yaml)["summary"], exactMatch: false);
            Assert.Equal("25", summary["duplicationPercentage"]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }
}
