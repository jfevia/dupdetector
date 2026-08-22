using System.Text.Json;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.Core.Scoring;
using DupDetector.Reporting;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
/// One test per defect confirmed while auditing the duplication report the tool produced.
/// </summary>
// Each test names the behaviour that was wrong, so a regression is recognised rather than rediscovered.
// See docs/disproven-findings.md for claims that were refuted and must not be "fixed".
public class ReportAuditRegressionTests
{
    /// <summary>
    /// The shape that was reported as zero: three members of four lines each, so every member falls
    /// below MinLines while the enclosing type is duplicated verbatim.
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

    private static DetectionReport Analyse(DetectionSettings settings, params string[] sources)
    {
        var units = sources
            .Select((source, index) => Code.Unit(source, $"/repo/P{index}/File{index}.cs", $"Proj{index}"))
            .ToArray();

        return AnalysisPipeline.Run(units, settings, DiscoveryStats.Empty).Report;
    }

    // Previously: reported ZERO. Every member was below MinLines and no type was ever a candidate.
    [Fact]
    public void DuplicatedClassIsDetectedEvenWhenEveryMemberIsBelowMinLines()
    {
        var report = Analyse(
            new DetectionSettings { MinLines = 5, MinTypeLines = 8 },
            SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass);

        var cluster = Assert.Single(report.Clusters);
        Assert.Equal(3, cluster.Metrics.Occurrences);
        Assert.StartsWith("class SettableTimeProvider", cluster.Instances[0].MemberName, StringComparison.Ordinal);
    }

    // Previously: a member and its enclosing type were both reported, describing the same code twice.
    [Fact]
    public void MemberClusterContainedInATypeClusterIsNotAlsoReported()
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

        var withTypes = Analyse(new DetectionSettings { MinLines = 5, MinTypeLines = 8 }, source, source);
        var membersOnly = Analyse(new DetectionSettings { MinLines = 5, Kinds = DetectionKind.Members }, source, source);

        Assert.Single(withTypes.Clusters);
        Assert.StartsWith("class Helper", withTypes.Clusters[0].Instances[0].MemberName, StringComparison.Ordinal);
        Assert.Equal("Compute", Assert.Single(membersOnly.Clusters).Instances[0].MemberName);
        Assert.Equal(1, withTypes.Scope!.Suppressed.ContainedInLargerCluster);
    }

    // Previously: ClusterScore.For had no production call site, so the HTML fell back to
    // removableLines and the Score and Removable columns were always identical.
    [Fact]
    public void EmittedClusterScoreIsPresentAndDistinctFromRemovableLines()
    {
        var report = Analyse(
            new DetectionSettings { MinLines = 5, MinTypeLines = 8 },
            SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass);

        var document = ReportDocument.From(report, includeRawSnippets: false);
        var cluster = Assert.Single(document.Clusters);

        Assert.Equal(ClusterScore.For(report.Clusters[0].Metrics), cluster.Score);
        Assert.InRange(cluster.Score, 0.0, 100.0);
        Assert.NotEqual(cluster.RemovableLines, cluster.Score);
    }

    // Previously: no output field recorded which thresholds had been applied, so a low percentage
    // produced by restrictive filters read as a clean bill of health.
    [Fact]
    public void ReportDisclosesActiveThresholdsAndWhatTheyWithheld()
    {
        var report = Analyse(
            new DetectionSettings { MinLines = 5, MinTypeLines = 8, MinFileSpread = 99 },
            SmallMemberedClass,
            SmallMemberedClass);

        var scope = ReportDocument.From(report, includeRawSnippets: false).Scope;

        Assert.NotNull(scope);
        Assert.Equal(99, scope.MinFileSpread);
        Assert.Equal(8, scope.MinTypeLines);
        Assert.Equal(1, scope.Suppressed.BelowFileSpread);
        Assert.Contains(scope.Limitations, note => note.Contains("fewer than 99 files", StringComparison.Ordinal));
    }

    // Previously: a stored report carried no version, timestamp or command, so it could not be
    // reproduced or attributed.
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

        var document = ReportDocument.From(
            Analyse(DetectionSettings.Default, SmallMemberedClass),
            includeRawSnippets: false,
            metadata);

        Assert.Equal("1.0", document.Metadata!.SchemaVersion);
        Assert.Equal("9.9.9", document.Metadata.ToolVersion);
        Assert.Equal("/repo --format json", document.Metadata.CommandLine);
    }

    // Previously: only a physical-line percentage was emitted, so blanks, comments and using
    // directives inflated the denominator and understated duplication.
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

        var report = Analyse(new DetectionSettings { MinLines = 5, MinTypeLines = 8 }, padded, padded);

        Assert.True(report.Summary.TotalCodeLines < report.Summary.TotalLines);
        Assert.True(report.Summary.CodeDuplicationPercentage > report.Summary.DuplicationPercentage);
        Assert.Equal(
            report.Summary.TotalDuplicateCodeLines,
            report.FileScores.Sum(score => score.DuplicateCodeLines));
    }

    // Previously: an expression-bodied property was never a candidate, while its block-bodied
    // equivalent was, so semantics depended on syntax.
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

        var blocks = Code.Blocks(source, new DetectionSettings { MinLines = 5, Kinds = DetectionKind.Accessors });

        Assert.Equal("Total", Assert.Single(blocks).MemberName);
    }

    // Previously: a block-bodied property would be counted once as the property and again as each
    // accessor, so the same lines were reported twice.
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

        var names = Code.Blocks(source, new DetectionSettings { MinLines = 5, Kinds = DetectionKind.Accessors })
            .Select(block => block.MemberName)
            .ToArray();

        Assert.Equal(["Total.get"], names);
    }

    // Previously: the baseline keyed on cluster id, which encodes the full membership, so a cluster
    // that gained a copy looked like an unrelated new one and growth could never be reported.
    [Fact]
    public void BaselineReportsAGrownClusterAsGrownRatherThanNew()
    {
        var settings = new DetectionSettings { MinLines = 5, MinTypeLines = 8 };
        var before = Analyse(settings, SmallMemberedClass, SmallMemberedClass);
        var after = Analyse(settings, SmallMemberedClass, SmallMemberedClass, SmallMemberedClass);

        var delta = BaselineDelta.Between(Baseline.From(before, TimeProvider.System), after);

        Assert.Empty(delta.Added);
        Assert.Single(delta.Grown);
        Assert.True(delta.IsRegression);
    }

    [Fact]
    public void BaselineReportsResolvedClustersAndSurvivesARoundTrip()
    {
        var settings = new DetectionSettings { MinLines = 5, MinTypeLines = 8 };
        var before = Baseline.From(Analyse(settings, SmallMemberedClass, SmallMemberedClass), TimeProvider.System);
        var after = Analyse(settings, SmallMemberedClass);

        var delta = BaselineDelta.Between(Baseline.Parse(before.ToJson()), after);

        Assert.Single(delta.Removed);
        Assert.Empty(delta.Added);
        Assert.False(delta.IsRegression);
    }

    [Fact]
    public void SarifOutputCarriesOneResultPerClusterWithTheRestAsRelatedLocations()
    {
        var report = Analyse(
            new DetectionSettings { MinLines = 5, MinTypeLines = 8 },
            SmallMemberedClass,
            SmallMemberedClass,
            SmallMemberedClass);

        using var document = JsonDocument.Parse(new SarifReportWriter().Write(report));
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
