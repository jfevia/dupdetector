using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Core.Pipeline;
using DupDetector.Reporting.Documents;

using DupDetector.Reporting.Sarif;
using DupDetector.TestKit;

using System.Text.Json;

using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     Covers the output surfaces added after the report audit.
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

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnEmptyBaselineIsRejectedRatherThanTreatedAsNoFindings()
    {
        var clock = new FixedTimeProvider();
        var valid = Baselines.From(AuditFixtures.Report(), clock);

        Assert.NotEmpty(Baselines.Parse(valid.ToJson()).Clusters);
        Assert.Throws<FormatException>(() => Baselines.Parse("null"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryFormatIsResolvableAndParsesBackFromItsName()
    {
        foreach (var name in ReportFormats.Names)
        {
            Assert.True(ReportFormats.CanTryParse(name, out var format));
            Assert.Equal(format, ReportWriters.For(format).Format);
            Assert.Equal(format, ReportWriters.For(format, metadata: null).Format);
        }

        Assert.False(ReportFormats.CanTryParse("xml", out _));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryWriterCarriesMetadataThroughToItsOutput()
    {
        var report = AuditFixtures.Report();

        var jsonReportWriter = new JsonReportWriter
        {
            Metadata = AuditFixtures.Metadata()
        };
        Assert.Contains("9.9.9", jsonReportWriter.Write(report), StringComparison.Ordinal);
        var yamlReportWriter = new YamlReportWriter
        {
            Metadata = AuditFixtures.Metadata()
        };
        Assert.Contains("9.9.9", yamlReportWriter.Write(report), StringComparison.Ordinal);
        var hypertextReportWriter = new HypertextReportWriter
        {
            Metadata = AuditFixtures.Metadata()
        };
        Assert.Contains("9.9.9", hypertextReportWriter.Write(report), StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void HypertextCarriesTheNewSummaryFiguresAndKeyboardAccessibleSorting()
    {
        var hypertextReportWriter2 = new HypertextReportWriter
        {
            Metadata = AuditFixtures.Metadata()
        };
        var markup = hypertextReportWriter2.Write(AuditFixtures.Report());

        Assert.Contains("aria-sort", markup, StringComparison.Ordinal);
        Assert.Contains("<button type=\"button\" data-sort=\"score\">", markup, StringComparison.Ordinal);
        Assert.Contains("Duplication (code lines)", markup, StringComparison.Ordinal);
        Assert.Contains("analysable lines", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("#64748b", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void HypertextRendersWithoutScopeBlock()
    {
        var report = AuditFixtures.Report() with
        {
            Scope = null
        };

        var hypertextReportWriter3 = new HypertextReportWriter();
        var markup = hypertextReportWriter3.Write(report);

        Assert.DoesNotContain("{{", markup, StringComparison.Ordinal);
        Assert.Contains("DupDetector Report", markup, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifCarriesTheRunSettingsAndScope()
    {
        var report = AuditFixtures.Report();
        var sarifReportWriter2 = new SarifReportWriter
        {
            Metadata = AuditFixtures.Metadata()
        };
        using var document = JsonDocument.Parse(sarifReportWriter2.Write(report));
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

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifIsProducedEvenWithoutMetadataOrAnAbsolutePath()
    {
        var sarifReportWriter3 = new SarifReportWriter();
        using var document = JsonDocument.Parse(sarifReportWriter3.Write(AuditFixtures.Report()));
        var driver = document.RootElement.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver");

        Assert.Equal("0.0.0", driver.GetProperty("version").GetString());
        Assert.Equal("DUP001", driver.GetProperty("rules")[0].GetProperty("id").GetString());

        var uri = document.RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation")
            .GetProperty("artifactLocation").GetProperty("uri").GetString();

        Assert.Equal("/repo/P0/File0.cs", uri);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifOmitsSettingsWhenTheReportCarriesNoScope()
    {
        var report = AuditFixtures.Report() with
        {
            Scope = null
        };
        var sarifReportWriter4 = new SarifReportWriter();
        using var document = JsonDocument.Parse(sarifReportWriter4.Write(report));
        var run = document.RootElement.GetProperty("runs")[0];

        Assert.False(run.GetProperty("invocations")[0].TryGetProperty("properties", out _));
        Assert.False(run.GetProperty("invocations")[0].TryGetProperty("commandLine", out _));
        Assert.Equal(0, run.GetProperty("properties").GetProperty("suppressedClusters").GetInt32());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifSeverityRisesWithReach()
    {
        static string LevelOf(DetectionReport report)
        {
            var sarifReportWriter5 = new SarifReportWriter();
            return JsonDocument.Parse(sarifReportWriter5.Write(report))
                .RootElement.GetProperty("runs")[0].GetProperty("results")[0]
                .GetProperty("level").GetString()!;
        }

        Assert.Equal("warning", LevelOf(AuditFixtures.Report(2)));

        var detectionSettings = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8,
            MinProjectSpread = 1
        };
        var wideUnits = new List<SourceUnit>();
        for (var index = 0; index < 6; index++)
        {
            wideUnits.Add(Code.Unit(Duplicated, $"/repo/F{index}.cs", "OneProject"));
        }

        var wide = AnalysisPipeline.Run(
            wideUnits,
            detectionSettings,
            DiscoveryStats.Empty).Report;

        Assert.False(wide.Clusters[0].IsProductionDuplicate);
        Assert.Equal("warning", LevelOf(wide));

        var detectionSettings2 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8,
            MinProjectSpread = 1
        };
        var confined = AnalysisPipeline.Run(
            [Code.Unit(Duplicated, "/repo/A.cs", "P"), Code.Unit(Duplicated, "/repo/B.cs", "P")],
detectionSettings2,
            DiscoveryStats.Empty).Report;

        Assert.Equal("note", LevelOf(confined));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifUsesFileUriForRootedPath()
    {
        var detectionSettings3 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var report = AnalysisPipeline.Run(
            [
                Code.Unit(Duplicated, Path.Combine(Path.GetTempPath(), "A.cs"), "P1"),
                Code.Unit(Duplicated, Path.Combine(Path.GetTempPath(), "B.cs"), "P2"),
            ],
detectionSettings3,
            DiscoveryStats.Empty).Report;

        var sarifReportWriter6 = new SarifReportWriter();
        var uri = JsonDocument.Parse(sarifReportWriter6.Write(report))
            .RootElement.GetProperty("runs")[0].GetProperty("results")[0]
            .GetProperty("locations")[0].GetProperty("physicalLocation")
            .GetProperty("artifactLocation").GetProperty("uri").GetString();

        Assert.StartsWith("file:///", uri, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SuppressedCountsSurviveProjectionToTheDocument()
    {
        var suppressed = new SuppressionCounts
        {
            BelowFileSpread = 1,
            BelowProjectSpread = 2,
            AboveFileSpread = 3,
            AboveOccurrences = 4,
            ContainedInLargerCluster = 5,
            ExcludedBySnippetPattern = 6,
            ExcludedByFileGlob = 7,
            ExcludedByProjectPattern = 8,
        };

        var scope = new AnalysisScope
        {
            Settings = DetectionSettings.Default,
            Suppressed = suppressed,
        };

        var document = ScopeDocuments.From(scope);

        Assert.Equal(36, document.Suppressed.Total);
        Assert.Equal(2, document.Suppressed.BelowProjectSpread);
        Assert.Equal(5, document.Suppressed.ContainedInLargerCluster);
        Assert.Equal(8, document.Suppressed.ExcludedByProjectPattern);
        Assert.Equal("all", document.Kinds);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheLabelReadsTheAnalysableFigureAndFallsBackWhenItIsAbsent()
    {
        var report = AuditFixtures.Report();

        Assert.True(report.Summary.TotalCodeLines > 0);
        Assert.Equal(ScoreLabels.For(report.Summary.CodeDuplicationPercentage), report.Summary.Label);

        var withoutCodeLines = report.Summary with
        {
            TotalCodeLines = 0,
            CodeDuplicationPercentage = 0
        };

        Assert.Equal(ScoreLabels.For(withoutCodeLines.DuplicationPercentage), withoutCodeLines.Label);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnchangedRunIsNotRegression()
    {
        var report = AuditFixtures.Report();
        var clock = new FixedTimeProvider();
        var delta = BaselineDeltas.Between(Baselines.From(report, clock), report);

        Assert.False(delta.IsRegression);
        Assert.Empty(delta.Added);
        Assert.Empty(delta.Grown);
        Assert.Empty(delta.Removed);
        Assert.Equal(0.0, delta.PercentagePointChange);
    }
}
