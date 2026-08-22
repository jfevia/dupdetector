using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class ModelProjectionTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void CodeBlock_ToInstance_PreservesIdentity()
    {
        var blockSpec = new BlockSpec("var0")
        {
            Path = "/repo/B.cs",
            Project = "Beta",
            IsTestFile = true,
            Hash = "abc",
            MemberName = "M"
        };
        var block = Code.Block(blockSpec);
        var instance = block.ToInstance();

        Assert.Equal("/repo/B.cs", instance.FilePath);
        Assert.Equal(ProjectIdentities.Named("Beta"), instance.Project);
        Assert.True(instance.IsTestFile);
        Assert.Equal("M", instance.MemberName);
        Assert.Equal("abc", instance.Hash);
        Assert.Equal(block.Lines, instance.Lines);
    }

    /// <summary>
    ///     The content key is the lowest hash, wherever it sits in the instance order.
    /// </summary>
    [Fact]
    public void ContentKey_TakesTheLowestHashRegardlessOfOrder()
    {
        Assert.Equal("a", ProjectionFixtures.Cluster(["b", "a"]).ContentKey);
        Assert.Equal("a", ProjectionFixtures.Cluster(["a", "b"]).ContentKey);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsExact_IsFalse_WhenHashesDiffer()
    {
        Assert.False(ProjectionFixtures.Cluster(["h1", "h2"]).IsExact);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsExact_IsTrue_WhenEveryInstanceSharesOneHash()
    {
        Assert.True(ProjectionFixtures.Cluster(["h", "h"]).IsExact);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="occurrences"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData(10, 2, 10)]
    [InlineData(10, 1, 0)]
    [InlineData(6, 12, 66)]
    [InlineData(36, 2, 36)]
    public void RemovableLines_CountsWhatDeduplicationWouldDelete(int lines, int occurrences, int expected)
    {
        var clusterSpread = new ClusterSpread(1, 1, true);
        var clusterMetrics = new ClusterMetrics(lines, occurrences, clusterSpread);
        Assert.Equal(expected, clusterMetrics.RemovableLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SourceUnit_ToFile_CarriesClassificationAndLineCount()
    {
        var unitSpec = new UnitSpec("class C\n{\n}\n")
        {
            Path = "/repo/a/C.cs",
            Project = "Alpha",
            IsTestFile = true
        };
        var unit = Code.Unit(unitSpec);
        var file = unit.ToFile();

        Assert.Equal("/repo/a/C.cs", file.Path);
        Assert.Equal("repo/a/C.cs", file.RelativePath);
        Assert.Equal(ProjectIdentities.Named("Alpha"), file.Project);
        Assert.Equal(3, file.LineCount);
        Assert.True(file.IsTestFile);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Summary_DerivesItsLabel()
    {
        var summary = new ReportSummary
        {
            TotalFiles = 1,
            TotalClusters = 1,
            TotalDuplicateLines = 30,
            TotalLines = 100,
            DuplicationPercentage = 30.0,
            Discovery = DiscoveryStats.Empty
        };
        Assert.Equal(ScoreLabel.Critical, summary.Label);
        Assert.Equal(DiscoveryMode.None, summary.Discovery.Mode);
        Assert.Equal(0, summary.Discovery.Discovered);
        Assert.Equal(0, summary.Discovery.Excluded);
    }
}
