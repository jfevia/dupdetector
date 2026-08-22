using DupDetector.Core.Detection;

using DupDetector.Core.Model;

using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Covers how a cluster is judged to be a production duplicate.
/// </summary>
public class ProductionDuplicateTests
{
    private static readonly DetectionSettings Permissive;

    static ProductionDuplicateTests()
    {
        Permissive = new()
        {
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MinLines = 1,
            MinProductionDuplicateLines = 1,
        };
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsExact_StaysTrueForVerbatimCopiesThatSurviveAsNearDuplicateCluster()
    {
        var blockSpec20 = new BlockSpec("a b c d e")
        {
            Path = "/1.cs",
            Hash = "same",
            StartLine = 1,
            EndLine = 5
        };
        var blockSpec21 = new BlockSpec("a b c d e")
        {
            Path = "/1.cs",
            Hash = "same",
            StartLine = 10,
            EndLine = 14
        };
        var blockSpec22 = new BlockSpec("a b c d e")
        {
            Path = "/2.cs",
            Hash = "same"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec20),
            Code.Block(blockSpec21),
            Code.Block(blockSpec22),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            MinFileSpread = 2
        }));
        Assert.True(cluster.IsExact);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsProductionDuplicate_IsFalseForNearDuplicates()
    {
        var blockSpec23 = new BlockSpec("a b c d e")
        {
            Path = "/A/x.cs",
            Project = "Alpha",
            Hash = "h1",
            StartLine = 1,
            EndLine = 12
        };
        var blockSpec24 = new BlockSpec("a b c d f")
        {
            Path = "/B/x.cs",
            Project = "Beta",
            Hash = "h2",
            StartLine = 1,
            EndLine = 12
        };
        var blocks = new[]
        {
            Code.Block(blockSpec23),
            Code.Block(blockSpec24),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.5
        }));
        Assert.False(cluster.IsExact);
        Assert.False(cluster.IsProductionDuplicate);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsProductionDuplicate_IsFalseWhenEveryInstanceIsTestFile()
    {
        var blockSpec25 = new BlockSpec("a")
        {
            Path = "/A/xTests.cs",
            Project = "Alpha",
            IsTestFile = true,
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };
        var blockSpec26 = new BlockSpec("a")
        {
            Path = "/B/xTests.cs",
            Project = "Beta",
            IsTestFile = true,
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };
        var blocks = new[]
        {
            Code.Block(blockSpec25),
            Code.Block(blockSpec26),
        };

        Assert.False(DuplicateDetector.Detect(blocks, Permissive)[0].IsProductionDuplicate);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsProductionDuplicate_RequiresOnlyOneProductionInstance()
    {
        var blockSpec27 = new BlockSpec("a")
        {
            Path = "/A/x.cs",
            Project = "Alpha",
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };
        var blockSpec28 = new BlockSpec("a")
        {
            Path = "/B/x.cs",
            Project = "Beta",
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };
        var blocks = new[]
        {
            Code.Block(blockSpec27),
            Code.Block(blockSpec28),
        };

        var settings = Permissive with
        {
            MinProductionDuplicateLines = 10
        };
        Assert.True(DuplicateDetector.Detect(blocks, settings)[0].IsProductionDuplicate);

        var blockSpec29 = new BlockSpec("a")
        {
            Path = "/T/xTests.cs",
            Project = "Gamma",
            IsTestFile = true,
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };
        var withTest = new List<CodeBlock>(blocks)
        {
            Code.Block(blockSpec29)
        };

        Assert.True(DuplicateDetector.Detect(withTest, settings)[0].IsProductionDuplicate);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsProductionDuplicate_RequiresTwoProjectsAndTheLineMinimum()
    {
        var alpha = new BlockSpec("a")
        {
            Path = "/A/x.cs",
            Project = "Alpha",
            Hash = "same",
            StartLine = 1,
            EndLine = 12
        };

        var sameProject = alpha with
        {
            Path = "/A/y.cs"
        };
        var oneProject = new[]
        {
            Code.Block(alpha),
            Code.Block(sameProject),
        };
        Assert.False(DuplicateDetector.Detect(oneProject, Permissive)[0].IsProductionDuplicate);

        var shortAlpha = alpha with
        {
            EndLine = 2
        };
        var shortBeta = shortAlpha with
        {
            Path = "/B/x.cs",
            Project = "Beta"
        };
        var tooShort = new[]
        {
            Code.Block(shortAlpha),
            Code.Block(shortBeta),
        };
        Assert.False(DuplicateDetector.Detect(tooShort, Permissive with
        {
            MinProductionDuplicateLines = 10
        })[0].IsProductionDuplicate);
    }
}
