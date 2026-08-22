using DupDetector.Core.Detection;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

public class TokenMultisetTests
{
    [Fact]
    public void Intern_ReusesIdsForRepeatedTokens()
    {
        var interner = new TokenInterner();
        Assert.Equal(0, interner.Intern("a"));
        Assert.Equal(1, interner.Intern("b"));
        Assert.Equal(0, interner.Intern("a"));
        Assert.Equal(2, interner.Count);
    }

    [Fact]
    public void Create_CountsRepeatsAndSortsIds()
    {
        var multiset = TokenMultiset.Create("b a b", new TokenInterner());

        Assert.Equal(3, multiset.Cardinality);
        Assert.Equal(2, multiset.Ids.Length);
        Assert.True(multiset.Ids[0] < multiset.Ids[1]);
        Assert.Equal(3, multiset.Counts.Sum());
    }

    [Fact]
    public void Create_HandlesEmptyText() =>
        Assert.Equal(0, TokenMultiset.Create(string.Empty, new TokenInterner()).Cardinality);

    [Fact]
    public void Create_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => TokenMultiset.Create(null!, new TokenInterner()));
        Assert.Throws<ArgumentNullException>(() => TokenMultiset.Create("a", null!));
    }
}

public class SimilarityTests
{
    private static TokenMultiset Set(string text, TokenInterner interner) => TokenMultiset.Create(text, interner);

    [Fact]
    public void Jaccard_IsOneForIdenticalMultisets()
    {
        var interner = new TokenInterner();
        Assert.Equal(1.0, Similarity.Jaccard(Set("a b c", interner), Set("a b c", interner)));
    }

    [Fact]
    public void Jaccard_IsZeroForDisjointMultisets()
    {
        var interner = new TokenInterner();
        Assert.Equal(0.0, Similarity.Jaccard(Set("a b", interner), Set("c d", interner)));
    }

    [Fact]
    public void Jaccard_TreatsTwoEmptyMultisetsAsIdentical()
    {
        var interner = new TokenInterner();
        Assert.Equal(1.0, Similarity.Jaccard(Set(string.Empty, interner), Set(string.Empty, interner)));
    }

    [Fact]
    public void Jaccard_AccountsForTokenFrequency()
    {
        // Set semantics would call these identical; multiset semantics do not.
        var interner = new TokenInterner();
        var similarity = Similarity.Jaccard(Set("a b", interner), Set("a a a a b", interner));

        Assert.True(similarity < 1.0);
        Assert.Equal(2.0 / 5.0, similarity, 10);
    }

    [Fact]
    public void Overlap_WalksBothSidesOfTheMerge()
    {
        var interner = new TokenInterner();
        // "a" only on the left, "c" only on the right, "b" on both.
        Assert.Equal(1, Similarity.Overlap(Set("a b", interner), Set("b c", interner)));
    }

    [Fact]
    public void Overlap_RejectsNullArguments()
    {
        var interner = new TokenInterner();
        Assert.Throws<ArgumentNullException>(() => Similarity.Overlap(null!, Set("a", interner)));
        Assert.Throws<ArgumentNullException>(() => Similarity.Overlap(Set("a", interner), null!));
    }

    [Theory]
    [InlineData(0, 0, 1.0)]
    [InlineData(10, 10, 1.0)]
    [InlineData(9, 10, 0.9)]
    [InlineData(10, 9, 0.9)]
    [InlineData(1, 10, 0.1)]
    public void UpperBound_IsTheBestAchievableSimilarityForTheseSizes(int a, int b, double expected) =>
        Assert.Equal(expected, Similarity.UpperBound(a, b), 10);

    [Fact]
    public void UpperBound_IsNeverBelowTheActualSimilarity()
    {
        // The pruning guarantee: if the bound is below the threshold, the pair truly cannot qualify.
        var interner = new TokenInterner();
        var left = Set("a b c d e f g h i j", interner);
        var right = Set("a b c", interner);

        Assert.True(Similarity.UpperBound(left.Cardinality, right.Cardinality) >= Similarity.Jaccard(left, right));
    }
}

public class DisjointSetTests
{
    [Fact]
    public void Constructor_RejectsNegativeSize() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DisjointSet(-1));

    [Fact]
    public void EachElementStartsInItsOwnSet()
    {
        var set = new DisjointSet(3);
        Assert.Equal(3, set.Count);
        Assert.Equal(3, set.Groups().Count);
    }

    [Fact]
    public void Union_JoinsSetsAndReportsWhetherItChangedAnything()
    {
        var set = new DisjointSet(3);
        Assert.True(set.Union(0, 1));
        Assert.False(set.Union(0, 1));
        Assert.Equal(set.Find(0), set.Find(1));
    }

    [Fact]
    public void Union_MergesByRankInBothDirections()
    {
        var set = new DisjointSet(6);
        set.Union(0, 1);
        set.Union(2, 3);
        set.Union(4, 5);
        // Joining two trees of equal rank raises the winner's rank.
        set.Union(0, 2);
        // Joining a taller tree to a shorter one exercises the swap.
        set.Union(4, 0);

        Assert.Single(set.Groups());
        Assert.Equal([0, 1, 2, 3, 4, 5], set.Groups()[0]);
    }

    [Fact]
    public void Find_CollapsesLongChains()
    {
        var set = new DisjointSet(100);
        for (var index = 1; index < 100; index++)
        {
            set.Union(index - 1, index);
        }

        Assert.Equal(set.Find(0), set.Find(99));
        Assert.Single(set.Groups());
    }

    [Fact]
    public void Groups_AreEmptyForAnEmptySet() =>
        Assert.Empty(new DisjointSet(0).Groups());
}

public class SimilarityJoinTests
{
    [Fact]
    public void FindPairs_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => SimilarityJoin.FindPairs(null!, 0.9));

    [Fact]
    public void FindPairs_ReturnsNothingBelowTwoBlocks()
    {
        var interner = new TokenInterner();
        Assert.Empty(SimilarityJoin.FindPairs([], 0.9));
        Assert.Empty(SimilarityJoin.FindPairs([TokenMultiset.Create("a", interner)], 0.9));
    }

    [Fact]
    public void FindPairs_FindsEveryQualifyingPair()
    {
        var interner = new TokenInterner();
        var blocks = new[]
        {
            TokenMultiset.Create("a b c d", interner),
            TokenMultiset.Create("a b c d", interner),
            TokenMultiset.Create("x y z w", interner),
        };

        var pairs = SimilarityJoin.FindPairs(blocks, 0.9);

        var pair = Assert.Single(pairs);
        Assert.Equal(0, pair.Left);
        Assert.Equal(1, pair.Right);
        Assert.Equal(1.0, pair.Similarity);
    }

    [Fact]
    public void FindPairs_AgreesWithExhaustiveComparison()
    {
        // The completeness guarantee: index-driven pruning must lose nothing.
        var interner = new TokenInterner();
        var random = new Random(20260822);
        var blocks = Enumerable.Range(0, 120)
            .Select(_ => TokenMultiset.Create(
                string.Join(' ', Enumerable.Range(0, random.Next(4, 14)).Select(_ => "t" + random.Next(0, 20))),
                interner))
            .ToArray();

        const double Threshold = 0.55;

        var expected = new List<(int, int)>();
        for (var left = 0; left < blocks.Length; left++)
        {
            for (var right = left + 1; right < blocks.Length; right++)
            {
                if (Similarity.Jaccard(blocks[left], blocks[right]) >= Threshold)
                {
                    expected.Add((left, right));
                }
            }
        }

        var actual = SimilarityJoin.FindPairs(blocks, Threshold).Select(pair => (pair.Left, pair.Right)).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FindPairs_IsOrderedIndependentlyOfScheduling()
    {
        var interner = new TokenInterner();
        var blocks = Enumerable.Range(0, 60)
            .Select(index => TokenMultiset.Create($"shared common a{index % 5} b{index % 5}", interner))
            .ToArray();

        var first = SimilarityJoin.FindPairs(blocks, 0.5);
        var second = SimilarityJoin.FindPairs(blocks, 0.5);

        Assert.Equal(first, second);
        Assert.Equal(first.OrderBy(p => p.Left).ThenBy(p => p.Right), first);
    }
}
