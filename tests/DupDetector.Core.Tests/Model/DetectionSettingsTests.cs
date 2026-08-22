using DupDetector.Core.Model;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class DetectionSettingsTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Bounds_AcceptLegalValues()
    {
        var settings = new DetectionSettings
        {
            MinLines = 1,
            Similarity = 0.0,
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MaxFileSpread = 0,
            MaxOccurrences = 0,
            MinProductionDuplicateLines = 1,
            Kinds = DetectionKind.Methods | DetectionKind.Accessors,
            IsExcludeTestFiles = true,
            ExcludeFileGlobs = ["**/obj/**"],
            ExcludeSnippetPatterns = ["IArchRule"],
            ExcludeClusterFileGlobs = ["**/Arch/*.cs"],
            ExcludeProjectPatterns = [".Architecture."],
        };

        Assert.Equal(1, settings.MinLines);
        Assert.Equal(0.0, settings.Similarity);
        Assert.Equal(0, settings.MaxFileSpread);
        Assert.True(settings.IsExcludeTestFiles);
        Assert.Single(settings.ExcludeFileGlobs);
        Assert.Single(settings.ExcludeSnippetPatterns);
        Assert.Single(settings.ExcludeClusterFileGlobs);
        Assert.Single(settings.ExcludeProjectPatterns);
        Assert.Equal(DetectionKind.Methods | DetectionKind.Accessors, settings.Kinds);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Default_MatchesTheDocumentedDefaults()
    {
        var settings = DetectionSettings.Default;
        Assert.Equal(5, settings.MinLines);
        Assert.Equal(0.90, settings.Similarity);
        Assert.Equal(2, settings.MinFileSpread);
        Assert.Equal(2, settings.MinProjectSpread);
        Assert.Equal(20, settings.MaxFileSpread);
        Assert.Equal(50, settings.MaxOccurrences);
        Assert.Equal(10, settings.MinProductionDuplicateLines);
        Assert.Equal(DetectionKind.All, settings.Kinds);
        Assert.False(settings.IsExcludeTestFiles);
        Assert.Empty(settings.ExcludeFileGlobs);
        Assert.Empty(settings.ExcludeSnippetPatterns);
        Assert.Empty(settings.ExcludeClusterFileGlobs);
        Assert.Empty(settings.ExcludeProjectPatterns);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MinLines_RejectsZeroAndNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildMinLines(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildMinLines(-1));

        static DetectionSettings BuildMinLines(int minLines)
        {
            var settings = new DetectionSettings
            {
                MinLines = minLines
            };

            return settings;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Similarity_RejectsValuesOutsideZeroToOne(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BuildSimilarity(value));

        static DetectionSettings BuildSimilarity(double similarity)
        {
            var settings = new DetectionSettings
            {
                Similarity = similarity
            };

            return settings;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SpreadBounds_RejectIllegalValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(BuildMinFileSpread);
        Assert.Throws<ArgumentOutOfRangeException>(BuildMinProjectSpread);
        Assert.Throws<ArgumentOutOfRangeException>(BuildMaxFileSpread);
        Assert.Throws<ArgumentOutOfRangeException>(BuildMaxOccurrences);
        Assert.Throws<ArgumentOutOfRangeException>(BuildMinProductionDuplicateLines);

        static DetectionSettings BuildMinFileSpread()
        {
            var settings = new DetectionSettings
            {
                MinFileSpread = 0
            };

            return settings;
        }

        static DetectionSettings BuildMinProjectSpread()
        {
            var settings = new DetectionSettings
            {
                MinProjectSpread = 0
            };

            return settings;
        }

        static DetectionSettings BuildMaxFileSpread()
        {
            var settings = new DetectionSettings
            {
                MaxFileSpread = -1
            };

            return settings;
        }

        static DetectionSettings BuildMaxOccurrences()
        {
            var settings = new DetectionSettings
            {
                MaxOccurrences = -1
            };

            return settings;
        }

        static DetectionSettings BuildMinProductionDuplicateLines()
        {
            var settings = new DetectionSettings
            {
                MinProductionDuplicateLines = 0
            };

            return settings;
        }
    }
}
