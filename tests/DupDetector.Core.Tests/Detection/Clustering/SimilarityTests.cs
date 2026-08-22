using DupDetector.Core.Detection;
using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class SimilarityTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Jaccard_AccountsForTokenFrequency()
    {
        var interner = new TokenInterner();
        var similarity = Similarity.Jaccard(TokenFixtures.Set("a b", interner), TokenFixtures.Set("a a a a b", interner));

        Assert.True(similarity < 1.0);
        Assert.Equal(2.0 / 5.0, similarity, 10);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Jaccard_IsOneForIdenticalMultisets()
    {
        var interner = new TokenInterner();
        Assert.Equal(1.0, Similarity.Jaccard(TokenFixtures.Set("a b c", interner), TokenFixtures.Set("a b c", interner)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Jaccard_IsZeroForDisjointMultisets()
    {
        var interner = new TokenInterner();
        Assert.Equal(0.0, Similarity.Jaccard(TokenFixtures.Set("a b", interner), TokenFixtures.Set("c d", interner)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Jaccard_TreatsTwoEmptyMultisetsAsIdentical()
    {
        var interner = new TokenInterner();
        Assert.Equal(1.0, Similarity.Jaccard(TokenFixtures.Set(string.Empty, interner), TokenFixtures.Set(string.Empty, interner)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Overlap_WalksBothSidesOfTheMerge()
    {
        var interner = new TokenInterner();
        Assert.Equal(1, Similarity.Overlap(TokenFixtures.Set("a b", interner), TokenFixtures.Set("b c", interner)));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UpperBound_IsNeverBelowTheActualSimilarity()
    {
        var interner = new TokenInterner();
        var left = TokenFixtures.Set("a b c d e f g h i j", interner);
        var right = TokenFixtures.Set("a b c", interner);

        Assert.True(Similarity.UpperBound(left.Cardinality, right.Cardinality) >= Similarity.Jaccard(left, right));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(0, 0, 1.0)]
    [InlineData(10, 10, 1.0)]
    [InlineData(9, 10, 0.9)]
    [InlineData(10, 9, 0.9)]
    [InlineData(1, 10, 0.1)]
    public void UpperBound_IsTheBestAchievableSimilarityForTheseSizes(int left, int right, double expected)
    {
        Assert.Equal(expected, Similarity.UpperBound(left, right), 10);
    }
}
