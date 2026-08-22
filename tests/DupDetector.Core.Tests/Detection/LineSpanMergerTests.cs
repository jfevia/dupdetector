using DupDetector.Core.Model;
using DupDetector.Core.Scoring;
using System.Globalization;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     
/// </summary>
public class LineSpanMergerTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void CountDistinctLines_IsZeroForNoRanges()
    {
        Assert.Equal(0, LineSpanMerger.CountDistinctLines([]));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="first">Inclusive line range written as "start-end".</param>
    /// <param name="second">Inclusive line range written as "start-end".</param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("1-5", "6-10", 10)]
    [InlineData("1-5", "7-10", 9)]
    [InlineData("1-10", "3-5", 10)]
    [InlineData("1-5", "1-5", 5)]
    public void CountDistinctLines_MergesOverlappingAndTouchingRanges(string first, string second, int expected)
    {
        Assert.Equal(expected, LineSpanMerger.CountDistinctLines([Parse(first), Parse(second)]));

        static LineRange Parse(string value)
        {
            var parts = value.Split('-');
            var lineRange = new LineRange(
                int.Parse(parts[0], CultureInfo.InvariantCulture),
                int.Parse(parts[1], CultureInfo.InvariantCulture));

            return lineRange;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void CountDistinctLines_SortsBeforeMerging()
    {
        var lineRange3 = new LineRange(6, 10);
        var lineRange4 = new LineRange(1, 5);
        Assert.Equal(10, LineSpanMerger.CountDistinctLines([lineRange3, lineRange4]));
    }
}
