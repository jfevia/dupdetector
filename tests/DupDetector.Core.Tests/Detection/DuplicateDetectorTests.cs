using DupDetector.Core.Detection;
using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.Core.Scoring;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

public class DuplicateDetectorTests
{
    private static readonly DetectionSettings Permissive = new()
    {
        MinFileSpread = 1,
        MinProjectSpread = 1,
        MinLines = 1,
        MinProductionDuplicateLines = 1,
    };

    [Fact]
    public void Detect_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => DuplicateDetector.Detect(null!, Permissive));
        Assert.Throws<ArgumentNullException>(() => DuplicateDetector.Detect([], null!));
    }

    [Fact]
    public void Detect_ReturnsNothingWhenThereIsNoDuplication() =>
        Assert.Empty(DuplicateDetector.Detect(
            [Code.Block("a b c", path: "/1.cs", hash: "h1"), Code.Block("x y z", path: "/2.cs", hash: "h2")],
            Permissive with { Similarity = 1.0 }));

    [Fact]
    public void Detect_GroupsVerbatimCopies()
    {
        var cluster = Assert.Single(DuplicateDetector.Detect(
            [Code.Block("a b c", path: "/1.cs", hash: "same"), Code.Block("a b c", path: "/2.cs", hash: "same")],
            Permissive));

        Assert.True(cluster.IsExact);
        Assert.Equal(2, cluster.Metrics.Occurrences);
        Assert.Equal(2, cluster.Metrics.FileSpread);
    }

    [Fact]
    public void Detect_SkipsTheNearDuplicatePassWhenSimilarityIsOne()
    {
        var blocks = new[]
        {
            Code.Block("a b c d e", path: "/1.cs", hash: "h1"),
            Code.Block("a b c d x", path: "/2.cs", hash: "h2"),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 1.0 }));
        Assert.NotEmpty(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.5 }));
    }

    [Fact]
    public void Detect_LeavesFilteredExactGroupsAvailableToTheNearDuplicatePass()
    {
        // Two identical blocks in one file are rejected by the file-spread minimum, then merge with
        // a near-duplicate elsewhere. The hashes still differ, so the cluster is not exact.
        var blocks = new[]
        {
            Code.Block("a b c d e", path: "/1.cs", hash: "same", startLine: 1, endLine: 5),
            Code.Block("a b c d e", path: "/1.cs", hash: "same", startLine: 10, endLine: 14),
            Code.Block("a b c d f", path: "/2.cs", hash: "other"),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(
            blocks,
            Permissive with { MinFileSpread = 2, Similarity = 0.5 }));

        Assert.Equal(3, cluster.Metrics.Occurrences);
        Assert.False(cluster.IsExact);
    }

    [Fact]
    public void IsExact_StaysTrueForVerbatimCopiesThatSurviveAsANearDuplicateCluster()
    {
        // Same-file exact pair rejected by the spread minimum, re-formed via the near pass with a
        // second file whose block is byte-identical. Every hash matches, so the cluster is exact.
        var blocks = new[]
        {
            Code.Block("a b c d e", path: "/1.cs", hash: "same", startLine: 1, endLine: 5),
            Code.Block("a b c d e", path: "/1.cs", hash: "same", startLine: 10, endLine: 14),
            Code.Block("a b c d e", path: "/2.cs", hash: "same"),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { MinFileSpread = 2 }));
        Assert.True(cluster.IsExact);
    }

    [Fact]
    public void Detect_AppliesSpreadMinimums()
    {
        var blocks = new[]
        {
            Code.Block("a b c", path: "/1.cs", project: "P", hash: "same", startLine: 1, endLine: 3),
            Code.Block("a b c", path: "/1.cs", project: "P", hash: "same", startLine: 9, endLine: 11),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with { MinFileSpread = 2, Similarity = 1.0 }));
        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { MinFileSpread = 1, Similarity = 1.0 }));
    }

    [Fact]
    public void Detect_AppliesProjectSpreadMinimum()
    {
        var blocks = new[]
        {
            Code.Block("a b c", path: "/1.cs", project: "P", hash: "same"),
            Code.Block("a b c", path: "/2.cs", project: "P", hash: "same"),
        };

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with { MinProjectSpread = 2, Similarity = 1.0 }));
    }

    [Fact]
    public void UnknownProjectsNeverFabricateProjectSpread()
    {
        // File spread is 2, but neither block knows its project, so project spread is genuinely 0
        // rather than being quietly borrowed from the file count.
        var blocks = new[]
        {
            Code.Block("a b c", path: "/1.cs", project: null, hash: "same"),
            Code.Block("a b c", path: "/2.cs", project: null, hash: "same"),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 1.0 }));
        Assert.Equal(0, cluster.Metrics.ProjectSpread);
        Assert.False(cluster.Metrics.ProjectSpreadKnown);

        // The minimum cannot be evaluated without project data, so it is not enforced. The
        // alternative would empty the report on any tree that has no project files.
        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { MinProjectSpread = 2, Similarity = 1.0 }));
    }

    [Fact]
    public void Detect_AppliesNearDuplicateMaximums()
    {
        var blocks = Enumerable.Range(0, 6)
            .Select(index => Code.Block($"a b c d e f g h {index}", path: $"/{index}.cs", hash: $"h{index}"))
            .ToArray();

        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.5, MaxFileSpread = 3 }));
        Assert.Empty(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.5, MaxOccurrences = 3 }));
        Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.5, MaxFileSpread = 0, MaxOccurrences = 0 }));
    }

    [Fact]
    public void Detect_ReturnsNothingWhenFewerThanTwoBlocksRemainForTheNearPass() =>
        Assert.Empty(DuplicateDetector.Detect([Code.Block("a b c", hash: "only")], Permissive));

    [Fact]
    public void ClusterId_DependsOnlyOnMemberHashes()
    {
        var first = DuplicateDetector.Detect(
            [Code.Block("a", path: "/aaa.cs", hash: "same"), Code.Block("a", path: "/bbb.cs", hash: "same")],
            Permissive);

        // Same duplicated code, different file names and a different discovery order.
        var second = DuplicateDetector.Detect(
            [Code.Block("a", path: "/zzz.cs", hash: "same"), Code.Block("a", path: "/aaa.cs", hash: "same")],
            Permissive);

        Assert.Equal(first[0].Id, second[0].Id);
        Assert.StartsWith("dup-", first[0].Id, StringComparison.Ordinal);
    }

    [Fact]
    public void Clusters_AreOrderedByRemovableLinesThenId()
    {
        var blocks = new List<CodeBlock>
        {
            Code.Block("small", path: "/a1.cs", hash: "small", startLine: 1, endLine: 2),
            Code.Block("small", path: "/a2.cs", hash: "small", startLine: 1, endLine: 2),
            Code.Block("big", path: "/b1.cs", hash: "big", startLine: 1, endLine: 40),
            Code.Block("big", path: "/b2.cs", hash: "big", startLine: 1, endLine: 40),
        };

        var clusters = DuplicateDetector.Detect(blocks, Permissive with { Similarity = 1.0 });

        Assert.Equal(2, clusters.Count);
        Assert.True(clusters[0].Metrics.RemovableLines > clusters[1].Metrics.RemovableLines);
    }

    [Fact]
    public void IsProductionDuplicate_RequiresOnlyOneProductionInstance()
    {
        var blocks = new[]
        {
            Code.Block("a", path: "/A/x.cs", project: "Alpha", hash: "same", startLine: 1, endLine: 12),
            Code.Block("a", path: "/B/x.cs", project: "Beta", hash: "same", startLine: 1, endLine: 12),
        };

        var settings = Permissive with { MinProductionDuplicateLines = 10 };
        Assert.True(DuplicateDetector.Detect(blocks, settings)[0].IsProductionDuplicate);

        // Adding a test-file copy must not clear the flag: the production debt is still real.
        var withTest = blocks.Append(
            Code.Block("a", path: "/T/xTests.cs", project: "Gamma", isTestFile: true, hash: "same", startLine: 1, endLine: 12)).ToArray();

        Assert.True(DuplicateDetector.Detect(withTest, settings)[0].IsProductionDuplicate);
    }

    [Fact]
    public void IsProductionDuplicate_IsFalseWhenEveryInstanceIsATestFile()
    {
        var blocks = new[]
        {
            Code.Block("a", path: "/A/xTests.cs", project: "Alpha", isTestFile: true, hash: "same", startLine: 1, endLine: 12),
            Code.Block("a", path: "/B/xTests.cs", project: "Beta", isTestFile: true, hash: "same", startLine: 1, endLine: 12),
        };

        Assert.False(DuplicateDetector.Detect(blocks, Permissive)[0].IsProductionDuplicate);
    }

    [Fact]
    public void IsProductionDuplicate_RequiresTwoProjectsAndTheLineMinimum()
    {
        var oneProject = new[]
        {
            Code.Block("a", path: "/A/x.cs", project: "Alpha", hash: "same", startLine: 1, endLine: 12),
            Code.Block("a", path: "/A/y.cs", project: "Alpha", hash: "same", startLine: 1, endLine: 12),
        };
        Assert.False(DuplicateDetector.Detect(oneProject, Permissive)[0].IsProductionDuplicate);

        var tooShort = new[]
        {
            Code.Block("a", path: "/A/x.cs", project: "Alpha", hash: "same", startLine: 1, endLine: 2),
            Code.Block("a", path: "/B/x.cs", project: "Beta", hash: "same", startLine: 1, endLine: 2),
        };
        Assert.False(DuplicateDetector.Detect(tooShort, Permissive with { MinProductionDuplicateLines = 10 })[0].IsProductionDuplicate);
    }

    [Fact]
    public void IsProductionDuplicate_IsFalseForNearDuplicates()
    {
        var blocks = new[]
        {
            Code.Block("a b c d e", path: "/A/x.cs", project: "Alpha", hash: "h1", startLine: 1, endLine: 12),
            Code.Block("a b c d f", path: "/B/x.cs", project: "Beta", hash: "h2", startLine: 1, endLine: 12),
        };

        var cluster = Assert.Single(DuplicateDetector.Detect(blocks, Permissive with { Similarity = 0.5 }));
        Assert.False(cluster.IsExact);
        Assert.False(cluster.IsProductionDuplicate);
    }
}

public class LineSpanMergerTests
{
    [Fact]
    public void CountDistinctLines_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => LineSpanMerger.CountDistinctLines(null!));

    [Fact]
    public void CountDistinctLines_IsZeroForNoRanges() =>
        Assert.Equal(0, LineSpanMerger.CountDistinctLines([]));

    [Theory]
    [InlineData(1, 5, 6, 10, 10)]
    [InlineData(1, 5, 7, 10, 9)]
    [InlineData(1, 10, 3, 5, 10)]
    [InlineData(1, 5, 1, 5, 5)]
    public void CountDistinctLines_MergesOverlappingAndTouchingRanges(int s1, int e1, int s2, int e2, int expected) =>
        Assert.Equal(expected, LineSpanMerger.CountDistinctLines([new LineRange(s1, e1), new LineRange(s2, e2)]));

    [Fact]
    public void CountDistinctLines_SortsBeforeMerging() =>
        Assert.Equal(10, LineSpanMerger.CountDistinctLines([new LineRange(6, 10), new LineRange(1, 5)]));
}

public class AggregateScorerTests
{
    private static readonly DuplicateCluster Cluster = new()
    {
        Id = "dup-1",
        Instances =
        [
            new CodeInstance("/a.cs", ProjectIdentity.Named("P"), false, "M", new LineRange(1, 10), "h"),
            new CodeInstance("/b.cs", ProjectIdentity.Named("Q"), false, "M", new LineRange(1, 10), "h"),
        ],
        Metrics = new ClusterMetrics(10, 2, 2, 2, true),
        NormalizedSnippet = "n",
        RawSnippets = ["r", "r"],
        IsCohesive = true,
        IsProductionDuplicate = true,
    };

    [Fact]
    public void Percentage_IsZeroWhenThereAreNoLines() => Assert.Equal(0.0, AggregateScorer.Percentage(5, 0));

    [Fact]
    public void Percentage_RoundsAwayFromZero()
    {
        Assert.Equal(50.0, AggregateScorer.Percentage(1, 2));
        Assert.Equal(6.63, AggregateScorer.RoundPercentage(6.625));
    }

    [Fact]
    public void DuplicateLinesByFile_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.DuplicateLinesByFile(null!));

    [Fact]
    public void DuplicateLinesByFile_CountsEachLineOnce()
    {
        var lines = AggregateScorer.DuplicateLinesByFile([Cluster]);
        Assert.Equal(10, lines["/a.cs"]);
        Assert.Equal(10, lines["/b.cs"]);
    }

    [Fact]
    public void ScoreFiles_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.ScoreFiles(null!, []));
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.ScoreFiles([], null!));
    }

    [Fact]
    public void ScoreFiles_ReportsPercentageAndClusterContext()
    {
        var files = new[]
        {
            new SourceFile("/a.cs", "a.cs", ProjectIdentity.Named("P"), 20, false),
            new SourceFile("/c.cs", "c.cs", ProjectIdentity.Named("P"), 0, true),
        };

        var scores = AggregateScorer.ScoreFiles(files, [Cluster]);

        var a = scores.Single(score => score.Path == "/a.cs");
        Assert.Equal(10, a.DuplicateLines);
        Assert.Equal(20, a.TotalLines);
        Assert.Equal(50.0, a.Percentage);
        Assert.Equal(1, a.ClusterCount);
        Assert.Equal(2, a.WidestClusterSpread);

        var c = scores.Single(score => score.Path == "/c.cs");
        Assert.Equal(0, c.DuplicateLines);
        Assert.Equal(0.0, c.Percentage);
        Assert.Equal(0, c.ClusterCount);
        Assert.Equal(0, c.WidestClusterSpread);
        Assert.True(c.IsTestFile);
    }

    [Fact]
    public void ScoreProjects_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.ScoreProjects(null!));

    [Fact]
    public void ScoreProjects_AggregatesFileScores()
    {
        var files = new[]
        {
            new SourceFile("/a.cs", "a.cs", ProjectIdentity.Named("P"), 20, false),
            new SourceFile("/b.cs", "b.cs", ProjectIdentity.Named("Q"), 40, false),
        };

        var projects = AggregateScorer.ScoreProjects(AggregateScorer.ScoreFiles(files, [Cluster]));

        Assert.Equal(2, projects.Count);
        Assert.Equal(50.0, projects.Single(p => p.Project.Name == "P").Percentage);
        Assert.Equal(25.0, projects.Single(p => p.Project.Name == "Q").Percentage);
    }

    [Fact]
    public void Summarize_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.Summarize(null!, [], DiscoveryStats.Empty));
        Assert.Throws<ArgumentNullException>(() => AggregateScorer.Summarize([], null!, DiscoveryStats.Empty));
    }

    [Fact]
    public void Summarize_TotalsTheRun()
    {
        var files = new[] { new SourceFile("/a.cs", "a.cs", ProjectIdentity.Named("P"), 20, false) };
        var summary = AggregateScorer.Summarize(
            AggregateScorer.ScoreFiles(files, [Cluster]),
            [Cluster],
            new DiscoveryStats(5, 1, DiscoveryMode.FileSystem));

        Assert.Equal(1, summary.TotalFiles);
        Assert.Equal(1, summary.TotalClusters);
        Assert.Equal(10, summary.TotalDuplicateLines);
        Assert.Equal(20, summary.TotalLines);
        Assert.Equal(50.0, summary.DuplicationPercentage);
        Assert.Equal(ScoreLabel.Critical, summary.Label);
        Assert.Equal(DiscoveryMode.FileSystem, summary.Discovery.Mode);
    }

    [Fact]
    public void Summarize_HandlesAnEmptyRun()
    {
        var summary = AggregateScorer.Summarize([], [], DiscoveryStats.Empty);
        Assert.Equal(0.0, summary.DuplicationPercentage);
        Assert.Equal(ScoreLabel.Low, summary.Label);
    }
}

public class ClusterFiltersTests
{
    private static DuplicateCluster Make(params (string Path, string Project)[] instances) => new()
    {
        Id = "dup-1",
        Instances = [.. instances.Select(i =>
            new CodeInstance(i.Path, ProjectIdentity.Named(i.Project), false, "M", new LineRange(1, 3), "h"))],
        Metrics = new ClusterMetrics(3, instances.Length, instances.Length, 1, true),
        NormalizedSnippet = "n",
        RawSnippets = ["public void IArchRule() { }"],
        IsCohesive = true,
        IsProductionDuplicate = false,
    };

    [Fact]
    public void Predicates_RejectNullArguments()
    {
        var cluster = Make(("/a.cs", "P"));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.MatchesAnySnippetPattern(null!, []));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.MatchesAnySnippetPattern(cluster, null!));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.AllInstancesMatchGlob(null!, GlobSet.Empty));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.AllInstancesMatchGlob(cluster, null!));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.AllInstancesInMatchingProject(null!, []));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.AllInstancesInMatchingProject(cluster, null!));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.Apply(null!, DetectionSettings.Default));
        Assert.Throws<ArgumentNullException>(() => ClusterFilters.Apply([], null!));
    }

    [Fact]
    public void MatchesAnySnippetPattern_IsCaseInsensitive()
    {
        var cluster = Make(("/a.cs", "P"));
        Assert.True(ClusterFilters.MatchesAnySnippetPattern(cluster, ["iarchrule"]));
        Assert.False(ClusterFilters.MatchesAnySnippetPattern(cluster, ["absent"]));
        Assert.False(ClusterFilters.MatchesAnySnippetPattern(cluster, []));
    }

    [Fact]
    public void AllInstancesMatchGlob_KeepsClustersThatStraddleTheBoundary()
    {
        var globs = GlobSet.Parse(["**/Arch/*.cs"]);

        Assert.True(ClusterFilters.AllInstancesMatchGlob(Make(("/r/Arch/a.cs", "P"), ("/r/Arch/b.cs", "P")), globs));
        Assert.False(ClusterFilters.AllInstancesMatchGlob(Make(("/r/Arch/a.cs", "P"), ("/r/Core/b.cs", "P")), globs));
        Assert.False(ClusterFilters.AllInstancesMatchGlob(Make(("/r/Arch/a.cs", "P")), GlobSet.Empty));
    }

    [Fact]
    public void AllInstancesInMatchingProject_RequiresEveryInstance()
    {
        Assert.True(ClusterFilters.AllInstancesInMatchingProject(
            Make(("/a.cs", "Acme.Architecture.Tests"), ("/b.cs", "Other.Architecture.Tests")), [".Architecture."]));

        Assert.False(ClusterFilters.AllInstancesInMatchingProject(
            Make(("/a.cs", "Acme.Architecture.Tests"), ("/b.cs", "Acme.Core")), [".Architecture."]));

        Assert.False(ClusterFilters.AllInstancesInMatchingProject(Make(("/a.cs", "P")), []));
    }

    [Fact]
    public void AllInstancesInMatchingProject_IsFalseWhenAProjectIsUnknown()
    {
        var cluster = new DuplicateCluster
        {
            Id = "dup-1",
            Instances = [new CodeInstance("/a.cs", ProjectIdentity.Unknown, false, "M", new LineRange(1, 3), "h")],
            Metrics = new ClusterMetrics(3, 1, 1, 0, false),
            NormalizedSnippet = "n",
            RawSnippets = ["r"],
            IsCohesive = true,
            IsProductionDuplicate = false,
        };

        Assert.False(ClusterFilters.AllInstancesInMatchingProject(cluster, ["any"]));
    }

    [Fact]
    public void Apply_RemovesOnlyWhatEachRuleSelects()
    {
        var kept = Make(("/r/Core/a.cs", "Acme.Core"));
        var suppressed = Make(("/r/Arch/a.cs", "Acme.Architecture"));

        Assert.Equal([kept, suppressed], ClusterFilters.Apply([kept, suppressed], DetectionSettings.Default));

        Assert.Equal([kept], ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with { ExcludeClusterFileGlobs = ["**/Arch/*.cs"] }));

        Assert.Equal([kept], ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with { ExcludeProjectPatterns = [".Architecture"] }));

        Assert.Empty(ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with { ExcludeSnippetPatterns = ["IArchRule"] }));
    }
}
