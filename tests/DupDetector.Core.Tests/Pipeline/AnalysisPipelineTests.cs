using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

public class AnalysisPipelineTests
{
    private static readonly DetectionSettings Settings = new()
    {
        MinLines = 1,
        MinFileSpread = 1,
        MinProjectSpread = 1,
        MinProductionDuplicateLines = 1,
    };

    private const string Duplicated = """
        public class Holder
        {
            public int Total(Order order)
            {
                var running = order.Price;
                var adjusted = running;
                var final = adjusted;
                return final;
            }
        }
        """;

    private const string Unique = """
        public class Other
        {
            public string Describe(Customer customer)
            {
                var label = customer.Name;
                return label;
            }
        }
        """;

    [Fact]
    public void Run_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => AnalysisPipeline.Run(null!, Settings, DiscoveryStats.Empty));
        Assert.Throws<ArgumentNullException>(() => AnalysisPipeline.Run([], null!, DiscoveryStats.Empty));
        Assert.Throws<ArgumentNullException>(() => AnalysisPipeline.Run([], Settings, null!));
    }

    [Fact]
    public void Run_ProducesAnEmptyReportForNoInput()
    {
        var result = AnalysisPipeline.Run([], Settings, DiscoveryStats.Empty);

        Assert.Empty(result.Report.Clusters);
        Assert.Empty(result.Report.FileScores);
        Assert.Empty(result.Report.ProjectScores);
        Assert.Equal(0, result.Report.Summary.TotalFiles);
        Assert.Equal(0.0, result.Report.Summary.DuplicationPercentage);
        Assert.Empty(result.Notes);
    }

    [Fact]
    public void Run_DetectsCrossFileDuplicationEndToEnd()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha"),
            Code.Unit(Duplicated, path: "/repo/B/Two.cs", project: "Beta"),
            Code.Unit(Unique, path: "/repo/A/Three.cs", project: "Alpha"),
        };

        var result = AnalysisPipeline.Run(units, Settings, new DiscoveryStats(3, 0, DiscoveryMode.FileSystem));

        var cluster = Assert.Single(result.Report.Clusters);
        Assert.True(cluster.IsExact);
        Assert.Equal(2, cluster.Metrics.Occurrences);
        Assert.Equal(2, cluster.Metrics.FileSpread);
        Assert.Equal(2, cluster.Metrics.ProjectSpread);
        Assert.True(cluster.IsProductionDuplicate);
        Assert.Equal(["/repo/A/One.cs", "/repo/B/Two.cs"], cluster.Instances.Select(instance => instance.FilePath));

        Assert.Equal(3, result.Report.Summary.TotalFiles);
        Assert.Equal(1, result.Report.Summary.TotalClusters);
        Assert.Equal(2, result.Report.ProjectScores.Count);
        Assert.Equal(DiscoveryMode.FileSystem, result.Report.Summary.Discovery.Mode);
    }

    [Fact]
    public void Run_CountsEachFileOnceWhenReachedThroughSeveralPaths()
    {
        var unit = Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha");

        var result = AnalysisPipeline.Run([unit, unit], Settings, DiscoveryStats.Empty);

        Assert.Equal(1, result.Report.Summary.TotalFiles);
        Assert.Empty(result.Report.Clusters);
    }

    [Fact]
    public void Run_ScoresFilesProjectsAndTheRunConsistently()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha"),
            Code.Unit(Duplicated, path: "/repo/B/Two.cs", project: "Beta"),
        };

        var result = AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty);

        var fileScore = result.Report.FileScores[0];
        Assert.True(fileScore.DuplicateLines > 0);
        Assert.True(fileScore.DuplicateLines <= fileScore.TotalLines);
        Assert.Equal(1, fileScore.ClusterCount);
        Assert.Equal(2, fileScore.WidestClusterSpread);

        Assert.Equal(
            result.Report.FileScores.Sum(score => score.DuplicateLines),
            result.Report.Summary.TotalDuplicateLines);
        Assert.Equal(
            result.Report.FileScores.Sum(score => score.TotalLines),
            result.Report.Summary.TotalLines);
    }

    [Fact]
    public void Run_AppliesClusterFilters()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/repo/Arch/One.cs", project: "Alpha"),
            Code.Unit(Duplicated, path: "/repo/Arch/Two.cs", project: "Beta"),
        };

        Assert.Single(AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty).Report.Clusters);

        var filtered = AnalysisPipeline.Run(
            units,
            Settings with { ExcludeClusterFileGlobs = ["**/Arch/*.cs"] },
            DiscoveryStats.Empty);

        Assert.Empty(filtered.Report.Clusters);
        // The summary agrees with the cluster list: suppressed duplication is not counted.
        Assert.Equal(0, filtered.Report.Summary.TotalDuplicateLines);
    }

    [Fact]
    public void Run_WarnsWhenProjectSpreadCannotBeEvaluated()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/loose/One.cs", project: null),
            Code.Unit(Duplicated, path: "/loose/Two.cs", project: null),
        };

        var result = AnalysisPipeline.Run(units, Settings with { MinProjectSpread = 2 }, DiscoveryStats.Empty);

        var note = Assert.Single(result.Notes);
        Assert.Contains("min-project-spread", note.Message, StringComparison.Ordinal);
        // The clusters are still reported rather than silently vanishing.
        Assert.NotEmpty(result.Report.Clusters);
    }

    [Fact]
    public void Run_DoesNotWarnAboutProjectSpreadWhenEveryProjectIsKnown()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha"),
            Code.Unit(Duplicated, path: "/repo/B/Two.cs", project: "Beta"),
        };

        Assert.Empty(AnalysisPipeline.Run(units, Settings with { MinProjectSpread = 2 }, DiscoveryStats.Empty).Notes);
    }

    [Fact]
    public void Run_WarnsWhenAClusterExceededTheGroupingBudget()
    {
        var units = Enumerable.Range(0, 6)
            .Select(index => Code.Unit(
                $$"""
                public class Holder
                {
                    public int Work(Order order)
                    {
                        var shared = order.Price;
                        var common = shared;
                        var extra = order.Field{{index}};
                        return common;
                    }
                }
                """,
                path: $"/repo/F{index}.cs",
                project: "P" + index))
            .ToArray();

        var result = AnalysisPipeline.Run(
            units,
            Settings with { Similarity = 0.3 },
            DiscoveryStats.Empty,
            new CliqueBudget(MaxGroupSize: 2, MaxWork: 10_000));

        Assert.Contains(result.Notes, note => note.Message.Contains("grouping budget", StringComparison.Ordinal));
        Assert.Contains(result.Report.Clusters, cluster => !cluster.IsCohesive);
    }

    [Fact]
    public void Run_HonoursCancellation()
    {
        var units = new[] { Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha") };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty, cancellation.Token));
    }

    [Fact]
    public void Run_IsDeterministicRegardlessOfInputOrder()
    {
        var units = new[]
        {
            Code.Unit(Duplicated, path: "/repo/B/Two.cs", project: "Beta"),
            Code.Unit(Duplicated, path: "/repo/A/One.cs", project: "Alpha"),
        };

        var first = AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty).Report;
        var second = AnalysisPipeline.Run([.. units.Reverse()], Settings, DiscoveryStats.Empty).Report;

        Assert.Equal(
            first.Clusters.Select(cluster => cluster.Id),
            second.Clusters.Select(cluster => cluster.Id));
        Assert.Equal(
            first.FileScores.Select(score => score.Path),
            second.FileScores.Select(score => score.Path));
    }
}
