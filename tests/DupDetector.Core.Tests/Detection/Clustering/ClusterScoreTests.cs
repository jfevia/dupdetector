using DupDetector.Core.Model;
using DupDetector.Core.Scoring;
using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class ClusterScoreTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BeyondTheAnchorTheScoreIsCapped()
    {
        Assert.Equal(100.0, ClusterScore.For(ClusterScore.Anchor * 100));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void For_RejectsNegativeInput()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ClusterScore.For(-1));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FormulaIsDerivedFromTheAnchorRatherThanRestated()
    {
        Assert.Equal($"100 * ln(1 + removableLines) / ln(1 + {ClusterScore.Anchor})", ClusterScore.Formula);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NoRemovableLinesScoresZero()
    {
        Assert.Equal(0.0, ClusterScore.For(0));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="removableLines"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(36, 52.3)]
    [InlineData(66, 60.9)]
    public void ScoreFollowsTheDocumentedCurve(int removableLines, double expected)
    {
        Assert.Equal(expected, ClusterScore.For(removableLines), 1);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ScoreRisesWithRemovableLines()
    {
        Assert.True(ClusterScore.For(500) > ClusterScore.For(100));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SizeAndReachNoLongerCollapseToTheSameScore()
    {
        var clusterSpread = new ClusterSpread(12, 2, true);
        var clusterMetrics = new ClusterMetrics(6, 12, clusterSpread);
        var wide = ClusterScore.For(clusterMetrics);
        var clusterSpread2 = new ClusterSpread(2, 2, true);
        var clusterMetrics2 = new ClusterMetrics(36, 2, clusterSpread2);
        var deep = ClusterScore.For(clusterMetrics2);

        Assert.NotEqual(wide, deep);
        Assert.True(wide > deep);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TheAnchorScoresOneHundred()
    {
        Assert.Equal(100.0, ClusterScore.For(ClusterScore.Anchor));
    }
}
