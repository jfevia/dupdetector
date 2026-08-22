using DupDetector.Core.Detection;
using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class SimilarityJoinTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FindPairs_AgreesWithExhaustiveComparison()
    {
        var interner = new TokenInterner();
        var random = new Random(20260822);
        var blocks = new List<TokenMultiset>(120);
        for (var block = 0; block < 120; block++)
        {
            var tokens = new List<string>();
            var tokenCount = random.Next(4, 14);
            for (var token = 0; token < tokenCount; token++)
            {
                tokens.Add("t" + random.Next(0, 20));
            }

            blocks.Add(TokenMultisets.Create(string.Join(' ', tokens), interner));
        }

        const double Threshold = 0.55;

        var expected = new List<Edge>();
        for (var left = 0; left < blocks.Count; left++)
        {
            for (var right = left + 1; right < blocks.Count; right++)
            {
                if (Similarity.Jaccard(blocks[left], blocks[right]) >= Threshold)
                {
                    var edge = new Edge(left, right);
                    expected.Add(edge);
                }
            }
        }

        var actual = new List<Edge>();
        foreach (var pair in SimilarityJoin.FindPairs(blocks, Threshold))
        {
            var edge = new Edge(pair.Left, pair.Right);
            actual.Add(edge);
        }

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FindPairs_FindsEveryQualifyingPair()
    {
        var interner = new TokenInterner();
        var blocks = new[]
        {
            TokenMultisets.Create("a b c d", interner),
            TokenMultisets.Create("a b c d", interner),
            TokenMultisets.Create("x y z w", interner),
        };

        var pairs = SimilarityJoin.FindPairs(blocks, 0.9);

        var pair = Assert.Single(pairs);
        Assert.Equal(0, pair.Left);
        Assert.Equal(1, pair.Right);
        Assert.Equal(1.0, pair.Similarity);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FindPairs_IsOrderedIndependentlyOfScheduling()
    {
        var interner = new TokenInterner();
        var blocks = new List<TokenMultiset>(60);
        for (var index = 0; index < 60; index++)
        {
            blocks.Add(TokenMultisets.Create($"shared common a{index % 5} b{index % 5}", interner));
        }

        var first = SimilarityJoin.FindPairs(blocks, 0.5);
        var second = SimilarityJoin.FindPairs(blocks, 0.5);

        Assert.Equal(first, second);
        var sorted = new List<SimilarPair>(first);
        sorted.Sort(static (left, right) =>
            left.Left == right.Left ? left.Right.CompareTo(right.Right) : left.Left.CompareTo(right.Left));
        Assert.Equal(sorted, first);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FindPairs_ReturnsNothingBelowTwoBlocks()
    {
        var interner = new TokenInterner();
        Assert.Empty(SimilarityJoin.FindPairs([], 0.9));
        Assert.Empty(SimilarityJoin.FindPairs([TokenMultisets.Create("a", interner)], 0.9));
    }
}
