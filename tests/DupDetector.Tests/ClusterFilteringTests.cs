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

    // ──── Min cluster spread filtering (GAP-2/3) ─────────────────────────────

    [Fact]
    public void MinSpread_Default1_KeepsAllClusters()
    {
        // Exact match across 1 file (spread=1)
        var code = """
            void SingleFileWork(int x, int y, int z) {
                var r = x * y + z;
                Console.WriteLine(r);
                return;
            }
            """;
        var block1 = MakeBlock(code, "single.cs", 1, 5);
        var block2 = MakeBlock(code, "single.cs", 10, 14);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.99,
            maxClusterSpread: 0, maxClusterOccurrences: 0, minClusterSpread: 1);

        Assert.NotEmpty(clusters);
    }

    [Fact]
    public void MinSpread_2_RemovesSingleFileExactClusters()
    {
        // Exact match across 1 file (spread=1) should be filtered when minSpread=2
        var code = """
            void SingleFileWork(int x, int y, int z) {
                var r = x * y + z;
                Console.WriteLine(r);
                return;
            }
            """;
        var block1 = MakeBlock(code, "single.cs", 1, 5);
        var block2 = MakeBlock(code, "single.cs", 10, 14);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.99,
            maxClusterSpread: 0, maxClusterOccurrences: 0, minClusterSpread: 2);

        // spread=1 cluster must be removed
        Assert.DoesNotContain(clusters, c => c.Metrics.Spread < 2);
    }

    [Fact]
    public void MinSpread_2_KeepsMultiFileClusters()
    {
        // Same code across 3 different files (spread=3) should survive minSpread=2
        var code = """
            void MultiFileWork(int x, int y, int z) {
                var r = x * y + z;
                Console.WriteLine(r);
                return;
            }
            """;
        var blocks = Enumerable.Range(0, 3)
            .Select(i => MakeBlock(code, $"file{i}.cs", 1, 5))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.99,
            maxClusterSpread: 0, maxClusterOccurrences: 0, minClusterSpread: 2);

        Assert.NotEmpty(clusters);
        Assert.Contains(clusters, c => c.Metrics.Spread == 3);
    }

    [Fact]
    public void MinSpread_AtExactlyRequired_IsKept()
    {
        // spread == minClusterSpread should be kept (boundary is inclusive)
        var code = """
            void BoundaryWork(int x, int y, int z) {
                var r = x + y + z;
                Console.WriteLine(r);
                return;
            }
            """;
        var blocks = Enumerable.Range(0, 3)
            .Select(i => MakeBlock(code, $"bound{i}.cs", 1, 5))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.99,
            maxClusterSpread: 0, maxClusterOccurrences: 0, minClusterSpread: 3);

        // spread=3 and minSpread=3 → cluster should survive
        Assert.NotEmpty(clusters);
    }

    [Fact]
    public void MinSpread_NearDupClusters_AlsoFiltered()
    {
        // Near-dup cluster with spread=1 should also be filtered
        var code1 = """
            void Work1(int x, int y, int z) {
                var alpha = x + y;
                var beta = alpha + z;
                Console.WriteLine(beta);
            }
            """;
        var code2 = """
            void Work2(int a, int b, int c) {
                var foo = a + b;
                var bar = foo + c;
                Console.WriteLine(bar);
            }
            """;
        // Both in same file → spread=1 after normalization makes them near-dups
        var block1 = MakeBlock(code1, "oneFile.cs", 1, 5);
        var block2 = MakeBlock(code2, "oneFile.cs", 10, 14);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.70,
            maxClusterSpread: 0, maxClusterOccurrences: 0, minClusterSpread: 2);

        Assert.DoesNotContain(clusters, c => c.Metrics.Spread < 2);
    }

    [Fact]
    public void MinSpread_CombinedWithMaxSpread_FiltersCorrectly()
    {
        // spread=1 filtered by minSpread, spread=10 filtered by maxSpread
        // Only spread in range [2,8] should survive
        var codeA = """
            void WorkA(int x, int y, int z) {
                var r1 = x * y;
                var r2 = r1 + z;
                Console.WriteLine(r2);
            }
            """;
        var codeB = """
            void WorkB(int a, int b, int c) {
                var r1 = a + b;
                var r2 = r1 * c;
                Console.WriteLine(r2);
            }
            """;

        // Cluster A: spread=1 (same file, exact dup)
        var blocksA = new List<CodeBlock>
        {
            MakeBlock(codeA, "same.cs", 1, 5),
            MakeBlock(codeA, "same.cs", 10, 14),
        };
        // Cluster B: spread=4
        var blocksB = Enumerable.Range(0, 4)
            .Select(i => MakeBlock(codeB, $"spreadB{i}.cs", 1, 5))
            .ToList();

        var allBlocks = blocksA.Concat(blocksB).ToList();
        var clusters = _detector.Detect(allBlocks, 0.99,
            maxClusterSpread: 8, maxClusterOccurrences: 0, minClusterSpread: 2);

        // Cluster A (spread=1) should be removed by minSpread
        Assert.DoesNotContain(clusters, c => c.Metrics.Spread < 2);
        // Cluster B (spread=4) should survive
        Assert.Contains(clusters, c => c.Metrics.Spread == 4);
    }
}

