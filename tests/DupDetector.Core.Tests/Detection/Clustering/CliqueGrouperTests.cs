using DupDetector.Core.Detection;

using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class CliqueGrouperTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BlockMayBelongToSeveralGroups()
    {
        var groups = CliqueGrouper.Group(4, [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(2, 3), CliqueFixtures.Pair(1, 3)], CliqueBudget.Default);

        Assert.Contains([0, 1], CliqueFixtures.Members(groups));
        Assert.Contains([1, 2, 3], CliqueFixtures.Members(groups));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BlocksWithNoSimilarPairsAreIgnored()
    {
        var groups = CliqueGrouper.Group(5, [CliqueFixtures.Pair(0, 1)], CliqueBudget.Default);

        Assert.Equal([[0, 1]], CliqueFixtures.Members(groups));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ComponentLargerThanTheBudgetFallsBackAndIsFlagged()
    {
        SimilarPair[] ring = [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(2, 3), CliqueFixtures.Pair(3, 4), CliqueFixtures.Pair(0, 4)];

        var cliqueBudget = new CliqueBudget(3, 10_000);
        var group = Assert.Single(CliqueGrouper.Group(5, ring, cliqueBudget));

        Assert.Equal([0, 1, 2, 3, 4], group.Members);
        Assert.False(group.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DefaultBudgetIsPublished()
    {
        Assert.Equal(64, CliqueBudget.Default.MaxGroupSize);
        Assert.Equal(20_000, CliqueBudget.Default.MaxWork);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DisconnectedComponentsAreGroupedIndependently()
    {
        var groups = CliqueGrouper.Group(4, [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(2, 3)], CliqueBudget.Default);

        Assert.Equal([[0, 1], [2, 3]], CliqueFixtures.Members(groups));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryReportedGroupIsFullyConnected()
    {
        var edges = CliqueFixtures.RandomEdges(120, 18);
        var pairs = new List<SimilarPair>(edges.Count);
        foreach (var edge in edges)
        {
            pairs.Add(CliqueFixtures.Pair(edge.Left, edge.Right));
        }

        var groups = CliqueGrouper.Group(18, pairs, CliqueBudget.Default);

        Assert.NotEmpty(groups);
        foreach (var group in groups)
        {
            if (group.IsCohesive)
            {
                CliqueFixtures.AssertFullyConnected(group.Members, edges);
            }
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExhaustingTheWorkBudgetFallsBackAndIsFlagged()
    {
        SimilarPair[] ring = [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(2, 3), CliqueFixtures.Pair(3, 4), CliqueFixtures.Pair(0, 4)];

        var cliqueBudget2 = new CliqueBudget(64, 1);
        var group = Assert.Single(CliqueGrouper.Group(5, ring, cliqueBudget2));

        Assert.Equal([0, 1, 2, 3, 4], group.Members);
        Assert.False(group.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExhaustingTheWorkBudgetMidEnumerationFallsBack()
    {
        SimilarPair[] complete = [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(0, 2), CliqueFixtures.Pair(0, 3), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(1, 3), CliqueFixtures.Pair(2, 3)];

        var cliqueBudget3 = new CliqueBudget(64, 3);
        var group = Assert.Single(CliqueGrouper.Group(4, complete, cliqueBudget3));

        Assert.Equal([0, 1, 2, 3], group.Members);
        Assert.False(group.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FullyConnectedSetBecomesOneGroup()
    {
        var groups = CliqueGrouper.Group(3, [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(0, 2)], CliqueBudget.Default);

        var group = Assert.Single(groups);
        Assert.Equal([0, 1, 2], group.Members);
        Assert.True(group.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Group_IsDeterministic()
    {
        SimilarPair[] pairs = [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2), CliqueFixtures.Pair(2, 3), CliqueFixtures.Pair(0, 3), CliqueFixtures.Pair(0, 2)];

        Assert.Equal(
            CliqueFixtures.Members(CliqueGrouper.Group(4, pairs, CliqueBudget.Default)),
            CliqueFixtures.Members(CliqueGrouper.Group(4, pairs, CliqueBudget.Default)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Group_RejectsInvalidInput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(-1, [], CliqueBudget.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(1, [CliqueFixtures.Pair(0, 5)], CliqueBudget.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(1, [CliqueFixtures.Pair(5, 0)], CliqueBudget.Default));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Group_ReturnsNothingWithoutPairs()
    {
        Assert.Empty(CliqueGrouper.Group(3, [], CliqueBudget.Default));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SimilarityIsNotTransitive_SoOneChainBecomesTwoGroups()
    {
        var groups = CliqueGrouper.Group(3, [CliqueFixtures.Pair(0, 1), CliqueFixtures.Pair(1, 2)], CliqueBudget.Default);

        Assert.Equal([[0, 1], [1, 2]], CliqueFixtures.Members(groups));
        Assert.All(groups, group => Assert.True(group.IsCohesive));
    }
}
