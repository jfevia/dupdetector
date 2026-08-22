using DupDetector.Core.Detection;
using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class TokenMultisetTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Create_CountsRepeatsAndSortsIds()
    {
        var tokenInterner = new TokenInterner();
        var multiset = TokenMultisets.Create("b a b", tokenInterner);

        Assert.Equal(3, multiset.Cardinality);
        Assert.Equal(2, multiset.Ids.Length);
        Assert.True(multiset.Ids[0] < multiset.Ids[1]);
        var total = 0;
        foreach (var count in multiset.Counts)
        {
            total += count;
        }

        Assert.Equal(3, total);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Create_HandlesEmptyText()
    {
        var tokenInterner2 = new TokenInterner();
        Assert.Equal(0, TokenMultisets.Create(string.Empty, tokenInterner2).Cardinality);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Intern_ReusesIdsForRepeatedTokens()
    {
        var interner = new TokenInterner();
        Assert.Equal(0, interner.Intern("a"));
        Assert.Equal(1, interner.Intern("b"));
        Assert.Equal(0, interner.Intern("a"));
        Assert.Equal(2, interner.Count);
    }
}
