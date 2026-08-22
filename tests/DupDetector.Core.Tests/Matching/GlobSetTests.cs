using DupDetector.Core.Matching;
using Xunit;

namespace DupDetector.Core.Tests.Matching;

/// <summary>
///     
/// </summary>
public class GlobSetTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Empty_MatchesNothing()
    {
        Assert.Equal(0, GlobSet.Empty.Count);
        Assert.False(GlobSet.Empty.IsMatch("C:/a/F.cs"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Parse_CombinesPatternsAsOr()
    {
        var set = GlobSets.Parse(["**/obj/**", "**/*.g.cs"]);
        Assert.Equal(2, set.Count);
        Assert.True(set.IsMatch("C:/a/obj/F.cs"));
        Assert.True(set.IsMatch("C:/a/F.g.cs"));
        Assert.False(set.IsMatch("C:/a/F.cs"));
    }
}
