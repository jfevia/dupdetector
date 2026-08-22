using System.Text.Json;
using DupDetector.Core.Model;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting.Tests;

public static class Reports
{
    public static DetectionReport Sample(
        string rawSnippet = "public void M() { }",
        string normalizedSnippet = "public void var0 ( ) { }")
    {
        var cluster = new DuplicateCluster
        {
            Id = "dup-abc123abc123",
            Instances =
            [
                new CodeInstance("/repo/a.cs", ProjectIdentity.Named("Alpha"), false, "M", new LineRange(1, 10), "h"),
                new CodeInstance("/repo/b.cs", ProjectIdentity.Unknown, true, "M", new LineRange(4, 13), "h"),
            ],
            Metrics = new ClusterMetrics(10, 2, 2, 1, false),
            NormalizedSnippet = normalizedSnippet,
            RawSnippets = [rawSnippet, rawSnippet],
            IsCohesive = true,
            IsProductionDuplicate = true,
        };

        var fileScores = new[]
        {
            new FileScore("/repo/a.cs", ProjectIdentity.Named("Alpha"), 10, 40, 25.0, false, 1, 2),
            new FileScore("/repo/b.cs", ProjectIdentity.Unknown, 10, 40, 25.0, true, 1, 2),
        };

        return new DetectionReport(
            new ReportSummary(2, 1, 20, 80, 25.0, new DiscoveryStats(5, 3, DiscoveryMode.FileSystem)),
            [cluster],
            fileScores,
            [new ProjectScore(ProjectIdentity.Named("Alpha"), 10, 40, 25.0)]);
    }

    public static DetectionReport Empty() => new(
        new ReportSummary(0, 0, 0, 0, 0.0, DiscoveryStats.Empty),
        [],
        [],
        []);
}

public class ReportFormatTests
{
    [Theory]
    [InlineData("yaml", ReportFormat.Yaml)]
    [InlineData("YAML", ReportFormat.Yaml)]
    [InlineData("  json  ", ReportFormat.Json)]
    public void TryParse_AcceptsKnownNames(string value, ReportFormat expected)
    {
        Assert.True(ReportFormats.TryParse(value, out var format));
        Assert.Equal(expected, format);
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_RejectsAnythingElse(string? value)
    {
        // An unknown format must fail loudly rather than quietly rendering YAML.
        Assert.False(ReportFormats.TryParse(value, out _));
    }

    [Fact]
    public void Names_ListTheSupportedFormats() =>
        Assert.Equal(["yaml", "json", "html", "sarif"], ReportFormats.Names);

    [Fact]
    public void For_ReturnsAWriterPerFormat()
    {
        Assert.Equal(ReportFormat.Yaml, ReportWriters.For(ReportFormat.Yaml).Format);
        Assert.Equal(ReportFormat.Json, ReportWriters.For(ReportFormat.Json).Format);
        Assert.Equal(ReportFormat.Html, ReportWriters.For(ReportFormat.Html).Format);
    }
}

public class ReportDocumentTests
{
    [Fact]
    public void From_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => ReportDocument.From(null!, includeRawSnippets: false));

    [Fact]
    public void From_OmitsRawSnippetsUnlessRequested()
    {
        Assert.Null(ReportDocument.From(Reports.Sample(), includeRawSnippets: false).Clusters[0].RawSnippets);
        Assert.NotNull(ReportDocument.From(Reports.Sample(), includeRawSnippets: true).Clusters[0].RawSnippets);
    }

    [Fact]
    public void From_ProjectsEveryMeasuredValue()
    {
        var document = ReportDocument.From(Reports.Sample(), includeRawSnippets: true);

        Assert.Equal("dup-abc123abc123", document.Clusters[0].Id);
        Assert.Equal(10, document.Clusters[0].Lines);
        Assert.Equal(2, document.Clusters[0].Occurrences);
        Assert.Equal(10, document.Clusters[0].RemovableLines);
        Assert.True(document.Clusters[0].IsExact);
        Assert.True(document.Clusters[0].IsProductionDuplicate);
        Assert.False(document.Clusters[0].ProjectSpreadKnown);

        Assert.Equal("Alpha", document.Clusters[0].Instances[0].Project);
        Assert.Equal("<unknown>", document.Clusters[0].Instances[1].Project);
        Assert.True(document.Clusters[0].Instances[1].IsTestFile);
        Assert.Equal(4, document.Clusters[0].Instances[1].StartLine);
        Assert.Equal(13, document.Clusters[0].Instances[1].EndLine);
        Assert.Equal("M", document.Clusters[0].Instances[0].Member);
        Assert.Equal("h", document.Clusters[0].Instances[0].Hash);

        Assert.Equal("critical", document.Summary.Label);
        Assert.Equal("filesystem", document.Summary.DiscoveryMode);
        Assert.Equal(5, document.Summary.DiscoveredFiles);
        Assert.Equal(3, document.Summary.ExcludedFiles);
        Assert.Equal(2, document.Summary.TotalFiles);
        Assert.Equal(1, document.Summary.TotalClusters);
        Assert.Equal(20, document.Summary.TotalDuplicateLines);
        Assert.Equal(80, document.Summary.TotalLines);
        Assert.Equal(25.0, document.Summary.DuplicationPercentage);

        Assert.Equal("/repo/a.cs", document.FileScores[0].File);
        Assert.Equal("Alpha", document.FileScores[0].Project);
        Assert.Equal(10, document.FileScores[0].DuplicateLines);
        Assert.Equal(40, document.FileScores[0].TotalLines);
        Assert.Equal(25.0, document.FileScores[0].Percentage);
        Assert.Equal(1, document.FileScores[0].ClusterCount);
        Assert.Equal(2, document.FileScores[0].WidestClusterSpread);
        Assert.True(document.FileScores[1].IsTestFile);

        Assert.Equal("Alpha", document.ProjectScores[0].Project);
        Assert.Equal(10, document.ProjectScores[0].DuplicateLines);
        Assert.Equal(40, document.ProjectScores[0].TotalLines);
        Assert.Equal(25.0, document.ProjectScores[0].Percentage);
    }
}

public class YamlReportWriterTests
{
    private static readonly IDeserializer Reader = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    [Fact]
    public void Write_ProducesYamlAnIndependentParserAccepts()
    {
        var yaml = new YamlReportWriter().Write(Reports.Sample());
        var parsed = Reader.Deserialize<Dictionary<string, object>>(yaml);

        Assert.Contains("summary", parsed.Keys);
        Assert.Contains("clusters", parsed.Keys);
    }

    [Fact]
    public void Write_QuotesSnippetsContainingStructuralIndicators()
    {
        // Braces begin a flow mapping in YAML, so an unquoted snippet would not round-trip.
        const string Snippet = "void var0 ( ) { var1 = NUM ; } # not a comment";
        var parsed = Parse(new YamlReportWriter().Write(Reports.Sample(normalizedSnippet: Snippet)));

        Assert.Equal(Snippet, Cluster(parsed, 0)["normalizedSnippet"]);
    }

    [Fact]
    public void Write_EmitsAnEmptySequenceRatherThanNull()
    {
        var parsed = Parse(new YamlReportWriter().Write(Reports.Empty()));

        Assert.Empty((IList<object>)parsed["clusters"]);
        Assert.Empty((IList<object>)parsed["fileScores"]);
        Assert.Empty((IList<object>)parsed["projectScores"]);
    }

    [Fact]
    public void Write_UsesInvariantNumberFormatting()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var yaml = new YamlReportWriter().Write(Reports.Sample());

            Assert.DoesNotContain("25,0", yaml, StringComparison.Ordinal);
            var summary = (IDictionary<object, object>)Parse(yaml)["summary"];
            Assert.Equal("25", summary["duplicationPercentage"]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    private static Dictionary<object, object> Parse(string yaml) =>
        Reader.Deserialize<Dictionary<object, object>>(yaml);

    private static IDictionary<object, object> Cluster(Dictionary<object, object> parsed, int index) =>
        (IDictionary<object, object>)((IList<object>)parsed["clusters"])[index];

    [Fact]
    public void Write_IncludesRawSnippetsByDefault()
    {
        Assert.True(new YamlReportWriter().IncludeRawSnippets);
        Assert.Contains("rawSnippets", new YamlReportWriter().Write(Reports.Sample()), StringComparison.Ordinal);
        Assert.DoesNotContain("rawSnippets", new YamlReportWriter(includeRawSnippets: false).Write(Reports.Sample()), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_IsDeterministic() =>
        Assert.Equal(new YamlReportWriter().Write(Reports.Sample()), new YamlReportWriter().Write(Reports.Sample()));
}

public class JsonReportWriterTests
{
    [Fact]
    public void Write_ProducesParseableJson()
    {
        using var document = JsonDocument.Parse(new JsonReportWriter().Write(Reports.Sample()));

        Assert.Equal(25.0, document.RootElement.GetProperty("summary").GetProperty("duplicationPercentage").GetDouble());
        Assert.Equal(1, document.RootElement.GetProperty("clusters").GetArrayLength());
    }

    [Fact]
    public void Write_EmitsAnEmptyArrayRatherThanNull()
    {
        using var document = JsonDocument.Parse(new JsonReportWriter().Write(Reports.Empty()));

        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("clusters").ValueKind);
        Assert.Equal(0, document.RootElement.GetProperty("clusters").GetArrayLength());
    }

    [Fact]
    public void Write_IncludesRawSnippetsByDefault()
    {
        Assert.True(new JsonReportWriter().IncludeRawSnippets);
        Assert.Contains("rawSnippets", new JsonReportWriter().Write(Reports.Sample()), StringComparison.Ordinal);
        Assert.DoesNotContain("rawSnippets", new JsonReportWriter(includeRawSnippets: false).Write(Reports.Sample()), StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneOutput_KeepsTextReadable()
    {
        var json = new JsonReportWriter(includeRawSnippets: true)
            .Write(Reports.Sample(rawSnippet: "var total = a + b; // ünïcödé"));

        Assert.Contains("a + b", json, StringComparison.Ordinal);
        Assert.Contains("ünïcödé", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u002B", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkupOutput_EscapesContentThatCouldCloseTheHostElement()
    {
        // This is a security control: relaxing it would let source content break out of a script block.
        var json = JsonReportWriter.WriteForMarkup(
            Reports.Sample(rawSnippet: "var x = \"</script><h1>injected</h1>\";"),
            includeRawSnippets: true);

        Assert.DoesNotContain("</script>", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003C", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkupOutput_OmitsRawSnippetsByDefault() =>
        Assert.DoesNotContain("rawSnippets", JsonReportWriter.WriteForMarkup(Reports.Sample()), StringComparison.Ordinal);

    [Fact]
    public void BothEncoderProfilesAreConfiguredForCamelCase()
    {
        Assert.Equal(JsonNamingPolicy.CamelCase, JsonReportWriter.Standalone.PropertyNamingPolicy);
        Assert.Equal(JsonNamingPolicy.CamelCase, JsonReportWriter.EmbeddedInMarkup.PropertyNamingPolicy);
    }

    [Fact]
    public void YamlAndJsonDescribeTheSameDocument()
    {
        var report = Reports.Sample();

        using var json = JsonDocument.Parse(new JsonReportWriter().Write(report));
        var yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build()
            .Deserialize<Dictionary<object, object>>(new YamlReportWriter().Write(report));

        var yamlClusters = (IList<object>)yaml["clusters"];
        var yamlCluster = (IDictionary<object, object>)yamlClusters[0];
        var jsonCluster = json.RootElement.GetProperty("clusters")[0];

        Assert.Equal(json.RootElement.GetProperty("clusters").GetArrayLength(), yamlClusters.Count);
        Assert.Equal(jsonCluster.GetProperty("id").GetString(), yamlCluster["id"]);
        Assert.Equal(
            jsonCluster.GetProperty("instances").GetArrayLength(),
            ((IList<object>)yamlCluster["instances"]).Count);
        Assert.Equal(
            json.RootElement.GetProperty("fileScores").GetArrayLength(),
            ((IList<object>)yaml["fileScores"]).Count);
    }
}
