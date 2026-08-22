using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Core.Scoring;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     
/// </summary>
public class AggregateScorerTests
{
    private static readonly DuplicateCluster Cluster;

    static AggregateScorerTests()
    {
        var lineRange = new LineRange(1, 10);
        var codeLocation = new CodeLocation("/a.cs", ProjectIdentities.Named("P"), false, lineRange);
        var codeInstance = new CodeInstance(codeLocation, "M", "h");
        var lineRange2 = new LineRange(1, 10);
        var codeLocation2 = new CodeLocation("/b.cs", ProjectIdentities.Named("Q"), false, lineRange2);
        var codeInstance2 = new CodeInstance(codeLocation2, "M", "h");
        var clusterSpread = new ClusterSpread(2, 2, true);
        var clusterMetrics = new ClusterMetrics(10, 2, clusterSpread);
        Cluster = new()
        {
            Id = "dup-1",
            Instances =
        [
codeInstance,
codeInstance2,
        ],
            Metrics = clusterMetrics,
            NormalizedSnippet = "n",
            RawSnippets = ["r", "r"],
            IsCohesive = true,
            IsProductionDuplicate = true,
        };
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DuplicateLinesByFile_CountsEachLineOnce()
    {
        var lines = AggregateScorer.DuplicateLinesByFile([Cluster]);
        Assert.Equal(10, lines["/a.cs"]);
        Assert.Equal(10, lines["/b.cs"]);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Percentage_IsZeroWhenThereAreNoLines()
    {
        Assert.Equal(0.0, AggregateScorer.Percentage(5, 0));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Percentage_RoundsAwayFromZero()
    {
        Assert.Equal(50.0, AggregateScorer.Percentage(1, 2));
        Assert.Equal(6.63, AggregateScorer.RoundPercentage(6.625));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ScoreFiles_ReportsPercentageAndClusterContext()
    {
        var sourceOrigin = new SourceOrigin("a.cs", ProjectIdentities.Named("P"), false);
        var sourceFile = new SourceFile("/a.cs", sourceOrigin, 20);
        var sourceOrigin2 = new SourceOrigin("c.cs", ProjectIdentities.Named("P"), true);
        var sourceFile2 = new SourceFile("/c.cs", sourceOrigin2, 0);
        var files = new[]
        {
sourceFile,
sourceFile2,
        };

        var scores = AggregateScorer.ScoreFiles(files, [Cluster]);

        var production = ScoreQueries.ScoreFor(scores, "/a.cs");
        Assert.Equal(10, production.DuplicateLines);
        Assert.Equal(20, production.TotalLines);
        Assert.Equal(50.0, production.Percentage);
        Assert.Equal(1, production.ClusterCount);
        Assert.Equal(2, production.WidestClusterSpread);

        var testFile = ScoreQueries.ScoreFor(scores, "/c.cs");
        Assert.Equal(0, testFile.DuplicateLines);
        Assert.Equal(0.0, testFile.Percentage);
        Assert.Equal(0, testFile.ClusterCount);
        Assert.Equal(0, testFile.WidestClusterSpread);
        Assert.True(testFile.IsTestFile);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ScoreProjects_AggregatesFileScores()
    {
        var sourceOrigin3 = new SourceOrigin("a.cs", ProjectIdentities.Named("P"), false);
        var sourceFile3 = new SourceFile("/a.cs", sourceOrigin3, 20);
        var sourceOrigin4 = new SourceOrigin("b.cs", ProjectIdentities.Named("Q"), false);
        var sourceFile4 = new SourceFile("/b.cs", sourceOrigin4, 40);
        var files = new[]
        {
sourceFile3,
sourceFile4,
        };

        var projects = AggregateScorer.ScoreProjects(AggregateScorer.ScoreFiles(files, [Cluster]));

        Assert.Equal(2, projects.Count);
        Assert.Equal(50.0, ScoreQueries.ProjectFor(projects, "P").Percentage);
        Assert.Equal(25.0, ScoreQueries.ProjectFor(projects, "Q").Percentage);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Summarize_HandlesAnEmptyRun()
    {
        var summary = AggregateScorer.Summarize([], [], DiscoveryStats.Empty);
        Assert.Equal(0.0, summary.DuplicationPercentage);
        Assert.Equal(ScoreLabel.Low, summary.Label);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Summarize_TotalsTheRun()
    {
        var sourceOrigin5 = new SourceOrigin("a.cs", ProjectIdentities.Named("P"), false);
        var sourceFile5 = new SourceFile("/a.cs", sourceOrigin5, 20);
        var files = new[]
        {
            sourceFile5
        };
        var discoveryStats = new DiscoveryStats
        {
            Discovered = 5,
            Excluded = 1,
            Mode = DiscoveryMode.FileSystem
        };
        var summary = AggregateScorer.Summarize(
            AggregateScorer.ScoreFiles(files, [Cluster]),
            [Cluster],
discoveryStats);

        Assert.Equal(1, summary.TotalFiles);
        Assert.Equal(1, summary.TotalClusters);
        Assert.Equal(10, summary.TotalDuplicateLines);
        Assert.Equal(20, summary.TotalLines);
        Assert.Equal(50.0, summary.DuplicationPercentage);
        Assert.Equal(ScoreLabel.Critical, summary.Label);
        Assert.Equal(DiscoveryMode.FileSystem, summary.Discovery.Mode);
    }
}
