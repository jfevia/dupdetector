using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     
/// </summary>
public class DetectorCliqueIntegrationTests
{
    private static readonly DetectionSettings Permissive;

    static DetectorCliqueIntegrationTests()
    {
        Permissive = new()
        {
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MinLines = 1,
            Similarity = 0.6,
        };
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BudgetedRunStillProducesClustersAndFlagsThem()
    {
        var blocks = new List<CodeBlock>(6);
        for (var index = 0; index < 6; index++)
        {
            var spec = new BlockSpec($"shared shared t{index} t{(index + 1) % 6}")
            {
                Path = $"/{index}.cs",
                Hash = $"h{index}"
            };

            blocks.Add(Code.Block(spec));
        }

        var cliqueBudget4 = new CliqueBudget(2, 10_000);
        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.3
        }, cliqueBudget4);

        Assert.NotEmpty(clusters);
        Assert.Contains(clusters, cluster => !cluster.IsCohesive);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ChainedNearDuplicatesDoNotMergeIntoOneCluster()
    {
        var blockSpec2 = new BlockSpec("a a a a b b")
        {
            Path = "/0.cs",
            Hash = "h0"
        };
        var blockSpec3 = new BlockSpec("b b b a a c")
        {
            Path = "/1.cs",
            Hash = "h1"
        };
        var blockSpec4 = new BlockSpec("c c c b b b")
        {
            Path = "/2.cs",
            Hash = "h2"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec2),
            Code.Block(blockSpec3),
            Code.Block(blockSpec4),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.35
        });

        Assert.All(clusters, cluster => Assert.True(cluster.IsCohesive));
        Assert.DoesNotContain(clusters, cluster => CliqueAssertions.CanTouches(cluster, "/0.cs") && CliqueAssertions.CanTouches(cluster, "/2.cs"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExactClustersAreAlwaysCohesive()
    {
        var blockSpec5 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Hash = "same"
        };
        var blockSpec6 = new BlockSpec("a b c")
        {
            Path = "/2.cs",
            Hash = "same"
        };
        var cluster = Assert.Single(DuplicateDetector.Detect(
            [Code.Block(blockSpec5), Code.Block(blockSpec6)],
            Permissive));

        Assert.True(cluster.IsCohesive);
        Assert.True(cluster.IsExact);
    }
}
