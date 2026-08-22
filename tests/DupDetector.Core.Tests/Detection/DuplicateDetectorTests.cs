using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     
/// </summary>
public class DuplicateDetectorTests
{
    private static readonly DetectionSettings Permissive;

    static DuplicateDetectorTests()
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
    public void ClusterId_DependsOnlyOnMemberHashes()
    {
        var blockSpec = new BlockSpec("a")
        {
            Path = "/aaa.cs",
            Hash = "same"
        };
        var blockSpec2 = new BlockSpec("a")
        {
            Path = "/bbb.cs",
            Hash = "same"
        };
        var first = DuplicateDetector.Detect(
            [Code.Block(blockSpec), Code.Block(blockSpec2)],
            Permissive);

        var blockSpec3 = new BlockSpec("a")
        {
            Path = "/zzz.cs",
            Hash = "same"
        };
        var blockSpec4 = new BlockSpec("a")
        {
            Path = "/aaa.cs",
            Hash = "same"
        };
        var second = DuplicateDetector.Detect(
            [Code.Block(blockSpec3), Code.Block(blockSpec4)],
            Permissive);

        Assert.Equal(first[0].Id, second[0].Id);
        Assert.StartsWith("dup-", first[0].Id, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Clusters_AreOrderedByRemovableLinesThenId()
    {
        var blockSpec5 = new BlockSpec("small")
        {
            Path = "/a1.cs",
            Hash = "small",
            StartLine = 1,
            EndLine = 2
        };
        var blockSpec36 = new BlockSpec("small")
        {
            Path = "/a2.cs",
            Hash = "small",
            StartLine = 1,
            EndLine = 2
        };
        var blockSpec37 = new BlockSpec("big")
        {
            Path = "/b1.cs",
            Hash = "big",
            StartLine = 1,
            EndLine = 40
        };
        var blockSpec38 = new BlockSpec("big")
        {
            Path = "/b2.cs",
            Hash = "big",
            StartLine = 1,
            EndLine = 40
        };
        var blocks = new List<CodeBlock>
        {
            Code.Block(blockSpec5),
            Code.Block(blockSpec36),
            Code.Block(blockSpec37),
            Code.Block(blockSpec38),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        });

        Assert.Equal(2, clusters.Count);
        Assert.True(clusters[0].Metrics.RemovableLines > clusters[1].Metrics.RemovableLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_AppliesNearDuplicateMaximums()
    {
        var blocks = new List<CodeBlock>();
        for (var index = 0; index < 6; index++)
        {
            var spec = new BlockSpec($"a b c d e f g h {index}")
            {
                Path = $"/{index}.cs",
                Hash = $"h{index}"
            };

            blocks.Add(Code.Block(spec));
        }

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.5,
            MaxFileSpread = 3
        }));
        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.5,
            MaxOccurrences = 3
        }));
        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.5,
            MaxFileSpread = 0,
            MaxOccurrences = 0
        }));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_AppliesProjectSpreadMinimum()
    {
        var blockSpec6 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Project = "P",
            Hash = "same"
        };
        var blockSpec7 = new BlockSpec("a b c")
        {
            Path = "/2.cs",
            Project = "P",
            Hash = "same"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec6),
            Code.Block(blockSpec7),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with
        {
            MinProjectSpread = 2,
            Similarity = 1.0
        }));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_AppliesSpreadMinimums()
    {
        var blockSpec8 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Project = "P",
            Hash = "same",
            StartLine = 1,
            EndLine = 3
        };
        var blockSpec9 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Project = "P",
            Hash = "same",
            StartLine = 9,
            EndLine = 11
        };
        var blocks = new[]
        {
            Code.Block(blockSpec8),
            Code.Block(blockSpec9),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with
        {
            MinFileSpread = 2,
            Similarity = 1.0
        }));
        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            MinFileSpread = 1,
            Similarity = 1.0
        }));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_GroupsVerbatimCopies()
    {
        var blockSpec10 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Hash = "same"
        };
        var blockSpec11 = new BlockSpec("a b c")
        {
            Path = "/2.cs",
            Hash = "same"
        };
        var cluster = Assert.Single(DuplicateDetector.Detect(
            [Code.Block(blockSpec10), Code.Block(blockSpec11)],
            Permissive));

        Assert.True(cluster.IsExact);
        Assert.Equal(2, cluster.Metrics.Occurrences);
        Assert.Equal(2, cluster.Metrics.FileSpread);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_LeavesFilteredExactGroupsAvailableToTheNearDuplicatePass()
    {
        var blockSpec12 = new BlockSpec("a b c d e")
        {
            Path = "/1.cs",
            Hash = "same",
            StartLine = 1,
            EndLine = 5
        };
        var blockSpec13 = new BlockSpec("a b c d e")
        {
            Path = "/1.cs",
            Hash = "same",
            StartLine = 10,
            EndLine = 14
        };
        var blockSpec14 = new BlockSpec("a b c d f")
        {
            Path = "/2.cs",
            Hash = "other"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec12),
            Code.Block(blockSpec13),
            Code.Block(blockSpec14),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(
            blocks,
            Permissive with
            {
                MinFileSpread = 2,
                Similarity = 0.5
            }));

        Assert.Equal(3, cluster.Metrics.Occurrences);
        Assert.False(cluster.IsExact);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_ReturnsNothingWhenFewerThanTwoBlocksRemainForTheNearPass()
    {
        var blockSpec15 = new BlockSpec("a b c")
        {
            Hash = "only"
        };
        Assert.Empty(DuplicateDetector.Detect([Code.Block(blockSpec15)], Permissive));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_ReturnsNothingWhenThereIsNoDuplication()
    {
        var blockSpec16 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Hash = "h1"
        };
        var blockSpec17 = new BlockSpec("x y z")
        {
            Path = "/2.cs",
            Hash = "h2"
        };
        Assert.Empty(DuplicateDetector.Detect(
            [Code.Block(blockSpec16), Code.Block(blockSpec17)],
            Permissive with
            {
                Similarity = 1.0
            }));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Detect_SkipsTheNearDuplicatePassWhenSimilarityIsOne()
    {
        var blockSpec18 = new BlockSpec("a b c d e")
        {
            Path = "/1.cs",
            Hash = "h1"
        };
        var blockSpec19 = new BlockSpec("a b c d x")
        {
            Path = "/2.cs",
            Hash = "h2"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec18),
            Code.Block(blockSpec19),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        }));
        Assert.NotEmpty(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 0.5
        }));
    }

    /// <summary>
    ///     Clusters that tie on removable lines and on occurrences are ordered by identifier.
    /// </summary>
    [Fact]
    public void Ordering_FallsBackToIdentifierWhenSeverityTies()
    {
        var first = new BlockSpec("one")
        {
            Path = "/a.cs",
            Hash = "one",
            StartLine = 1,
            EndLine = 6
        };

        var second = first with
        {
            Path = "/b.cs"
        };

        var third = new BlockSpec("two")
        {
            Path = "/c.cs",
            Hash = "two",
            StartLine = 1,
            EndLine = 6
        };

        var fourth = third with
        {
            Path = "/d.cs"
        };

        var blocks = new List<CodeBlock>
        {
            Code.Block(first),
            Code.Block(second),
            Code.Block(third),
            Code.Block(fourth),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        });

        Assert.Equal(2, clusters.Count);
        Assert.Equal(clusters[0].Metrics.RemovableLines, clusters[1].Metrics.RemovableLines);
        Assert.Equal(clusters[0].Metrics.Occurrences, clusters[1].Metrics.Occurrences);
        Assert.True(string.CompareOrdinal(clusters[0].Id, clusters[1].Id) < 0);
    }

    /// <summary>
    ///     Equally severe clusters fall back to identifier order, and instances sharing a location
    ///     fall back to hash order.
    /// </summary>
    [Fact]
    public void Ordering_FallsBackWhenTheLeadingKeysTie()
    {
        var first = new BlockSpec("one")
        {
            Path = "/same.cs",
            Hash = "one",
            StartLine = 1,
            EndLine = 6
        };

        var second = first with
        {
            Path = "/other.cs"
        };

        var third = new BlockSpec("two")
        {
            Path = "/third.cs",
            Hash = "two",
            StartLine = 1,
            EndLine = 6
        };

        var fourth = third with
        {
            Path = "/fourth.cs"
        };

        var blocks = new List<CodeBlock>
        {
            Code.Block(first),
            Code.Block(second),
            Code.Block(first),
            Code.Block(third),
            Code.Block(fourth),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        });

        Assert.Equal(2, clusters.Count);
        Assert.True(clusters[0].Metrics.RemovableLines >= clusters[1].Metrics.RemovableLines);
        Assert.Equal(3, clusters[0].Instances.Count);
        Assert.Equal("/other.cs", clusters[0].Instances[0].FilePath);
    }

    /// <summary>
    ///     Clusters that save the same number of lines are ordered by how far they have spread.
    /// </summary>
    [Fact]
    public void Ordering_PrefersMoreOccurrencesWhenRemovableLinesTie()
    {
        var wide = new BlockSpec("wide")
        {
            Path = "/w1.cs",
            Hash = "wide",
            StartLine = 1,
            EndLine = 3
        };

        var tall = new BlockSpec("tall")
        {
            Path = "/t1.cs",
            Hash = "tall",
            StartLine = 1,
            EndLine = 6
        };

        var blocks = new List<CodeBlock>
        {
            Code.Block(wide),
            Code.Block(wide with
            {
                Path = "/w2.cs"
            }),
            Code.Block(wide with
            {
                Path = "/w3.cs"
            }),
            Code.Block(tall),
            Code.Block(tall with
            {
                Path = "/t2.cs"
            }),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        });

        Assert.Equal(2, clusters.Count);
        Assert.Equal(clusters[0].Metrics.RemovableLines, clusters[1].Metrics.RemovableLines);
        Assert.Equal(3, clusters[0].Metrics.Occurrences);
        Assert.Equal(2, clusters[1].Metrics.Occurrences);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnknownProjectsNeverFabricateProjectSpread()
    {
        var blockSpec34 = new BlockSpec("a b c")
        {
            Path = "/1.cs",
            Project = null,
            Hash = "same"
        };
        var blockSpec35 = new BlockSpec("a b c")
        {
            Path = "/2.cs",
            Project = null,
            Hash = "same"
        };
        var blocks = new[]
        {
            Code.Block(blockSpec34),
            Code.Block(blockSpec35),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            Similarity = 1.0
        }));
        Assert.Equal(0, cluster.Metrics.ProjectSpread);
        Assert.False(cluster.Metrics.IsProjectSpreadKnown);

        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with
        {
            MinProjectSpread = 2,
            Similarity = 1.0
        }));
    }
}
