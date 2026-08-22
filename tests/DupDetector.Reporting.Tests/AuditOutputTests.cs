using System.Text.Json;
using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
/// Covers the output surfaces added after the report audit.
/// </summary>
public class AuditOutputTests
{
    private const string Duplicated = """
        internal sealed class Repeated
        {
            public int Compute(int a)
            {
                var total = a;
                total += 1;
                total *= 2;
                return total;
            }
        }
        """;

    private static DetectionReport Report(int copies = 3, DetectionSettings? settings = null)
    {
        var units = Enumerable.Range(0, copies)
            .Select(index => Code.Unit(Duplicated, $"/repo/P{index}/File{index}.cs", $"Proj{index}"))
            .ToArray();

        return AnalysisPipeline.Run(
            units,
            settings ?? new DetectionSettings { MinLines = 5, MinTypeLines = 8 },
            DiscoveryStats.Empty).Report;
    }

    [Fact]
    public void EveryFormatIsResolvableAndParsesBackFromItsName()
    {
        foreach (var name in ReportFormats.Names)
        {
            Assert.True(ReportFormats.TryParse(name, out var format));
            Assert.Equal(format, ReportWriters.For(format).Format);
            Assert.Equal(format, ReportWriters.For(format, metadata: null).Format);
        }

        Assert.False(ReportFormats.TryParse("xml", out _));
    }

    [Fact]
    public void SarifIsProducedEvenWithoutMetadataOrAnAbsolutePath()
    {
        using var document = JsonDocument.Parse(new SarifReportWriter().Write(Report()));
        var driver = document.RootElement.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver");

        Assert.Equal("0.0.0", driver.GetProperty("version").GetString());
        Assert.Equal("DUP001", driver.GetProperty("rules")[0].GetProperty("id").GetString());

        var uri = document.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation")
            .GetProperty("artifactLocation").GetProperty("uri").GetString();

        Assert.Equal("/repo/P0/File0.cs", uri);
    }

    [Fact]
    public void SarifSeverityRisesWithReach()
    {
        static string LevelOf(DetectionReport report) =>
            JsonDocument.Parse(new SarifReportWriter().Write(report))
                .RootElement.GetProperty("runs")[0].GetProperty("results")[0]
                .GetProperty("level").GetString()!;

        // Two copies in two projects: exact and cross-project, so it is a production duplicate.
        Assert.Equal("warning", LevelOf(Report(2)));

        // Confined to one project, so not a production duplicate, but spread over six files.
        var wide = AnalysisPipeline.Run(
            [.. Enumerable.Range(0, 6).Select(index => Code.Unit(Duplicated, $"/repo/F{index}.cs", "OneProject"))],
            new DetectionSettings { MinLines = 5, MinTypeLines = 8, MinProjectSpread = 1 },
            DiscoveryStats.Empty).Report;

        Assert.False(wide.Clusters[0].IsProductionDuplicate);
        Assert.Equal("warning", LevelOf(wide));

        var confined = AnalysisPipeline.Run(
            [Code.Unit(Duplicated, "/repo/A.cs", "P"), Code.Unit(Duplicated, "/repo/B.cs", "P")],
            new DetectionSettings { MinLines = 5, MinTypeLines = 8, MinProjectSpread = 1 },
            DiscoveryStats.Empty).Report;

        Assert.Equal("note", LevelOf(confined));
    }

    [Fact]
    public void SarifUsesAFileUriForARootedPath()
    {
        var report = AnalysisPipeline.Run(
            [
                Code.Unit(Duplicated, Path.Combine(Path.GetTempPath(), "A.cs"), "P1"),
                Code.Unit(Duplicated, Path.Combine(Path.GetTempPath(), "B.cs"), "P2"),
            ],
            new DetectionSettings { MinLines = 5, MinTypeLines = 8 },
            DiscoveryStats.Empty).Report;

        var uri = JsonDocument.Parse(new SarifReportWriter().Write(report))
            .RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation")
            .GetProperty("artifactLocation").GetProperty("uri").GetString();

        Assert.StartsWith("file:///", uri, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlRendersWithoutAScopeBlock()
    {
        var report = Report() with { Scope = null };

        var html = new HtmlReportWriter().Write(report);

        Assert.DoesNotContain("{{", html, StringComparison.Ordinal);
        Assert.Contains("DupDetector Report", html, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlCarriesTheNewSummaryFiguresAndKeyboardAccessibleSorting()
    {
        var html = new HtmlReportWriter { Metadata = Metadata() }.Write(Report());

        Assert.Contains("aria-sort", html, StringComparison.Ordinal);
        Assert.Contains("<button type=\"button\" data-sort=\"score\">", html, StringComparison.Ordinal);
        Assert.Contains("Duplication (code lines)", html, StringComparison.Ordinal);
        Assert.Contains("analysable lines", html, StringComparison.Ordinal);
        Assert.DoesNotContain("#64748b", html, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryWriterCarriesMetadataThroughToItsOutput()
    {
        var report = Report();

        Assert.Contains("9.9.9", new JsonReportWriter { Metadata = Metadata() }.Write(report), StringComparison.Ordinal);
        Assert.Contains("9.9.9", new YamlReportWriter { Metadata = Metadata() }.Write(report), StringComparison.Ordinal);
        Assert.Contains("9.9.9", new HtmlReportWriter { Metadata = Metadata() }.Write(report), StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyBaselineIsRejectedRatherThanTreatedAsNoFindings()
    {
        var valid = Baseline.From(Report(), TimeProvider.System);

        Assert.NotEmpty(Baseline.Parse(valid.ToJson()).Clusters);
        Assert.Throws<FormatException>(() => Baseline.Parse("null"));
        Assert.Throws<ArgumentNullException>(() => Baseline.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => Baseline.From(null!, TimeProvider.System));
        Assert.Throws<ArgumentNullException>(() => Baseline.From(Report(), null!));
        Assert.Throws<ArgumentNullException>(() => BaselineDelta.Between(null!, Report()));
        Assert.Throws<ArgumentNullException>(() => BaselineDelta.Between(valid, null!));
    }

    [Fact]
    public void AnUnchangedRunIsNotARegression()
    {
        var report = Report();
        var delta = BaselineDelta.Between(Baseline.From(report, TimeProvider.System), report);

        Assert.False(delta.IsRegression);
        Assert.Empty(delta.Added);
        Assert.Empty(delta.Grown);
        Assert.Empty(delta.Removed);
        Assert.Equal(0.0, delta.PercentagePointChange);
    }

    [Fact]
    public void DocumentProjectionsRejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => ReportDocument.From(null!, includeRawSnippets: false));
        Assert.Throws<ArgumentNullException>(() => SummaryDocument.From(null!));
        Assert.Throws<ArgumentNullException>(() => ClusterDocument.From(null!, includeRawSnippets: false));
        Assert.Throws<ArgumentNullException>(() => FileScoreDocument.From(null!));
        Assert.Throws<ArgumentNullException>(() => ScopeDocument.From(null!));
        Assert.Throws<ArgumentNullException>(() => SuppressedDocument.From(null!));
        Assert.Throws<ArgumentNullException>(() => new SarifReportWriter().Write(null!));
    }

    [Fact]
    public void SuppressedCountsSurviveProjectionToTheDocument()
    {
        var scope = new AnalysisScope
        {
            Settings = DetectionSettings.Default,
            Suppressed = new SuppressionCounts
            {
                BelowFileSpread = 1,
                BelowProjectSpread = 2,
                AboveFileSpread = 3,
                AboveOccurrences = 4,
                ContainedInLargerCluster = 5,
                ExcludedBySnippetPattern = 6,
                ExcludedByFileGlob = 7,
                ExcludedByProjectPattern = 8,
            },
        };

        var document = ScopeDocument.From(scope);

        Assert.Equal(36, document.Suppressed.Total);
        Assert.Equal(2, document.Suppressed.BelowProjectSpread);
        Assert.Equal(5, document.Suppressed.ContainedInLargerCluster);
        Assert.Equal(8, document.Suppressed.ExcludedByProjectPattern);
        Assert.Equal("all", document.Kinds);
    }

    [Fact]
    public void SarifCarriesTheRunSettingsAndScope()
    {
        var report = Report();
        using var document = JsonDocument.Parse(new SarifReportWriter { Metadata = Metadata() }.Write(report));
        var run = document.RootElement.GetProperty("runs")[0];
        var invocation = run.GetProperty("invocations")[0];

        Assert.True(invocation.GetProperty("executionSuccessful").GetBoolean());
        Assert.Equal("/repo", invocation.GetProperty("commandLine").GetString());
        Assert.Equal(8, invocation.GetProperty("properties").GetProperty("minTypeLines").GetInt32());
        Assert.Equal("all", invocation.GetProperty("properties").GetProperty("kinds").GetString());

        var properties = run.GetProperty("properties");
        Assert.Equal(report.Summary.CodeDuplicationPercentage, properties.GetProperty("codeDuplicationPercentage").GetDouble());
        Assert.Equal(report.Summary.Label.ToString().ToLowerInvariant(), properties.GetProperty("label").GetString());
        Assert.NotEqual(0, properties.GetProperty("limitations").GetArrayLength());
    }

    [Fact]
    public void SarifOmitsSettingsWhenTheReportCarriesNoScope()
    {
        var report = Report() with { Scope = null };
        using var document = JsonDocument.Parse(new SarifReportWriter().Write(report));
        var run = document.RootElement.GetProperty("runs")[0];

        // Absent rather than null: SARIF consumers treat an omitted property as "not reported".
        Assert.False(run.GetProperty("invocations")[0].TryGetProperty("properties", out _));
        Assert.False(run.GetProperty("invocations")[0].TryGetProperty("commandLine", out _));
        Assert.Equal(0, run.GetProperty("properties").GetProperty("suppressedClusters").GetInt32());
    }

    [Fact]
    public void TheLabelReadsTheAnalysableFigureAndFallsBackWhenItIsAbsent()
    {
        var report = Report();

        Assert.True(report.Summary.TotalCodeLines > 0);
        Assert.Equal(ScoreLabels.For(report.Summary.CodeDuplicationPercentage), report.Summary.Label);

        var withoutCodeLines = report.Summary with { TotalCodeLines = 0, CodeDuplicationPercentage = 0 };

        Assert.Equal(ScoreLabels.For(withoutCodeLines.DuplicationPercentage), withoutCodeLines.Label);
    }

    private static MetadataDocument Metadata() => new()
    {
        ToolVersion = "9.9.9",
        GeneratedAtUtc = "2024-01-01T00:00:00.0000000Z",
        TargetPath = "/repo",
        CommandLine = "/repo",
    };
}
