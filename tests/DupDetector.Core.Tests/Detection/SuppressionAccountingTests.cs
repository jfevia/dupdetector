using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Covers attribution of discarded clusters to the threshold that discarded them.
/// </summary>
public class SuppressionAccountingTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ClustersRejectedForTooManyCopiesAreAttributedToThatThreshold()
    {
        var detectionSettings3 = new DetectionSettings
        {
            MinLines = 1,
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MaxOccurrences = 3,
            Similarity = 0.5
        };
        var outcome = DuplicateDetector.DetectDetailed(
            SuppressionFixtures.Similar(6),
detectionSettings3,
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.AboveOccurrences);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ClustersRejectedForTooNarrowProjectSpreadAreAttributedToThatThreshold()
    {
        var detectionSettings4 = new DetectionSettings
        {
            MinLines = 1,
            MinFileSpread = 1,
            MinProjectSpread = 9
        };
        var outcome = DuplicateDetector.DetectDetailed(
            SuppressionFixtures.Blocks(3, 3),
detectionSettings4,
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.BelowProjectSpread);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ClustersRejectedForTooWideSpreadAreAttributedToThatThreshold()
    {
        var detectionSettings5 = new DetectionSettings
        {
            MinLines = 1,
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MaxFileSpread = 3,
            Similarity = 0.5
        };
        var outcome = DuplicateDetector.DetectDetailed(
            SuppressionFixtures.Similar(6),
detectionSettings5,
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.AboveFileSpread);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ContainmentKeepsTheWiderClusterAndNeverSuppressesBothOfOnePair()
    {
        var outer = SuppressionFixtures.Cluster("outer", "o", 1, 20);
        var inner = SuppressionFixtures.Cluster("inner", "i", 5, 9);

        Assert.Same(outer, Assert.Single(ClusterFilters.SuppressContained([outer, inner])));

        Assert.Equal(2, ClusterFilters.SuppressContained([outer, outer with
        {
            Id = "other"
        }]).Count);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ContentKeyIsStableWhenCopyIsAdded()
    {
        var settings = new DetectionSettings
        {
            MinLines = 1
        };
        var two = DuplicateDetector.Build(SuppressionFixtures.Blocks(2, 2), settings, cohesive: true);
        var three = DuplicateDetector.Build(SuppressionFixtures.Blocks(3, 3), settings, cohesive: true);

        Assert.Equal(two.ContentKey, three.ContentKey);
        Assert.NotEqual(two.Id, three.Id);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExcludedClustersAreAttributedToTheRuleThatExcludedThem()
    {
        var detectionSettings6 = new DetectionSettings
        {
            MinLines = 1
        };
        var cluster = DuplicateDetector.Build(
            SuppressionFixtures.Blocks(2, 2),
detectionSettings6,
            cohesive: true);

        var outcome = new DetectionOutcome([cluster], SuppressionCounts.Empty);

        var detectionSettings7 = new DetectionSettings
        {
            ExcludeSnippetPatterns = ["identical"]
        };
        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, detectionSettings7)
                .Suppressed.ExcludedBySnippetPattern);

        var detectionSettings8 = new DetectionSettings
        {
            ExcludeClusterFileGlobs = ["**/*.cs"]
        };
        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, detectionSettings8)
                .Suppressed.ExcludedByFileGlob);

        var detectionSettings9 = new DetectionSettings
        {
            ExcludeProjectPatterns = ["Proj"]
        };
        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, detectionSettings9)
                .Suppressed.ExcludedByProjectPattern);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void GroupRejectedByBothPassesIsCountedOnce()
    {
        var detectionSettings2 = new DetectionSettings
        {
            MinLines = 1,
            MinFileSpread = 9,
            MinProjectSpread = 1,
            Similarity = 0.5
        };
        var outcome = DuplicateDetector.DetectDetailed(
            SuppressionFixtures.Blocks(3, 1),
detectionSettings2,
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.BelowFileSpread);
        Assert.Equal(1, outcome.Suppressed.Total);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void WidelySpreadExactClusterIsNeverWithheldByTheMaximums()
    {
        var settings = DetectionSettings.Default with
        {
            MinLines = 1,
            MinProjectSpread = 1
        };
        var outcome = DuplicateDetector.DetectDetailed(SuppressionFixtures.Blocks(25, 25), settings, CliqueBudget.Default);

        var cluster = Assert.Single(outcome.Clusters);

        Assert.True(cluster.IsExact);
        Assert.Equal(25, cluster.Metrics.FileSpread);
        Assert.True(cluster.Metrics.FileSpread > settings.MaxFileSpread);
        Assert.Equal(0, outcome.Suppressed.AboveFileSpread);
        Assert.Equal(0, outcome.Suppressed.AboveOccurrences);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void WiderClusterIsNotSuppressedByNarrowerOne()
    {
        var narrow = SuppressionFixtures.Cluster("x", "n", 1, 20);
        var wide = SuppressionFixtures.WideCluster("y", "w", 5, 9);

        Assert.Equal(2, ClusterFilters.SuppressContained([narrow, wide]).Count);
    }
}
