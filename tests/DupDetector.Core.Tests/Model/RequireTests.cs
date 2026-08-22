using DupDetector.Core.Internal;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class RequireTests
{
    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AtLeast_ReturnsValue_WhenAtOrAboveMinimum()
    {
        Assert.Equal(5, Require.AtLeast(5, 5, "x"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AtLeast_Throws_WhenBelowMinimum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Require.AtLeast(4, 5, "x"));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void InRange_ReturnsValue_WhenInsideBounds(double value)
    {
        Assert.Equal(value, Require.InRange(value, 0.0, 1.0, "x"));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void InRange_Throws_WhenOutsideBounds(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Require.InRange(value, 0.0, 1.0, "x"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NotBlank_ReturnsValue_WhenPopulated()
    {
        Assert.Equal("a", Require.NotBlank("a", "x"));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBlank_Throws_WhenBlank(string value)
    {
        Assert.Throws<ArgumentException>(() => Require.NotBlank(value, "x"));
    }
}
