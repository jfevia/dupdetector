using System.Text.Json;
using Xunit;

using YamlDotNet.Serialization;

using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     
/// </summary>
public class JsonReportWriterTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BothEncoderProfilesAreConfiguredForCamelCase()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, JsonReports.Standalone.PropertyNamingPolicy);
        Assert.Equal(JsonNamingPolicy.CamelCase, JsonReports.EmbeddedInMarkup.PropertyNamingPolicy);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MarkupOutput_EscapesContentThatCouldCloseTheHostElement()
    {
        var json = JsonReports.WriteForMarkup(
            Reports.Sample("var x = \"</script><h1>injected</h1>\";", "public void var0 ( ) { }"),
            true,
            null);

        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MarkupOutput_OmitsRawSnippetsByDefault()
    {
        var json = JsonReports.WriteForMarkup(Reports.Sample(), false, null);
        Assert.DoesNotContain("rawSnippets", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void StandaloneOutput_KeepsTextReadable()
    {
        var jsonReportWriter = new JsonReportWriter(includeRawSnippets: true);
        var json = jsonReportWriter.Write(Reports.Sample("var total = a + b; // ünïcödé", "public void var0 ( ) { }"));

        Assert.Contains("a + b", json, StringComparison.Ordinal);
        Assert.Contains("ünïcödé", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002B", json, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_EmitsAnEmptyArrayRatherThanNull()
    {
        var jsonReportWriter2 = new JsonReportWriter();
        using var document = JsonDocument.Parse(jsonReportWriter2.Write(Reports.Empty()));

        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("clusters").ValueKind);
        Assert.Equal(0, document.RootElement.GetProperty("clusters").GetArrayLength());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_IncludesRawSnippetsByDefault()
    {
        var jsonReportWriter3 = new JsonReportWriter();
        Assert.True(jsonReportWriter3.IsIncludeRawSnippets);
        var jsonReportWriter4 = new JsonReportWriter();
        Assert.Contains("rawSnippets", jsonReportWriter4.Write(Reports.Sample()), StringComparison.Ordinal);
        var jsonReportWriter5 = new JsonReportWriter(includeRawSnippets: false);
        Assert.DoesNotContain("rawSnippets", jsonReportWriter5.Write(Reports.Sample()), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Write_ProducesParseableJson()
    {
        var jsonReportWriter6 = new JsonReportWriter();
        using var document = JsonDocument.Parse(jsonReportWriter6.Write(Reports.Sample()));

        Assert.Equal(25.0, document.RootElement.GetProperty("summary").GetProperty("duplicationPercentage").GetDouble());
        Assert.Equal(1, document.RootElement.GetProperty("clusters").GetArrayLength());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void YamlAndJsonDescribeTheSameDocument()
    {
        var report = Reports.Sample();

        var jsonReportWriter7 = new JsonReportWriter();
        using var json = JsonDocument.Parse(jsonReportWriter7.Write(report));
        var deserializerBuilder = new DeserializerBuilder();
        var yamlReportWriter10 = new YamlReportWriter();
        var yaml = deserializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Dictionary<object, object>>(yamlReportWriter10.Write(report));

        var yamlClusters = Assert.IsType<IList<object>>(yaml["clusters"], exactMatch: false);
        var yamlCluster = Assert.IsType<IDictionary<object, object>>(yamlClusters[0], exactMatch: false);
        var jsonCluster = json.RootElement.GetProperty("clusters")[0];

        Assert.Equal(json.RootElement.GetProperty("clusters").GetArrayLength(), yamlClusters.Count);
        Assert.Equal(jsonCluster.GetProperty("id").GetString(), yamlCluster["id"]);
        Assert.Equal(
            jsonCluster.GetProperty("instances").GetArrayLength(),
            Assert.IsType<IList<object>>(yamlCluster["instances"], exactMatch: false).Count);
        Assert.Equal(
            json.RootElement.GetProperty("fileScores").GetArrayLength(),
            Assert.IsType<IList<object>>(yaml["fileScores"], exactMatch: false).Count);
    }
}
