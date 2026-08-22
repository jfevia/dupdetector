using DupDetector.Core.Model;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     Covers classification of physical lines as code or not.
/// </summary>
public class CodeLineMapTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnEmptyFileHasNoCodeLines()
    {
        var unit = Code.Unit(string.Empty);

        Assert.Equal(0, CodeLineMaps.Create(unit.Tree, 0).Total);
        Assert.Equal(0, CodeLineMap.Empty.Total);
        var lineRange = new LineRange(1, 10);
        Assert.Equal(0, CodeLineMap.Empty.CountIn(lineRange));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BlanksAndCommentsAreNotCodeButStringContentIs()
    {
        const string source = """
            // leading comment

            class C
            {
                string M() => "// not a comment";
            }
            """;

        var unit = Code.Unit(source);
        var map = CodeLineMaps.Create(unit.Tree, LineCounter.Count(source));

        Assert.Equal(4, map.Total);
        var lineRange2 = new LineRange(1, 6);
        Assert.Equal(4, map.CountIn(lineRange2));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void CountingIsClampedToTheFileRatherThanThrowing()
    {
        const string source = """
            class C
            {
                int M() => 1;
            }
            """;

        var map = CodeLineMaps.Create(Code.Unit(source).Tree, LineCounter.Count(source));

        var lineRange3 = new LineRange(1, 999);
        Assert.Equal(4, map.CountIn(lineRange3));
        var lineRange4 = new LineRange(50, 60);
        Assert.Equal(0, map.CountIn(lineRange4));
    }
}
