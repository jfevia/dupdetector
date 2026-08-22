using DupDetector.Core.Model;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class LineCounterTests
{
    /// <summary>
    ///     
    /// </summary>
    /// <param name="text"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\nb\nc\n", 3)]
    [InlineData("a\r\nb\r\nc\r\n", 3)]
    [InlineData("\n", 1)]
    public void Count_IgnoresTrailingNewline(string text, int expected)
    {
        Assert.Equal(expected, LineCounter.Count(text));
    }
}
