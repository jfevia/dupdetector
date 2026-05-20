using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests that verify the cluster size filter in <see cref="DuplicateDetector.Detect"/>.
/// Addresses GAP-4: oversized near-duplicate clusters (e.g., 4,462 occurrences) that
/// arise from generic structural matching at low similarity thresholds.
/// </summary>
public class ClusterFilteringTests
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

    /// <summary>
    /// Creates a block with a base body plus <paramref name="extraCalls"/> additional
    /// method calls appended. Each block has a unique hash but chains to adjacent blocks
    /// via Jaccard similarity, forming near-duplicate clusters at threshold ≤ 0.70.
    /// </summary>
    private CodeBlock MakeChainedNearDupBlock(string file, int extraCalls)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("void M() {");
        sb.AppendLine("    var a = Load();");
        sb.AppendLine("    if (a == null) return;");
        sb.AppendLine("    Execute(a);");
        for (int i = 0; i < extraCalls; i++)
            sb.AppendLine($"    Extra{i}(a);");
        sb.AppendLine("}");
        return MakeBlock(sb.ToString(), file, 1, 5 + extraCalls);
    }

    // ──── Max spread filtering ────────────────────────────────────────────────

    [Fact]
    public void NearDuplicateCluster_ExceedingMaxSpread_IsDiscarded()
    {
        // 10 near-dup blocks across 10 different files, consecutive Jaccard > 0.85
        var blocks = Enumerable.Range(0, 10)
            .Select(i => MakeChainedNearDupBlock($"file{i}.cs", i))
            .ToList();

        // maxClusterSpread = 5 → a cluster spanning all 10 files should be filtered
        var clusters = _detector.Detect(blocks, 0.70, maxClusterSpread: 5, maxClusterOccurrences: 0);

        var oversized = clusters.Where(c => c.Metrics.Spread > 5).ToList();
        Assert.Empty(oversized);
    }

    [Fact]
    public void NearDuplicateCluster_BelowMaxSpread_IsKept()
    {
        // Exact-match blocks across 5 files (spread = 5), cap = 20 → should be kept
        var code = """
            void Process(int a, int b, int c) {
                var result = a + b + c;
                Console.WriteLine(result);
            }
            """;
        var blocks = Enumerable.Range(0, 5)
            .Select(i => MakeBlock(code, $"file{i}.cs", 1, 4))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.85, maxClusterSpread: 20, maxClusterOccurrences: 0);

        Assert.NotEmpty(clusters);
        Assert.Contains(clusters, c => c.Metrics.Spread == 5);
    }

    [Fact]
    public void NearDuplicateCluster_AtExactlyMaxSpread_IsKept()
    {
        // Boundary: spread == maxClusterSpread (not-strictly-greater-than → should be kept)
        var code = """
            void Compute(int x, int y, int z) {
                var total = x + y + z;
                Console.WriteLine(total);
                return;
            }
            """;
        var blocks = Enumerable.Range(0, 5)
            .Select(i => MakeBlock(code, $"file{i}.cs", 1, 5))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.85, maxClusterSpread: 5, maxClusterOccurrences: 0);

        Assert.NotEmpty(clusters);
    }

    // ──── Max occurrences filtering ───────────────────────────────────────────

    [Fact]
    public void NearDuplicateCluster_ExceedingMaxOccurrences_IsDiscarded()
    {
        // 10 near-dup blocks all in one file (many occurrences, spread=1)
        var blocks = Enumerable.Range(0, 10)
            .Select(i => MakeChainedNearDupBlock("bigfile.cs", i))
            .ToList();

        // maxClusterOccurrences = 5 → a cluster with 10 occurrences should be filtered
        var clusters = _detector.Detect(blocks, 0.70, maxClusterSpread: 0, maxClusterOccurrences: 5);

        var oversized = clusters.Where(c => c.Metrics.Occurrences > 5).ToList();
        Assert.Empty(oversized);
    }

    [Fact]
    public void NearDuplicateCluster_BelowMaxOccurrences_IsKept()
    {
        var code = """
            void DoWork(int a, int b, int c) {
                var total = a * b + c;
                Console.WriteLine(total);
            }
            """;
        var blocks = Enumerable.Range(0, 5)
            .Select(i => MakeBlock(code, $"f{i}.cs", 1, 4))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.85, maxClusterSpread: 0, maxClusterOccurrences: 50);

        Assert.NotEmpty(clusters);
    }

    [Fact]
    public void MaxSpreadZero_MeansNoLimit()
    {
        // Unlimited spread should produce same or more cluster occurrences than limited
        var blocks = Enumerable.Range(0, 10)
            .Select(i => MakeChainedNearDupBlock($"file{i}.cs", i))
            .ToList();

        var clustersLimited = _detector.Detect(blocks, 0.70, maxClusterSpread: 3, maxClusterOccurrences: 0);
        var clustersUnlimited = _detector.Detect(blocks, 0.70, maxClusterSpread: 0, maxClusterOccurrences: 0);

        var limitedTotal = clustersLimited.Sum(c => c.Metrics.Occurrences);
        var unlimitedTotal = clustersUnlimited.Sum(c => c.Metrics.Occurrences);
        Assert.True(unlimitedTotal >= limitedTotal,
            $"Unlimited ({unlimitedTotal}) should have >= occurrences than limited ({limitedTotal})");
    }

    [Fact]
    public void MaxOccurrencesZero_MeansNoLimit()
    {
        var blocks = Enumerable.Range(0, 10)
            .Select(i => MakeChainedNearDupBlock("bigfile.cs", i))
            .ToList();

        var clustersLimited = _detector.Detect(blocks, 0.70, maxClusterSpread: 0, maxClusterOccurrences: 5);
        var clustersUnlimited = _detector.Detect(blocks, 0.70, maxClusterSpread: 0, maxClusterOccurrences: 0);

        var limitedTotal = clustersLimited.Sum(c => c.Metrics.Occurrences);
        var unlimitedTotal = clustersUnlimited.Sum(c => c.Metrics.Occurrences);
        Assert.True(unlimitedTotal >= limitedTotal,
            $"Unlimited ({unlimitedTotal}) should have >= occurrences than limited ({limitedTotal})");
    }

    // ──── Exact-match clusters are NOT filtered by size caps ─────────────────

    [Fact]
    public void ExactMatchCluster_IsNeverFilteredBySizeCap()
    {
        // Build a large exact-match cluster (same hash, many files)
        // Exact-match clusters represent genuine duplicates and must never be filtered.
        var code = """
            void DoWork(int a, int b, int c, int d, int e) {
                var sum = a + b + c + d + e;
                Console.WriteLine(sum);
                if (sum > 100) throw new InvalidOperationException();
                return;
            }
            """;
        var blocks = Enumerable.Range(0, 30)
            .Select(i => MakeBlock(code, $"file{i}.cs", 1, 6))
            .ToList();

        // Even with very tight caps, exact-match clusters should not be filtered
        var clusters = _detector.Detect(blocks, 0.99, maxClusterSpread: 5, maxClusterOccurrences: 10);

        Assert.NotEmpty(clusters);
        Assert.Contains(clusters, c => c.Metrics.Occurrences == 30);
    }
}
