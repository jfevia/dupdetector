using DupDetector.Core.Model;
using DupDetector.Core.Scoring;
using DupDetector.Reporting;
using DupDetector.Reporting.Documents;
using DupDetector.Reporting.Sarif;

using DupDetector.TestKit;

using System.Text.Json;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     One test per defect confirmed while auditing the duplication report the tool produced.
/// </summary>
public class ReportAuditRegressionTests
{
    /// <summary>
    ///     Three members of four lines each: every member is below MinLines, the type is not.
    /// </summary>
    private const string SmallMemberedClass = """
        internal sealed class SettableTimeProvider
        {
            private long _now;

            public long GetUtcNow()
            {
                return _now;
            }

            public void Set(long value)
            {
                _now = value;
            }
        }
        """;

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BaselineReportsGrownClusterAsGrownRatherThanNew()
    {
        var settings = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var before = Analyses.Run(settings, [SmallMemberedClass, SmallMemberedClass]);
        var after = Analyses.Run(settings, [SmallMemberedClass, SmallMemberedClass, SmallMemberedClass]);

        var clock = new FixedTimeProvider();
        var delta = BaselineDeltas.Between(Baselines.From(before, clock), after);

        Assert.Empty(delta.Added);
        Assert.Single(delta.Grown);
        Assert.True(delta.IsRegression);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BaselineReportsResolvedClustersAndSurvivesRoundTrip()
    {
        var settings = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var clock = new FixedTimeProvider();
        var before = Baselines.From(Analyses.Run(settings, [SmallMemberedClass, SmallMemberedClass]), clock);
        var after = Analyses.Run(settings, [SmallMemberedClass]);

        var delta = BaselineDeltas.Between(Baselines.Parse(before.ToJson()), after);

        Assert.Single(delta.Removed);
        Assert.Empty(delta.Added);
        Assert.False(delta.IsRegression);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BlockBodiedPropertyIsNotCountedAsBothPropertyAndAccessors()
    {
        const string source = """
            class Holder
            {
                public int Total
                {
                    get
                    {
                        var a = 1;
                        var b = 2;
                        return a + b;
                    }
                }
            }
            """;

        var detectionSettings = new DetectionSettings
        {
            MinLines = 5,
            Kinds = DetectionKind.Accessors
        };
        var names = Code.MemberNames(Code.Blocks(source, detectionSettings));

        Assert.Equal(["Total.get"], names);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DuplicatedClassIsDetectedEvenWhenEveryMemberIsBelowMinLines()
    {
        var detectionSettings2 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var report = Analyses.Run(detectionSettings2, [SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass]);

        var cluster = Assert.Single(report.Clusters);
        Assert.Equal(3, cluster.Metrics.Occurrences);
        Assert.StartsWith("class SettableTimeProvider", cluster.Instances[0].MemberName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EmittedClusterScoreIsPresentAndDistinctFromRemovableLines()
    {
        var detectionSettings3 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var report = Analyses.Run(detectionSettings3, [SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass]);

        var document = ReportDocuments.From(report, includeRawSnippets: false);
        var cluster = Assert.Single(document.Clusters);

        Assert.Equal(ClusterScore.For(report.Clusters[0].Metrics), cluster.Score);
        Assert.InRange(cluster.Score, 0.0, 100.0);
        Assert.NotEqual(cluster.RemovableLines, cluster.Score);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExpressionBodiedPropertyIsExtractedWhenLargeEnough()
    {
        const string source = """
            class Holder
            {
                public int Total =>
                    1 +
                    2 +
                    3 +
                    4;
            }
            """;

        var detectionSettings4 = new DetectionSettings
        {
            MinLines = 5,
            Kinds = DetectionKind.Accessors
        };
        var blocks = Code.Blocks(source, detectionSettings4);

        Assert.Equal("Total", Assert.Single(blocks).MemberName);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MemberClusterContainedInTypeClusterIsNotAlsoReported()
    {
        const string source = """
            internal sealed class Helper
            {
                public int Compute(int a, int b)
                {
                    var total = 0;
                    var scale = a * b;
                    total += scale;
                    total ^= a;
                    return total;
                }
            }
            """;

        var detectionSettings5 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var withTypes = Analyses.Run(detectionSettings5, [source, source]);
        var detectionSettings6 = new DetectionSettings
        {
            MinLines = 5,
            Kinds = DetectionKind.Members
        };
        var membersOnly = Analyses.Run(detectionSettings6, [source, source]);

        Assert.Single(withTypes.Clusters);
        Assert.StartsWith("class Helper", withTypes.Clusters[0].Instances[0].MemberName, StringComparison.Ordinal);
        Assert.Equal("Compute", Assert.Single(membersOnly.Clusters).Instances[0].MemberName);
        Assert.Equal(1, withTypes.Scope!.Suppressed.ContainedInLargerCluster);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NclocPercentageIsEmittedAlongsideThePhysicalOne()
    {
        const string padded = """
            // A comment that is not code.

            using System;

            internal sealed class Padded
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

        var detectionSettings7 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var report = Analyses.Run(detectionSettings7, [padded, padded]);

        Assert.True(report.Summary.TotalCodeLines < report.Summary.TotalLines);
        Assert.True(report.Summary.CodeDuplicationPercentage > report.Summary.DuplicationPercentage);

        var duplicateCodeLines = 0;
        foreach (var score in report.FileScores)
        {
            duplicateCodeLines += score.DuplicateCodeLines;
        }

        Assert.Equal(report.Summary.TotalDuplicateCodeLines, duplicateCodeLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ReportCarriesProvenance()
    {
        var metadata = new MetadataDocument
        {
            ToolVersion = "9.9.9",
            GeneratedAtUtc = "2024-01-01T00:00:00.0000000Z",
            TargetPath = "/repo",
            CommandLine = "/repo --format json",
        };

        var document = ReportDocuments.From(
            Analyses.Run(DetectionSettings.Default, [SmallMemberedClass]),
            includeRawSnippets: false,
            metadata);

        Assert.Equal("1.0", document.Metadata!.SchemaVersion);
        Assert.Equal("9.9.9", document.Metadata.ToolVersion);
        Assert.Equal("/repo --format json", document.Metadata.CommandLine);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ReportDisclosesActiveThresholdsAndWhatTheyWithheld()
    {
        var detectionSettings8 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8,
            MinFileSpread = 99
        };
        var report = Analyses.Run(detectionSettings8, [SmallMemberedClass,
            SmallMemberedClass]);

        var scope = ReportDocuments.From(report, includeRawSnippets: false).Scope;

        Assert.NotNull(scope);
        Assert.Equal(99, scope.MinFileSpread);
        Assert.Equal(8, scope.MinTypeLines);
        Assert.Equal(1, scope.Suppressed.BelowFileSpread);
        Assert.Contains(scope.Limitations, note => note.Contains("fewer than 99 files", StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SarifOutputCarriesOneResultPerClusterWithTheRestAsRelatedLocations()
    {
        var detectionSettings9 = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8
        };
        var report = Analyses.Run(detectionSettings9, [SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass]);

        var sarifReportWriter = new SarifReportWriter();
        using var document = JsonDocument.Parse(sarifReportWriter.Write(report));
        var run = document.RootElement.GetProperty("runs")[0];
        var result = run.GetProperty("results")[0];

        Assert.Equal("2.1.0", document.RootElement.GetProperty("version").GetString());
        Assert.Equal("DupDetector", run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString());
        Assert.Equal("DUP001", result.GetProperty("ruleId").GetString());
        Assert.Equal(2, result.GetProperty("relatedLocations").GetArrayLength());
        Assert.Equal(
            report.Clusters[0].Id,
            result.GetProperty("partialFingerprints").GetProperty("dupDetectorClusterId").GetString());
    }
}
