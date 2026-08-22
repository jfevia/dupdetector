using DupDetector.Core.Model.Reporting;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class ScoreLabelTests
{
    /// <summary>
    ///     
    /// </summary>
    /// <param name="percentage"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(0, ScoreLabel.Low)]
    [InlineData(2.99, ScoreLabel.Low)]
    [InlineData(3, ScoreLabel.Medium)]
    [InlineData(9.99, ScoreLabel.Medium)]
    [InlineData(10, ScoreLabel.High)]
    [InlineData(19.99, ScoreLabel.High)]
    [InlineData(20, ScoreLabel.Critical)]
    [InlineData(100, ScoreLabel.Critical)]
    public void For_MapsPercentageToBand(double percentage, ScoreLabel expected)
    {
        Assert.Equal(expected, ScoreLabels.For(percentage));
    }
}
