using DupDetector.Core.Internal;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Core.Tests.Model;

public class RequireTests
{
    [Fact]
    public void AtLeast_ReturnsValue_WhenAtOrAboveMinimum() =>
        Assert.Equal(5, Require.AtLeast(5, 5, "x"));

    [Fact]
    public void AtLeast_Throws_WhenBelowMinimum() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Require.AtLeast(4, 5, "x"));

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void InRange_ReturnsValue_WhenInsideBounds(double value) =>
        Assert.Equal(value, Require.InRange(value, 0.0, 1.0, "x"));

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void InRange_Throws_WhenOutsideBounds(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Require.InRange(value, 0.0, 1.0, "x"));

    [Fact]
    public void NotBlank_ReturnsValue_WhenPopulated() =>
        Assert.Equal("a", Require.NotBlank("a", "x"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NotBlank_Throws_WhenBlank(string value) =>
        Assert.Throws<ArgumentException>(() => Require.NotBlank(value, "x"));
}

public class LineCounterTests
{
    [Theory]
    [InlineData("", 0)]
    [InlineData("a", 1)]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\nb\nc\n", 3)]
    [InlineData("a\r\nb\r\nc\r\n", 3)]
    [InlineData("\n", 1)]
    public void Count_IgnoresTrailingNewline(string text, int expected) =>
        Assert.Equal(expected, LineCounter.Count(text));

    [Fact]
    public void Count_Throws_WhenTextIsNull() =>
        Assert.Throws<ArgumentNullException>(() => LineCounter.Count(null!));
}

public class ProjectIdentityTests
{
    [Fact]
    public void Named_ExposesTheName()
    {
        var identity = ProjectIdentity.Named("Alpha");
        Assert.Equal("Alpha", identity.Name);
        Assert.True(identity.IsKnown);
        Assert.Equal("Alpha", identity.ToString());
    }

    [Fact]
    public void Unknown_IsADistinctState()
    {
        Assert.Null(ProjectIdentity.Unknown.Name);
        Assert.False(ProjectIdentity.Unknown.IsKnown);
        Assert.Equal("<unknown>", ProjectIdentity.Unknown.ToString());
        Assert.Equal(0, ProjectIdentity.Unknown.GetHashCode());
    }

    [Fact]
    public void Named_RejectsBlankNames() =>
        Assert.Throws<ArgumentException>(() => ProjectIdentity.Named(" "));

    [Fact]
    public void Equality_IgnoresCase()
    {
        Assert.Equal(ProjectIdentity.Named("Alpha"), ProjectIdentity.Named("ALPHA"));
        Assert.Equal(ProjectIdentity.Named("Alpha").GetHashCode(), ProjectIdentity.Named("ALPHA").GetHashCode());
    }

    [Fact]
    public void Equality_DistinguishesDifferentProjects() =>
        Assert.NotEqual(ProjectIdentity.Named("Alpha"), ProjectIdentity.Named("Beta"));

    [Fact]
    public void Equals_HandlesNullAndForeignTypes()
    {
        var identity = ProjectIdentity.Named("Alpha");
        Assert.False(identity.Equals(null));
        Assert.False(identity.Equals((object?)"Alpha"));
        Assert.True(identity.Equals((object?)ProjectIdentity.Named("alpha")));
    }

    [Fact]
    public void Operators_MatchEqualsRatherThanReferenceIdentity()
    {
        Assert.True(ProjectIdentity.Named("Alpha") == ProjectIdentity.Named("ALPHA"));
        Assert.False(ProjectIdentity.Named("Alpha") != ProjectIdentity.Named("ALPHA"));
        Assert.True(ProjectIdentity.Named("Alpha") != ProjectIdentity.Named("Beta"));

        ProjectIdentity? nothing = null;
        ProjectIdentity? alsoNothing = null;
        Assert.True(nothing == alsoNothing);
        Assert.False(nothing == ProjectIdentity.Named("Alpha"));
        Assert.False(ProjectIdentity.Named("Alpha") == nothing);
    }
}

public class LineRangeTests
{
    [Fact]
    public void Count_IsInclusiveOfBothEndpoints()
    {
        var range = new LineRange(10, 12);
        Assert.Equal(10, range.Start);
        Assert.Equal(12, range.End);
        Assert.Equal(3, range.Count);
        Assert.Equal("10-12", range.ToString());
    }

    [Fact]
    public void Constructor_RejectsNonPositiveStart() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new LineRange(0, 5));

    [Fact]
    public void Constructor_RejectsInvertedRange() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new LineRange(5, 4));
}

public class ScoreLabelTests
{
    [Theory]
    [InlineData(0, ScoreLabel.Low)]
    [InlineData(2.99, ScoreLabel.Low)]
    [InlineData(3, ScoreLabel.Medium)]
    [InlineData(9.99, ScoreLabel.Medium)]
    [InlineData(10, ScoreLabel.High)]
    [InlineData(19.99, ScoreLabel.High)]
    [InlineData(20, ScoreLabel.Critical)]
    [InlineData(100, ScoreLabel.Critical)]
    public void For_MapsPercentageToBand(double percentage, ScoreLabel expected) =>
        Assert.Equal(expected, ScoreLabels.For(percentage));
}
