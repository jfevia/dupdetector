using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Scoring;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

public class CliqueGrouperTests
{
    private static SimilarPair Pair(int left, int right) => new(left, right, 1.0);

    private static IReadOnlyList<int[]> Members(IEnumerable<SimilarityGroup> groups) =>
        [.. groups.Select(group => group.Members.ToArray())];

    [Fact]
    public void Group_RejectsInvalidInput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(-1, [], CliqueBudget.Default));
        Assert.Throws<ArgumentNullException>(() => CliqueGrouper.Group(2, null!, CliqueBudget.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(1, [Pair(0, 5)], CliqueBudget.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => CliqueGrouper.Group(1, [Pair(5, 0)], CliqueBudget.Default));
    }

    [Fact]
    public void Group_ReturnsNothingWithoutPairs() =>
        Assert.Empty(CliqueGrouper.Group(3, [], CliqueBudget.Default));

    [Fact]
    public void SimilarityIsNotTransitive_SoAChainBecomesTwoGroups()
    {
        // A~B and B~C hold, but A and C are not similar. Connectivity alone would report {A,B,C}.
        var groups = CliqueGrouper.Group(3, [Pair(0, 1), Pair(1, 2)], CliqueBudget.Default);

        Assert.Equal([[0, 1], [1, 2]], Members(groups));
        Assert.All(groups, group => Assert.True(group.IsCohesive));
    }

    [Fact]
    public void AFullyConnectedSetBecomesOneGroup()
    {
        var groups = CliqueGrouper.Group(3, [Pair(0, 1), Pair(1, 2), Pair(0, 2)], CliqueBudget.Default);

        var group = Assert.Single(groups);
        Assert.Equal([0, 1, 2], group.Members);
        Assert.True(group.IsCohesive);
    }

    [Fact]
    public void ABlockMayBelongToSeveralGroups()
    {
        // 1 is similar to both 0 and 2, which are not similar to each other.
        var groups = CliqueGrouper.Group(4, [Pair(0, 1), Pair(1, 2), Pair(2, 3), Pair(1, 3)], CliqueBudget.Default);

        Assert.Contains([0, 1], Members(groups));
        Assert.Contains([1, 2, 3], Members(groups));
    }

    [Fact]
    public void DisconnectedComponentsAreGroupedIndependently()
    {
        var groups = CliqueGrouper.Group(4, [Pair(0, 1), Pair(2, 3)], CliqueBudget.Default);

        Assert.Equal([[0, 1], [2, 3]], Members(groups));
    }

    [Fact]
    public void AComponentLargerThanTheBudgetFallsBackAndIsFlagged()
    {
        // A ring of five: connected, but no clique larger than a pair.
        SimilarPair[] ring = [Pair(0, 1), Pair(1, 2), Pair(2, 3), Pair(3, 4), Pair(0, 4)];

        var group = Assert.Single(CliqueGrouper.Group(5, ring, new CliqueBudget(MaxGroupSize: 3, MaxWork: 10_000)));

        Assert.Equal([0, 1, 2, 3, 4], group.Members);
        Assert.False(group.IsCohesive);
    }

    [Fact]
    public void ExhaustingTheWorkBudgetFallsBackAndIsFlagged()
    {
        SimilarPair[] ring = [Pair(0, 1), Pair(1, 2), Pair(2, 3), Pair(3, 4), Pair(0, 4)];

        var group = Assert.Single(CliqueGrouper.Group(5, ring, new CliqueBudget(MaxGroupSize: 64, MaxWork: 1)));

        Assert.Equal([0, 1, 2, 3, 4], group.Members);
        Assert.False(group.IsCohesive);
    }

    [Fact]
    public void BlocksWithNoSimilarPairsAreIgnored()
    {
        // Blocks 2, 3 and 4 never appear in a pair, so they form no group.
        var groups = CliqueGrouper.Group(5, [Pair(0, 1)], CliqueBudget.Default);

        Assert.Equal([[0, 1]], Members(groups));
    }

    [Fact]
    public void ExhaustingTheWorkBudgetMidEnumerationFallsBack()
    {
        // A four-way clique needs more expansion steps than the budget allows, and the budget is
        // large enough to be consumed inside the recursion rather than on entry.
        SimilarPair[] complete = [Pair(0, 1), Pair(0, 2), Pair(0, 3), Pair(1, 2), Pair(1, 3), Pair(2, 3)];

        var group = Assert.Single(CliqueGrouper.Group(4, complete, new CliqueBudget(MaxGroupSize: 64, MaxWork: 3)));

        Assert.Equal([0, 1, 2, 3], group.Members);
        Assert.False(group.IsCohesive);
    }

    [Fact]
    public void Group_IsDeterministic()
    {
        SimilarPair[] pairs = [Pair(0, 1), Pair(1, 2), Pair(2, 3), Pair(0, 3), Pair(0, 2)];

        Assert.Equal(
            Members(CliqueGrouper.Group(4, pairs, CliqueBudget.Default)),
            Members(CliqueGrouper.Group(4, pairs, CliqueBudget.Default)));
    }

    [Fact]
    public void EveryReportedGroupIsFullyConnected()
    {
        var random = new Random(20260822);
        var edges = new List<(int Left, int Right)>();
        for (var index = 0; index < 120; index++)
        {
            var left = random.Next(0, 18);
            var right = random.Next(0, 18);
            var edge = left < right ? (left, right) : (right, left);
            if (left != right && !edges.Contains(edge))
            {
                edges.Add(edge);
            }
        }

        var pairs = edges.Select(edge => Pair(edge.Left, edge.Right)).ToArray();
        var groups = CliqueGrouper.Group(18, pairs, CliqueBudget.Default);

        Assert.NotEmpty(groups);
        foreach (var group in groups.Where(group => group.IsCohesive))
        {
            foreach (var left in group.Members)
            {
                foreach (var right in group.Members.Where(right => right != left))
                {
                    Assert.Contains(left < right ? (left, right) : (right, left), edges);
                }
            }
        }
    }

    [Fact]
    public void DefaultBudgetIsPublished()
    {
        Assert.Equal(64, CliqueBudget.Default.MaxGroupSize);
        Assert.Equal(20_000, CliqueBudget.Default.MaxWork);
    }
}

public class ClusterScoreTests
{
    [Fact]
    public void For_RejectsNegativeInput() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ClusterScore.For(-1));

    [Fact]
    public void For_RejectsNullMetrics()
    {
        ClusterMetrics? metrics = null;
        Assert.Throws<ArgumentNullException>(() => ClusterScore.For(metrics!));
    }

    [Fact]
    public void NoRemovableLinesScoresZero() => Assert.Equal(0.0, ClusterScore.For(0));

    [Fact]
    public void TheAnchorScoresOneHundred() => Assert.Equal(100.0, ClusterScore.For(ClusterScore.Anchor));

    [Fact]
    public void BeyondTheAnchorTheScoreIsCapped() => Assert.Equal(100.0, ClusterScore.For(ClusterScore.Anchor * 100));

    [Theory]
    // The two clusters that tied at 1.15 under the old product formula now separate.
    [InlineData(36, 52.3)]
    [InlineData(66, 60.9)]
    public void ScoreFollowsTheDocumentedCurve(int removableLines, double expected) =>
        Assert.Equal(expected, ClusterScore.For(removableLines), 1);

    [Fact]
    public void SizeAndReachNoLongerCollapseToTheSameScore()
    {
        var wide = ClusterScore.For(new ClusterMetrics(6, 12, 12, 2, true));
        var deep = ClusterScore.For(new ClusterMetrics(36, 2, 2, 2, true));

        Assert.NotEqual(wide, deep);
        Assert.True(wide > deep);
    }

    [Fact]
    public void ScoreRisesWithRemovableLines() =>
        Assert.True(ClusterScore.For(500) > ClusterScore.For(100));

    [Fact]
    public void FormulaIsDerivedFromTheAnchorRatherThanRestated() =>
        Assert.Equal($"100 * ln(1 + removableLines) / ln(1 + {ClusterScore.Anchor})", ClusterScore.Formula);
}

public class DetectorCliqueIntegrationTests
{
    private static readonly DetectionSettings Permissive = new()
    {
        MinFileSpread = 1,
        MinProjectSpread = 1,
        MinLines = 1,
        Similarity = 0.6,
    };

    [Fact]
    public void ChainedNearDuplicatesDoNotMergeIntoOneCluster()
    {
        // Distinct token sets: 0 and 2 share nothing, so they must not end up together.
        var blocks = new[]
        {
            Code.Block("a a a a b b", path: "/0.cs", hash: "h0"),
            Code.Block("b b b a a c", path: "/1.cs", hash: "h1"),
            Code.Block("c c c b b b", path: "/2.cs", hash: "h2"),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.35 });

        Assert.All(clusters, cluster => Assert.True(cluster.IsCohesive));
        Assert.DoesNotContain(clusters, cluster =>
            cluster.Instances.Any(i => i.FilePath == "/0.cs") &&
            cluster.Instances.Any(i => i.FilePath == "/2.cs"));
    }

    [Fact]
    public void ExactClustersAreAlwaysCohesive()
    {
        var cluster = Assert.Single(DuplicateDetector.Detect(
            [Code.Block("a b c", path: "/1.cs", hash: "same"), Code.Block("a b c", path: "/2.cs", hash: "same")],
            Permissive));

        Assert.True(cluster.IsCohesive);
        Assert.True(cluster.IsExact);
    }

    [Fact]
    public void ABudgetedRunStillProducesClustersAndFlagsThem()
    {
        var blocks = Enumerable.Range(0, 6)
            .Select(index => Code.Block($"shared shared t{index} t{(index + 1) % 6}", path: $"/{index}.cs", hash: $"h{index}"))
            .ToArray();

        var clusters = DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.3 }, new CliqueBudget(2, 10_000));

        Assert.NotEmpty(clusters);
        Assert.Contains(clusters, cluster => !cluster.IsCohesive);
    }
}
