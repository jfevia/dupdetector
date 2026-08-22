using DupDetector.Core.Detection;
using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class DisjointSetTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Constructor_RejectsNegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(BuildNegative);

        static DisjointSet BuildNegative()
        {
            var set = new DisjointSet(-1);
            return set;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EachElementStartsInItsOwnSet()
    {
        var set = new DisjointSet(3);
        Assert.Equal(3, set.Count);
        Assert.Equal(3, set.Groups().Count);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Find_CollapsesLongChains()
    {
        var set = new DisjointSet(100);
        for (var index = 1; index < 100; index++)
        {
            set.CanUnion(index - 1, index);
        }

        Assert.Equal(set.Find(0), set.Find(99));
        Assert.Single(set.Groups());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Groups_AreEmptyForAnEmptySet()
    {
        var disjointSet2 = new DisjointSet(0);
        Assert.Empty(disjointSet2.Groups());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Union_JoinsSetsAndReportsWhetherItChangedAnything()
    {
        var set = new DisjointSet(3);
        Assert.True(set.CanUnion(0, 1));
        Assert.False(set.CanUnion(0, 1));
        Assert.Equal(set.Find(0), set.Find(1));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Union_MergesByRankInBothDirections()
    {
        var set = new DisjointSet(6);
        set.CanUnion(0, 1);
        set.CanUnion(2, 3);
        set.CanUnion(4, 5);
        set.CanUnion(0, 2);
        set.CanUnion(4, 0);

        Assert.Single(set.Groups());
        Assert.Equal([0, 1, 2, 3, 4, 5], set.Groups()[0]);
    }
}
