using DupDetector.Core.Model;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class LineRangeTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Constructor_RejectsInvertedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(BuildInverted);

        static object BuildInverted()
        {
            var range = new LineRange(5, 4);
            return range;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Constructor_RejectsNonPositiveStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(BuildNonPositive);

        static object BuildNonPositive()
        {
            var range = new LineRange(0, 5);
            return range;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Count_IsInclusiveOfBothEndpoints()
    {
        var range = new LineRange(10, 12);
        Assert.Equal(10, range.Start);
        Assert.Equal(12, range.End);
        Assert.Equal(3, range.Count);
        Assert.Equal("10-12", range.ToString());
    }
}
