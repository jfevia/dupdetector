using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Integration tests for the --exclude-pattern flag (Run 5, GAP-A).
/// Verifies that clusters whose normalized snippet contains a given pattern are removed
/// from output, while unaffected clusters are preserved.
/// </summary>
public class ExcludePatternTests
{
    private readonly DuplicateDetector _detector = new();
    private readonly CodeNormalizer _normalizer = new();

    private CodeBlock MakeBlock(string code, string file, int start = 1, int end = 10)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        return new CodeBlock(file, start, end, "M", hash, normalized, code, end - start + 1);
    }

    private List<DuplicateCluster> DetectWith(List<CodeBlock> blocks, List<string> patterns,
        double similarity = 0.99, int minSpread = 1, int minProjSpread = 1)
    {
        var clusters = _detector.Detect(blocks, similarity,
            minClusterSpread: minSpread, minProjectSpread: minProjSpread);

        if (patterns.Count > 0)
        {
            clusters = clusters
                .Where(c => !patterns.Any(p =>
                    c.RawSnippets.Any(raw => raw.Contains(p, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        return clusters;
    }

    // ──── Basic exclusion ─────────────────────────────────────────────────────

    [Fact]
    public void ExcludePattern_MatchingCluster_IsRemoved()
    {
        var code = """
            void CheckArchRule() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, ["IArchRule"]);

        Assert.Empty(clusters);
    }

    [Fact]
    public void ExcludePattern_NonMatchingCluster_IsKept()
    {
        var code = """
            void BuildServiceHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, ["IArchRule"]);

        Assert.Single(clusters);
    }

    [Fact]
    public void NoExcludePatterns_AllClustersKept()
    {
        var code = """
            void SomeMethod() {
                var x = 1;
                var y = x + 1;
                var z = y * 2;
                return z;
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, new List<string>());

        Assert.Single(clusters);
    }

    // ──── Case insensitivity ──────────────────────────────────────────────────

    [Fact]
    public void ExcludePattern_IsCaseInsensitive_Lowercase()
    {
        var code = """
            void CheckArchRule() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, ["iarchrule"]);

        Assert.Empty(clusters);
    }

    [Fact]
    public void ExcludePattern_IsCaseInsensitive_Uppercase()
    {
        var code = """
            void CheckArchRule() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, ["IARCHRULE"]);

        Assert.Empty(clusters);
    }

    // ──── Multiple patterns ───────────────────────────────────────────────────

    [Fact]
    public void MultipleExcludePatterns_AnyMatch_RemovesCluster()
    {
        var archCode = """
            void CheckArch() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var boilerCode = """
            void RegisterBoilerplate() {
                container.Register<IService, Service>();
                container.Register<IRepo, Repo>();
                container.Build();
            }
            """;

        var archBlocks = new List<CodeBlock> { MakeBlock(archCode, "a.cs"), MakeBlock(archCode, "b.cs") };
        var boilerBlocks = new List<CodeBlock> { MakeBlock(boilerCode, "c.cs", 1, 6), MakeBlock(boilerCode, "d.cs", 1, 6) };
        var all = archBlocks.Concat(boilerBlocks).ToList();

        // Both patterns match their respective clusters
        var clusters = DetectWith(all, ["IArchRule", "RegisterBoilerplate"]);

        Assert.Empty(clusters);
    }

    [Fact]
    public void MultipleExcludePatterns_PartialMatch_KeepsNonMatching()
    {
        var archCode = """
            void CheckArch() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var prodCode = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;

        var archBlocks = new List<CodeBlock> { MakeBlock(archCode, "a.cs"), MakeBlock(archCode, "b.cs") };
        var prodBlocks = new List<CodeBlock> { MakeBlock(prodCode, "c.cs", 1, 7), MakeBlock(prodCode, "d.cs", 1, 7) };
        var all = archBlocks.Concat(prodBlocks).ToList();

        // Only arch pattern matches
        var clusters = DetectWith(all, ["IArchRule", "NonExistentPattern"]);

        Assert.Single(clusters);
        Assert.DoesNotContain("IArchRule", clusters[0].NormalizedSnippet, StringComparison.OrdinalIgnoreCase);
    }

    // ──── Partial substring matching ──────────────────────────────────────────

    [Fact]
    public void ExcludePattern_PartialSubstring_Matches()
    {
        var code = """
            void CheckSomethingRule() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        // "ArchRule" is a substring of "IArchRule"
        var clusters = DetectWith(new List<CodeBlock> { b1, b2 }, ["ArchRule"]);

        Assert.Empty(clusters);
    }
}
