using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     
/// </summary>
public class AnalysisPipelineTests
{

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
    private static readonly DetectionSettings Settings;

    static AnalysisPipelineTests()
    {
        Settings = new()
        {
            MinLines = 1,
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MinProductionDuplicateLines = 1,
        };
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_AppliesClusterFilters()
    {
        var unitSpec = new UnitSpec(Duplicated)
        {
            Path = "/repo/Arch/One.cs",
            Project = "Alpha"
        };
        var unitSpec2 = new UnitSpec(Duplicated)
        {
            Path = "/repo/Arch/Two.cs",
            Project = "Beta"
        };
        var units = new[]
        {
            Code.Unit(unitSpec),
            Code.Unit(unitSpec2),
        };

        Assert.Single(AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty).Report.Clusters);

        var filtered = AnalysisPipeline.Run(
            units,
            Settings with
            {
                ExcludeClusterFileGlobs = ["**/Arch/*.cs"]
            },
            DiscoveryStats.Empty);

        Assert.Empty(filtered.Report.Clusters);
        Assert.Equal(0, filtered.Report.Summary.TotalDuplicateLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_CountsEachFileOnceWhenReachedThroughSeveralPaths()
    {
        var unitSpec3 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var unit = Code.Unit(unitSpec3);

        var result = AnalysisPipeline.Run([unit, unit], Settings, DiscoveryStats.Empty);

        Assert.Equal(1, result.Report.Summary.TotalFiles);
        Assert.Empty(result.Report.Clusters);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_DetectsCrossFileDuplicationEndToEnd()
    {
        var unitSpec4 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var unitSpec5 = new UnitSpec(Duplicated)
        {
            Path = "/repo/B/Two.cs",
            Project = "Beta"
        };
        var unitSpec6 = new UnitSpec(Unique)
        {
            Path = "/repo/A/Three.cs",
            Project = "Alpha"
        };
        var units = new[]
        {
            Code.Unit(unitSpec4),
            Code.Unit(unitSpec5),
            Code.Unit(unitSpec6),
        };

        var discoveryStats = new DiscoveryStats
        {
            Discovered = 3,
            Excluded = 0,
            Mode = DiscoveryMode.FileSystem
        };
        var result = AnalysisPipeline.Run(units, Settings, discoveryStats);

        var cluster = Assert.Single(result.Report.Clusters);
        Assert.True(cluster.IsExact);
        Assert.Equal(2, cluster.Metrics.Occurrences);
        Assert.Equal(2, cluster.Metrics.FileSpread);
        Assert.Equal(2, cluster.Metrics.ProjectSpread);
        Assert.True(cluster.IsProductionDuplicate);
        var paths = new List<string>();
        foreach (var instance in cluster.Instances)
        {
            paths.Add(instance.FilePath);
        }

        Assert.Equal(["/repo/A/One.cs", "/repo/B/Two.cs"], paths);

        Assert.Equal(3, result.Report.Summary.TotalFiles);
        Assert.Equal(1, result.Report.Summary.TotalClusters);
        Assert.Equal(2, result.Report.ProjectScores.Count);
        Assert.Equal(DiscoveryMode.FileSystem, result.Report.Summary.Discovery.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_DoesNotWarnAboutProjectSpreadWhenEveryProjectIsKnown()
    {
        var unitSpec7 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var unitSpec8 = new UnitSpec(Duplicated)
        {
            Path = "/repo/B/Two.cs",
            Project = "Beta"
        };
        var units = new[]
        {
            Code.Unit(unitSpec7),
            Code.Unit(unitSpec8),
        };

        Assert.Empty(AnalysisPipeline.Run(units, Settings with
        {
            MinProjectSpread = 2
        }, DiscoveryStats.Empty).Notes);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_HonoursCancellation()
    {
        var unitSpec9 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var units = new[]
        {
            Code.Unit(unitSpec9)
        };
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty, cancellation.Token));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_IsDeterministicRegardlessOfInputOrder()
    {
        var unitSpec10 = new UnitSpec(Duplicated)
        {
            Path = "/repo/B/Two.cs",
            Project = "Beta"
        };
        var unitSpec11 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var units = new[]
        {
            Code.Unit(unitSpec10),
            Code.Unit(unitSpec11),
        };

        var first = AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty).Report;
        var reversed = new List<SourceUnit>(units);
        reversed.Reverse();
        var second = AnalysisPipeline.Run(reversed, Settings, DiscoveryStats.Empty).Report;

        Assert.Equal(PipelineQueries.ClusterIds(first), PipelineQueries.ClusterIds(second));
        Assert.Equal(PipelineQueries.ScorePaths(first), PipelineQueries.ScorePaths(second));
    }

    /// <summary>
    ///     
    /// </summary>
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

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_ScoresFilesProjectsAndTheRunConsistently()
    {
        var unitSpec12 = new UnitSpec(Duplicated)
        {
            Path = "/repo/A/One.cs",
            Project = "Alpha"
        };
        var unitSpec13 = new UnitSpec(Duplicated)
        {
            Path = "/repo/B/Two.cs",
            Project = "Beta"
        };
        var units = new[]
        {
            Code.Unit(unitSpec12),
            Code.Unit(unitSpec13),
        };

        var result = AnalysisPipeline.Run(units, Settings, DiscoveryStats.Empty);

        var fileScore = result.Report.FileScores[0];
        Assert.True(fileScore.DuplicateLines > 0);
        Assert.True(fileScore.DuplicateLines <= fileScore.TotalLines);
        Assert.Equal(1, fileScore.ClusterCount);
        Assert.Equal(2, fileScore.WidestClusterSpread);

        var duplicated = 0;
        var total = 0;
        foreach (var score in result.Report.FileScores)
        {
            duplicated += score.DuplicateLines;
            total += score.TotalLines;
        }

        Assert.Equal(duplicated, result.Report.Summary.TotalDuplicateLines);
        Assert.Equal(total, result.Report.Summary.TotalLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_WarnsWhenClusterExceededTheGroupingBudget()
    {
        var units = new List<SourceUnit>(6);
        for (var index = 0; index < 6; index++)
        {
            var spec = new UnitSpec($$"""
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
                """)
            {
                Path = $"/repo/F{index}.cs",
                Project = "P" + index
            };

            units.Add(Code.Unit(spec));
        }

        var cliqueBudget = new CliqueBudget(2, 10_000);
        var result = AnalysisPipeline.Run(
            units,
            Settings with
            {
                Similarity = 0.3
            },
            DiscoveryStats.Empty,
cliqueBudget);

        Assert.Contains(result.Notes, note => note.Message.Contains("grouping budget", StringComparison.Ordinal));
        Assert.Contains(result.Report.Clusters, cluster => !cluster.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Run_WarnsWhenProjectSpreadCannotBeEvaluated()
    {
        var unitSpec15 = new UnitSpec(Duplicated)
        {
            Path = "/loose/One.cs",
            Project = null
        };
        var unitSpec16 = new UnitSpec(Duplicated)
        {
            Path = "/loose/Two.cs",
            Project = null
        };
        var units = new[]
        {
            Code.Unit(unitSpec15),
            Code.Unit(unitSpec16),
        };

        var result = AnalysisPipeline.Run(units, Settings with
        {
            MinProjectSpread = 2
        }, DiscoveryStats.Empty);

        var note = Assert.Single(result.Notes);
        Assert.Contains("min-project-spread", note.Message, StringComparison.Ordinal);
        Assert.NotEmpty(result.Report.Clusters);
    }
}
