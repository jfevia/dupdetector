using DupDetector.Core.Extraction;
using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
/// Covers whole-type extraction and the suppression accounting added after the report audit.
/// </summary>
public class TypeExtractionTests
{
    private static IReadOnlyList<CodeBlock> Extract(string source, DetectionKind kinds, int minTypeLines = 3) =>
        Code.Blocks(source, new DetectionSettings { MinLines = 1, MinTypeLines = minTypeLines, Kinds = kinds });

    [Theory]
    [InlineData("record R(int A)\n{\n    int M() => 1;\n}", "record R")]
    [InlineData("struct S\n{\n    int M() => 1;\n}", "struct S")]
    [InlineData("class C\n{\n    int M() => 1;\n}", "class C")]
    [InlineData("interface I\n{\n    int M();\n    int N();\n}", "interface I")]
    [InlineData("enum E\n{\n    One,\n    Two,\n}", "enum E")]
    public void EveryTypeDeclarationKindIsNamed(string source, string expected)
    {
        var block = Assert.Single(Extract(source, DetectionKind.Types));

        Assert.Equal(expected, block.MemberName);
    }

    [Fact]
    public void EveryDeclarationFormIsClassifiedByASingleSwitch()
    {
        const string source = """
            class Outer
            {
                ~Outer() { var a = 1; }

                public Outer() { var b = 2; }

                public static Outer operator +(Outer l, Outer r) { return l; }

                public static explicit operator int(Outer o) { return 0; }

                public int Arrow => 1;

                public int this[string k] => 2;

                public int Block { get { return 3; } }

                public int this[int i] { get { return 4; } }

                public event System.EventHandler E { add { } remove { } }

                void Method()
                {
                    void Local() { var c = 5; }
                    Local();
                }
            }

            record R(int A);

            record struct RS(int B);

            struct S { }

            interface I { }

            enum Kind { One }

            delegate void D();
            """;

        var described = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .Select(MemberBlockExtractor.Describe)
            .Where(result => result is not null)
            .Select(result => result!.Value.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "Arrow", "Block.get", "E.add", "E.remove", "Local", "Method", "Outer",
                "class Outer", "enum Kind", "interface I", "operator +", "operator int",
                "record R", "record RS", "struct S", "this[]", "this[].get", "~Outer",
            ],
            described);
    }

    [Fact]
    public void ARecordIsLabelledFromItsOwnKeywordRatherThanItsNodeType()
    {
        // A record is also a class declaration in Roslyn; the keyword token is what disambiguates.
        Assert.Equal("record R", Assert.Single(Extract("record class R\n{\n    int M() => 1;\n}", DetectionKind.Types)).MemberName);
        Assert.Equal("record RS", Assert.Single(Extract("record struct RS\n{\n    int M() => 1;\n}", DetectionKind.Types)).MemberName);
    }

    [Fact]
    public void ADelegateIsNotAType()
    {
        // A delegate declares no body, so there is nothing to duplicate.
        Assert.Empty(Extract("delegate void D(int a, int b, int c, int d, int e);", DetectionKind.Types, minTypeLines: 1));
    }

    [Fact]
    public void AnExpressionBodiedIndexerIsExtracted()
    {
        const string source = """
            class C
            {
                public int this[int i] =>
                    i +
                    1;
            }
            """;

        var block = Assert.Single(Extract(source, DetectionKind.Accessors));

        Assert.Equal("this[]", block.MemberName);
    }

    [Fact]
    public void ABlockBodiedIndexerIsCoveredByItsAccessorsRatherThanTwice()
    {
        const string source = """
            class C
            {
                public int this[int i]
                {
                    get
                    {
                        var a = i;
                        var b = a + 1;
                        return b;
                    }
                }
            }
            """;

        var names = Extract(source, DetectionKind.Accessors).Select(block => block.MemberName).ToArray();

        Assert.Equal(["this[].get"], names);
    }

    [Fact]
    public void TypesAreHeldToTheirOwnMinimum()
    {
        const string source = """
            class C
            {
                int M() => 1;
            }
            """;

        Assert.Empty(Extract(source, DetectionKind.Types, minTypeLines: 99));
        Assert.Single(Extract(source, DetectionKind.Types, minTypeLines: 4));
    }

    [Fact]
    public void AllIncludesTypesWhileMembersDoesNot()
    {
        Assert.True(DetectionKind.All.HasFlag(DetectionKind.Types));
        Assert.False(DetectionKind.Members.HasFlag(DetectionKind.Types));
        Assert.Equal(DetectionKind.All, DetectionKind.Members | DetectionKind.Types);
    }
}

/// <summary>
/// Covers attribution of discarded clusters to the threshold that discarded them.
/// </summary>
public class SuppressionAccountingTests
{
    /// <summary>
    /// Near-duplicate blocks: distinct hashes, so the exact pass cannot claim them and the maximums,
    /// which apply only to the near-duplicate pass, are the thresholds under test.
    /// </summary>
    private static IReadOnlyList<CodeBlock> Similar(int count) =>
        [.. Enumerable.Range(0, count).Select(index => Code.Block(
            $"alpha beta gamma delta epsilon zeta eta theta v{index}",
            path: $"/repo/File{index}.cs",
            project: $"Proj{index}",
            hash: $"hash{index}",
            startLine: (index * 20) + 1,
            endLine: (index * 20) + 10))];

    private static IReadOnlyList<CodeBlock> Blocks(int count, int fileCount) =>
        [.. Enumerable.Range(0, count).Select(index => Code.Block(
            "identical",
            path: $"/repo/File{index % fileCount}.cs",
            project: $"Proj{index % fileCount}",
            hash: "same",
            startLine: (index * 20) + 1,
            endLine: (index * 20) + 10))];

    [Fact]
    public void ClustersRejectedForTooManyCopiesAreAttributedToThatThreshold()
    {
        var outcome = DuplicateDetector.DetectDetailed(
            Similar(6),
            new DetectionSettings { MinLines = 1, MinFileSpread = 1, MinProjectSpread = 1, MaxOccurrences = 3, Similarity = 0.5 },
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.AboveOccurrences);
    }

    [Fact]
    public void ClustersRejectedForTooWideASpreadAreAttributedToThatThreshold()
    {
        var outcome = DuplicateDetector.DetectDetailed(
            Similar(6),
            new DetectionSettings { MinLines = 1, MinFileSpread = 1, MinProjectSpread = 1, MaxFileSpread = 3, Similarity = 0.5 },
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.AboveFileSpread);
    }

    [Fact]
    public void ClustersRejectedForTooNarrowAProjectSpreadAreAttributedToThatThreshold()
    {
        var outcome = DuplicateDetector.DetectDetailed(
            Blocks(3, 3),
            new DetectionSettings { MinLines = 1, MinFileSpread = 1, MinProjectSpread = 9 },
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.BelowProjectSpread);
    }

    [Fact]
    public void AGroupRejectedByBothPassesIsCountedOnce()
    {
        // The exact pass leaves rejected members unclaimed so they can form a wider group, and the
        // near-duplicate pass then re-forms the same one. Counting it twice would overstate the total.
        var outcome = DuplicateDetector.DetectDetailed(
            Blocks(3, 1),
            new DetectionSettings { MinLines = 1, MinFileSpread = 9, MinProjectSpread = 1, Similarity = 0.5 },
            CliqueBudget.Default);

        Assert.Empty(outcome.Clusters);
        Assert.Equal(1, outcome.Suppressed.BelowFileSpread);
        Assert.Equal(1, outcome.Suppressed.Total);
    }

    [Fact]
    public void ExcludedClustersAreAttributedToTheRuleThatExcludedThem()
    {
        var cluster = DuplicateDetector.Build(
            Blocks(2, 2),
            new DetectionSettings { MinLines = 1 },
            cohesive: true);

        var outcome = new DetectionOutcome([cluster], SuppressionCounts.Empty);

        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, new DetectionSettings { ExcludeSnippetPatterns = ["identical"] })
                .Suppressed.ExcludedBySnippetPattern);

        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, new DetectionSettings { ExcludeClusterFileGlobs = ["**/*.cs"] })
                .Suppressed.ExcludedByFileGlob);

        Assert.Equal(
            1,
            ClusterFilters.ApplyDetailed(outcome, new DetectionSettings { ExcludeProjectPatterns = ["Proj"] })
                .Suppressed.ExcludedByProjectPattern);
    }

    [Fact]
    public void ContainmentKeepsTheWiderClusterAndNeverSuppressesBothOfAPair()
    {
        var settings = new DetectionSettings { MinLines = 1 };

        var outer = DuplicateDetector.Build(
            [
                Code.Block("outer", path: "/a.cs", hash: "o", startLine: 1, endLine: 20),
                Code.Block("outer", path: "/b.cs", hash: "o", startLine: 1, endLine: 20),
            ],
            settings,
            cohesive: true);

        var inner = DuplicateDetector.Build(
            [
                Code.Block("inner", path: "/a.cs", hash: "i", startLine: 5, endLine: 9),
                Code.Block("inner", path: "/b.cs", hash: "i", startLine: 5, endLine: 9),
            ],
            settings,
            cohesive: true);

        Assert.Same(outer, Assert.Single(ClusterFilters.SuppressContained([outer, inner])));

        // Same size and same span: neither encloses the other, so both survive.
        Assert.Equal(2, ClusterFilters.SuppressContained([outer, outer with { Id = "other" }]).Count);
    }

    [Fact]
    public void AWiderClusterIsNotSuppressedByANarrowerOne()
    {
        var settings = new DetectionSettings { MinLines = 1 };

        var narrow = DuplicateDetector.Build(
            [
                Code.Block("x", path: "/a.cs", hash: "n", startLine: 1, endLine: 20),
                Code.Block("x", path: "/b.cs", hash: "n", startLine: 1, endLine: 20),
            ],
            settings,
            cohesive: true);

        var wide = DuplicateDetector.Build(
            [
                Code.Block("y", path: "/a.cs", hash: "w", startLine: 5, endLine: 9),
                Code.Block("y", path: "/b.cs", hash: "w", startLine: 5, endLine: 9),
                Code.Block("y", path: "/c.cs", hash: "w", startLine: 5, endLine: 9),
            ],
            settings,
            cohesive: true);

        Assert.Equal(2, ClusterFilters.SuppressContained([narrow, wide]).Count);
    }

    [Fact]
    public void AWidelySpreadExactClusterIsNeverWithheldByTheMaximums()
    {
        // The maximums guard the fuzzy pass against weakly related cliques. An exact cluster shares one
        // hash by construction, so making them symmetric would discard the most valuable finding the
        // tool produces: on a real codebase this is a class copied verbatim into 25 files, against a
        // default limit of 20.
        var settings = DetectionSettings.Default with { MinLines = 1, MinProjectSpread = 1 };
        var outcome = DuplicateDetector.DetectDetailed(Blocks(25, 25), settings, CliqueBudget.Default);

        var cluster = Assert.Single(outcome.Clusters);

        Assert.True(cluster.IsExact);
        Assert.Equal(25, cluster.Metrics.FileSpread);
        Assert.True(cluster.Metrics.FileSpread > settings.MaxFileSpread);
        Assert.Equal(0, outcome.Suppressed.AboveFileSpread);
        Assert.Equal(0, outcome.Suppressed.AboveOccurrences);
    }

    [Fact]
    public void ContentKeyIsStableWhenACopyIsAdded()
    {
        var settings = new DetectionSettings { MinLines = 1 };
        var two = DuplicateDetector.Build(Blocks(2, 2), settings, cohesive: true);
        var three = DuplicateDetector.Build(Blocks(3, 3), settings, cohesive: true);

        Assert.Equal(two.ContentKey, three.ContentKey);
        Assert.NotEqual(two.Id, three.Id);
    }
}
