using DupDetector.Core.Model;
using DupDetector.Core.Scoring;
using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     Covers merging of duplicated line ranges.
/// </summary>
public class LineSpanMergerTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EmptyInputMergesToNothing()
    {
        Assert.Empty(LineSpanMerger.Merge([]));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OverlappingAndTouchingRangesCollapseButDisjointOnesDoNot()
    {
        var lineRange5 = new LineRange(1, 5);
        var lineRange6 = new LineRange(4, 8);
        var lineRange7 = new LineRange(9, 10);
        var lineRange8 = new LineRange(20, 22);
        var merged = LineSpanMerger.Merge(
            [lineRange5, lineRange6, lineRange7, lineRange8]);

        Assert.Equal(2, merged.Count);
        var lineRange9 = new LineRange(1, 10);
        Assert.Equal(lineRange9, merged[0]);
        var lineRange10 = new LineRange(20, 22);
        Assert.Equal(lineRange10, merged[1]);
    }
}
