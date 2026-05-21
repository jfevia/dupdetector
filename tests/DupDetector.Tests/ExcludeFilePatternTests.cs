using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for <c>--exclude-file-pattern</c> (GAP-L) and the <see cref="FilePatternMatcher"/> helper.
/// Verifies that clusters whose every instance resides in a matching file are suppressed,
/// while clusters spanning both matching and non-matching files are preserved.
/// </summary>
public class ExcludeFilePatternTests
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

    private List<DuplicateCluster> DetectWithFilePatterns(List<CodeBlock> blocks, List<string> patterns,
        double similarity = 0.99, int minSpread = 1, int minProjSpread = 1)
    {
        var clusters = _detector.Detect(blocks, similarity,
            minClusterSpread: minSpread, minProjectSpread: minProjSpread);

        if (patterns.Count > 0)
        {
            clusters = clusters
                .Where(c => !c.Instances.All(inst =>
                    patterns.Any(p => FilePatternMatcher.IsMatch(p, inst.File))))
                .ToList();
        }

        return clusters;
    }

    // ──── FilePatternMatcher unit tests ───────────────────────────────────────

    [Fact]
    public void GlobToRegex_DoubleStar_MatchesZeroSegments()
    {
        // "**/Arch/*.cs" should match "Arch/Foo.cs" (zero leading segments)
        Assert.True(FilePatternMatcher.IsMatch("**/Arch/*.cs", "Arch/Foo.cs"));
    }

    [Fact]
    public void GlobToRegex_DoubleStar_MatchesOneSegment()
    {
        Assert.True(FilePatternMatcher.IsMatch("**/Arch/*.cs", "tests/Arch/Foo.cs"));
    }

    [Fact]
    public void GlobToRegex_DoubleStar_MatchesMultipleSegments()
    {
        Assert.True(FilePatternMatcher.IsMatch("**/Arch/*.cs", "src/ProjectA/tests/Arch/FooTests.cs"));
    }

    [Fact]
    public void GlobToRegex_DoubleStar_AbsolutePath()
    {
        // Absolute Windows path normalised to forward slash
        Assert.True(FilePatternMatcher.IsMatch("**/Arch/*.cs",
            "C:/Users/dev/repo/src/ProjectA.Tests/Arch/SomeRule.cs"));
    }

    [Fact]
    public void GlobToRegex_SingleStar_DoesNotCrossSegment()
    {
        // "*.cs" should match "Foo.cs" but not "sub/Foo.cs"
        Assert.True(FilePatternMatcher.IsMatch("*.cs", "Foo.cs"));
        Assert.False(FilePatternMatcher.IsMatch("*.cs", "sub/Foo.cs"));
    }

    [Fact]
    public void GlobToRegex_IsCaseInsensitive()
    {
        Assert.True(FilePatternMatcher.IsMatch("**/arch/*.cs", "tests/ARCH/RuleTest.cs"));
    }

    [Fact]
    public void GlobToRegex_BackslashNormalized()
    {
        // Windows path with backslashes should be normalised and matched
        Assert.True(FilePatternMatcher.IsMatch("**/Arch/*.cs",
            @"src\ProjectA.Tests\Arch\SomeRule.cs"));
    }

    // ──── Cluster-level filtering ─────────────────────────────────────────────

    [Fact]
    public void AllInstancesMatch_ClusterIsExcluded()
    {
        var code = """
            void CheckRule() {
                IArchRule rule = Classes().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, @"tests\ProjectA.Tests\Arch\PublicClassesTests.cs");
        var b2 = MakeBlock(code, @"tests\ProjectB.Tests\Arch\PublicClassesTests.cs");

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 }, ["**/Arch/*.cs"]);

        Assert.Empty(clusters);
    }

    [Fact]
    public void NoInstancesMatch_ClusterIsKept()
    {
        var code = """
            void BuildServiceHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"src\ProjectA\Host.cs");
        var b2 = MakeBlock(code, @"src\ProjectB\Host.cs");

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 }, ["**/Arch/*.cs"]);

        Assert.Single(clusters);
    }

    [Fact]
    public void MixedInstances_OneMatchOneNonMatch_ClusterIsKept()
    {
        // Only ALL-matching clusters are excluded; mixed clusters are preserved
        var code = """
            void Configure() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                svc.AddSingleton<IDb, Db>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"tests\ProjectA.Tests\Arch\ConfigTests.cs");  // matches
        var b2 = MakeBlock(code, @"src\ProjectB\Startup.cs");                   // no match

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 }, ["**/Arch/*.cs"]);

        Assert.Single(clusters);
    }

    [Fact]
    public void NoFilePatterns_AllClustersKept()
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

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 }, []);

        Assert.Single(clusters);
    }

    [Fact]
    public void MultiplePatterns_AnyMatchAcrossAllInstances_ClusterIsExcluded()
    {
        // b1 matches pattern1, b2 matches pattern2 → all instances matched (different patterns) → excluded
        var code = """
            void CheckBoilerplate() {
                IArchRule rule = Types().Should().BeSealed();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, @"tests\ProjectA\Arch\SealedTests.cs");
        var b2 = MakeBlock(code, @"tests\ProjectB\Specs\SealedTests.cs");

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 },
            ["**/Arch/*.cs", "**/Specs/*.cs"]);

        Assert.Empty(clusters);
    }

    [Fact]
    public void MultiplePatterns_PartialMatch_ClusterIsKept()
    {
        // b1 matches, b2 does NOT match either pattern → cluster kept
        var code = """
            void Configure() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                svc.AddSingleton<IDb, Db>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"tests\ProjectA.Tests\Arch\StartupTests.cs");
        var b2 = MakeBlock(code, @"src\ProjectB\Startup.cs");

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2 },
            ["**/Arch/*.cs", "**/Specs/*.cs"]);

        Assert.Single(clusters);
    }

    [Fact]
    public void ThreeInstances_TwoMatchOneDoesNot_ClusterIsKept()
    {
        var code = """
            void VerifyRule() {
                IArchRule rule = Types().Should().BePublic();
                rule.Check(Architecture);
            }
            """;
        var b1 = MakeBlock(code, @"tests\A.Tests\Arch\RuleA.cs");    // matches
        var b2 = MakeBlock(code, @"tests\B.Tests\Arch\RuleB.cs");    // matches
        var b3 = MakeBlock(code, @"src\Shared\RuleHelper.cs");       // no match

        var clusters = DetectWithFilePatterns(new List<CodeBlock> { b1, b2, b3 }, ["**/Arch/*.cs"]);

        Assert.Single(clusters);
    }
}
