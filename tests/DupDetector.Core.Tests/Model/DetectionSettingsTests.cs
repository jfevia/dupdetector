using DupDetector.Core.Model;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Model;

public class DetectionSettingsTests
{
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
        Assert.False(settings.ExcludeTestFiles);
        Assert.Empty(settings.ExcludeFileGlobs);
        Assert.Empty(settings.ExcludeSnippetPatterns);
        Assert.Empty(settings.ExcludeClusterFileGlobs);
        Assert.Empty(settings.ExcludeProjectPatterns);
    }

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
            ExcludeTestFiles = true,
            ExcludeFileGlobs = ["**/obj/**"],
            ExcludeSnippetPatterns = ["IArchRule"],
            ExcludeClusterFileGlobs = ["**/Arch/*.cs"],
            ExcludeProjectPatterns = [".Architecture."],
        };

        Assert.Equal(1, settings.MinLines);
        Assert.Equal(0.0, settings.Similarity);
        Assert.Equal(0, settings.MaxFileSpread);
        Assert.True(settings.ExcludeTestFiles);
        Assert.Single(settings.ExcludeFileGlobs);
        Assert.Single(settings.ExcludeSnippetPatterns);
        Assert.Single(settings.ExcludeClusterFileGlobs);
        Assert.Single(settings.ExcludeProjectPatterns);
        Assert.Equal(DetectionKind.Methods | DetectionKind.Accessors, settings.Kinds);
    }

    [Fact]
    public void MinLines_RejectsZeroAndNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinLines = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinLines = -1 });
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Similarity_RejectsValuesOutsideZeroToOne(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { Similarity = value });

    [Fact]
    public void SpreadBounds_RejectIllegalValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinFileSpread = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinProjectSpread = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MaxFileSpread = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MaxOccurrences = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinProductionDuplicateLines = 0 });
    }
}

public class ModelProjectionTests
{
    [Fact]
    public void SourceUnit_ToFile_CarriesClassificationAndLineCount()
    {
        var unit = Code.Unit("class C\n{\n}\n", path: "/repo/a/C.cs", project: "Alpha", isTestFile: true);
        var file = unit.ToFile();

        Assert.Equal("/repo/a/C.cs", file.Path);
        Assert.Equal("repo/a/C.cs", file.RelativePath);
        Assert.Equal(ProjectIdentity.Named("Alpha"), file.Project);
        Assert.Equal(3, file.LineCount);
        Assert.True(file.IsTestFile);
    }

    [Fact]
    public void CodeBlock_ToInstance_PreservesIdentity()
    {
        var block = Code.Block("var0", path: "/repo/B.cs", project: "Beta", isTestFile: true, hash: "abc", memberName: "M");
        var instance = block.ToInstance();

        Assert.Equal("/repo/B.cs", instance.FilePath);
        Assert.Equal(ProjectIdentity.Named("Beta"), instance.Project);
        Assert.True(instance.IsTestFile);
        Assert.Equal("M", instance.MemberName);
        Assert.Equal("abc", instance.Hash);
        Assert.Equal(block.Lines, instance.Lines);
    }

    [Theory]
    [InlineData(10, 2, 10)]
    [InlineData(10, 1, 0)]
    [InlineData(6, 12, 66)]
    [InlineData(36, 2, 36)]
    public void RemovableLines_CountsWhatDeduplicationWouldDelete(int lines, int occurrences, int expected) =>
        Assert.Equal(expected, new ClusterMetrics(lines, occurrences, 1, 1, true).RemovableLines);

    [Fact]
    public void IsExact_IsTrue_WhenEveryInstanceSharesOneHash() =>
        Assert.True(Cluster("h", "h").IsExact);

    [Fact]
    public void IsExact_IsFalse_WhenHashesDiffer() =>
        Assert.False(Cluster("h1", "h2").IsExact);

    [Fact]
    public void Summary_DerivesItsLabel()
    {
        var summary = new ReportSummary(1, 1, 30, 100, 30.0, DiscoveryStats.Empty);
        Assert.Equal(ScoreLabel.Critical, summary.Label);
        Assert.Equal(DiscoveryMode.None, summary.Discovery.Mode);
        Assert.Equal(0, summary.Discovery.Discovered);
        Assert.Equal(0, summary.Discovery.Excluded);
    }

    private static DuplicateCluster Cluster(params string[] hashes) => new()
    {
        Id = "dup-1",
        Instances = [.. hashes.Select((hash, index) =>
            new CodeInstance($"/f{index}.cs", ProjectIdentity.Unknown, false, "M", new LineRange(1, 2), hash))],
        Metrics = new ClusterMetrics(2, hashes.Length, hashes.Length, 0, false),
        NormalizedSnippet = "n",
        RawSnippets = ["r"],
        IsCohesive = true,
        IsProductionDuplicate = false,
    };
}
