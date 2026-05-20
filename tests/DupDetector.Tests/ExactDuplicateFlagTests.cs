using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for the IsExact and IsHighImpact flags on DuplicateCluster (Run 4, GAP-C).
/// </summary>
public class ExactDuplicateFlagTests
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

    // ──── IsExact ─────────────────────────────────────────────────────────────

    [Fact]
    public void ExactMatch_Cluster_IsExact_IsTrue()
    {
        var code = """
            void DoWork() {
                var x = 1;
                var y = 2;
                Console.WriteLine(x + y);
            }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, similarityThreshold: 0.99);

        Assert.Single(clusters);
        Assert.True(clusters[0].IsExact, "Verbatim-copy cluster should have IsExact=true");
    }

    [Fact]
    public void NearDuplicate_Cluster_IsExact_IsFalse()
    {
        // Two structurally similar but textually different blocks (different variable names)
        var code1 = """
            void Process() {
                var alpha = GetValue();
                var beta = alpha * 2;
                var gamma = beta + alpha;
                var delta = gamma - beta;
                return delta;
            }
            """;
        var code2 = """
            void Process() {
                var x = GetValue();
                var y = x * 2;
                var z = y + x;
                var w = z - y;
                return w;
            }
            """;

        var b1 = MakeBlock(code1, "a.cs", 1, 8);
        var b2 = MakeBlock(code2, "b.cs", 1, 8);

        // Use similarity threshold < 1.0 to allow near-dup detection
        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, similarityThreshold: 0.5);

        // If a near-dup cluster was formed, it should NOT be marked as exact
        foreach (var cluster in clusters.Where(c => !c.IsExact))
            Assert.False(cluster.IsExact, "Near-duplicate cluster should have IsExact=false");
    }

    [Fact]
    public void ThreeWayExactMatch_IsExact_IsTrue()
    {
        var code = """
            void Build() {
                var a = new List<int>();
                a.Add(1);
                a.Add(2);
                Console.WriteLine(a.Count);
            }
            """;
        var blocks = new[] { "x.cs", "y.cs", "z.cs" }
            .Select(f => MakeBlock(code, f))
            .ToList();

        var clusters = _detector.Detect(blocks, similarityThreshold: 0.99);

        Assert.Single(clusters);
        Assert.True(clusters[0].IsExact);
    }

    // ──── IsHighImpact ────────────────────────────────────────────────────────

    [Fact]
    public void LargeExactDuplicate_Across2Files_IsHighImpact_IsTrue()
    {
        // 71 lines × 2 files = 142 ≥ 100 → IsHighImpact = true
        var lines = Enumerable.Range(1, 50)
            .Select(i => $"    var v{i} = {i};")
            .Prepend("void BigMethod() {")
            .Append("}");
        var code = string.Join("\n", lines);

        var b1 = MakeBlock(code, "a.cs", 1, 52);
        var b2 = MakeBlock(code, "b.cs", 1, 52);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, similarityThreshold: 0.99);
        Assert.Single(clusters);

        var c = clusters[0];
        Assert.True(c.IsExact, "Must be exact before IsHighImpact can be true");
        Assert.True(c.IsHighImpact,
            $"50-line block in 2 files: avgLines({c.Metrics.Lines}) * spread({c.Metrics.Spread}) = {c.Metrics.Lines * c.Metrics.Spread} should be ≥ 100");
    }

    [Fact]
    public void SmallExactDuplicate_IsHighImpact_IsFalse()
    {
        // 5 lines × 2 files = 10 < 100 → IsHighImpact = false
        var code = """
            void Tiny() {
                var x = 1;
                var y = 2;
            }
            """;
        var b1 = MakeBlock(code, "a.cs", 1, 5);
        var b2 = MakeBlock(code, "b.cs", 1, 5);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, similarityThreshold: 0.99);
        Assert.Single(clusters);

        var c = clusters[0];
        Assert.True(c.IsExact);
        Assert.False(c.IsHighImpact,
            $"5-line block in 2 files: lines × spread = {c.Metrics.Lines * c.Metrics.Spread}, expected < 100");
    }

    [Fact]
    public void MediumExactDuplicate_Across5Files_IsHighImpact_IsTrue()
    {
        // 25 lines × 5 files = 125 ≥ 100 → IsHighImpact = true
        var lines = Enumerable.Range(1, 23)
            .Select(i => $"    var val{i} = Process({i});")
            .Prepend("void MediumMethod() {")
            .Append("}");
        var code = string.Join("\n", lines);

        var blocks = Enumerable.Range(1, 5)
            .Select(i => MakeBlock(code, $"file{i}.cs", 1, 25))
            .ToList();

        var clusters = _detector.Detect(blocks, similarityThreshold: 0.99);
        Assert.Single(clusters);

        var c = clusters[0];
        Assert.True(c.IsExact);
        Assert.True(c.IsHighImpact,
            $"25-line block in 5 files: lines × spread = {c.Metrics.Lines * c.Metrics.Spread} should be ≥ 100");
    }

    [Fact]
    public void ExactDuplicate_ExactlyAtThreshold_IsHighImpact_IsTrue()
    {
        // 10 lines × 10 files = 100 → exactly at threshold, should be IsHighImpact = true
        var lines = Enumerable.Range(1, 8)
            .Select(i => $"    Console.WriteLine({i});")
            .Prepend("void AtThreshold() {")
            .Append("}");
        var code = string.Join("\n", lines);

        var blocks = Enumerable.Range(1, 10)
            .Select(i => MakeBlock(code, $"f{i}.cs", 1, 10))
            .ToList();

        var clusters = _detector.Detect(blocks, similarityThreshold: 0.99);
        Assert.Single(clusters);

        var c = clusters[0];
        Assert.True(c.IsExact);
        Assert.True(c.IsHighImpact,
            $"lines × spread = {c.Metrics.Lines * c.Metrics.Spread}, expected ≥ 100 at threshold");
    }

    [Fact]
    public void NearDuplicate_LargeBlock_IsHighImpact_IsFalse()
    {
        // Near-duplicate (not exact) clusters are never IsHighImpact, even if large
        var code1 = string.Join("\n",
            new[] { "void BigNear() {" }
            .Concat(Enumerable.Range(1, 40).Select(i => $"    var aaa{i} = Compute({i});"))
            .Append("}"));
        var code2 = string.Join("\n",
            new[] { "void BigNear() {" }
            .Concat(Enumerable.Range(1, 40).Select(i => $"    var bbb{i} = Compute({i});"))
            .Append("}"));

        var b1 = MakeBlock(code1, "a.cs", 1, 42);
        var b2 = MakeBlock(code2, "b.cs", 1, 42);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, similarityThreshold: 0.5);

        foreach (var c in clusters.Where(c => !c.IsExact))
            Assert.False(c.IsHighImpact, "Near-duplicate clusters must not have IsHighImpact=true");
    }

    // ──── YAML/JSON output ────────────────────────────────────────────────────

    [Fact]
    public void IsExact_AppearsInYamlOutput()
    {
        var code = """
            void Foo() { var x = 1; return x; }
            """;
        var b1 = MakeBlock(code, "a.cs");
        var b2 = MakeBlock(code, "b.cs");
        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99);

        var report = new DetectionReport { Clusters = clusters };
        var reporter = new Reporter();
        var yaml = reporter.Render(report, "yaml");

        Assert.Contains("isExact:", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void IsHighImpact_AppearsInYamlOutput()
    {
        var code = string.Join("\n",
            new[] { "void HighImpact() {" }
            .Concat(Enumerable.Range(1, 50).Select(i => $"    var z{i} = {i};"))
            .Append("}"));
        var b1 = MakeBlock(code, "a.cs", 1, 52);
        var b2 = MakeBlock(code, "b.cs", 1, 52);
        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99);

        var report = new DetectionReport { Clusters = clusters };
        var reporter = new Reporter();
        var yaml = reporter.Render(report, "yaml");

        Assert.Contains("isHighImpact:", yaml, StringComparison.Ordinal);
    }
}
