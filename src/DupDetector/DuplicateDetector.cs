using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector;

/// <summary>
/// Detects exact and near-duplicate code blocks using hash grouping and Jaccard similarity.
/// </summary>
public class DuplicateDetector
{
    public List<DuplicateCluster> Detect(List<CodeBlock> blocks, double similarityThreshold)
    {
        var clusters = new List<DuplicateCluster>();

        // Step 1: Exact match detection - group by normalized hash
        var exactGroups = blocks
            .GroupBy(b => b.NormalizedHash)
            .Where(g => g.Count() >= 2)
            .ToList();

        var assignedBlocks = new HashSet<CodeBlock>();

        foreach (var group in exactGroups)
        {
            var instances = group.OrderBy(b => b.FilePath).ThenBy(b => b.StartLine).ToList();
            var cluster = BuildCluster(instances, group.Key);
            clusters.Add(cluster);
            foreach (var b in instances) assignedBlocks.Add(b);
        }

        // Step 2: Near-duplicate detection for unassigned blocks
        if (similarityThreshold < 1.0)
        {
            var remaining = blocks.Where(b => !assignedBlocks.Contains(b)).ToList();
            var nearDupClusters = DetectNearDuplicates(remaining, similarityThreshold);
            clusters.AddRange(nearDupClusters);
        }

        // Step 3: Rank clusters by score descending
        return clusters.OrderByDescending(c => c.Metrics.Score).ToList();
    }

    private List<DuplicateCluster> DetectNearDuplicates(List<CodeBlock> blocks, double threshold)
    {
        if (blocks.Count < 2) return new List<DuplicateCluster>();

        // Precompute token sets for Jaccard similarity
        var tokenSets = blocks.Select(b => TokenSet(b.NormalizedText)).ToList();

        // Union-Find for clustering
        var parent = Enumerable.Range(0, blocks.Count).ToArray();

        for (int i = 0; i < blocks.Count; i++)
        {
            for (int j = i + 1; j < blocks.Count; j++)
            {
                var similarity = Jaccard(tokenSets[i], tokenSets[j]);
                if (similarity >= threshold)
                {
                    Union(parent, i, j);
                }
            }
        }

        // Group by root component
        var groups = new Dictionary<int, List<int>>();
        for (int i = 0; i < blocks.Count; i++)
        {
            var root = Find(parent, i);
            if (!groups.TryGetValue(root, out var list))
            {
                list = new List<int>();
                groups[root] = list;
            }
            list.Add(i);
        }

        var clusters = new List<DuplicateCluster>();
        foreach (var group in groups.Values)
        {
            if (group.Count < 2) continue;
            var groupBlocks = group.Select(i => blocks[i])
                                   .OrderBy(b => b.FilePath)
                                   .ThenBy(b => b.StartLine)
                                   .ToList();
            // Use hash of the first (canonical) block
            var cluster = BuildCluster(groupBlocks, groupBlocks[0].NormalizedHash);
            clusters.Add(cluster);
        }

        return clusters;
    }

    private static DuplicateCluster BuildCluster(List<CodeBlock> instances, string hashKey)
    {
        var id = $"dup-{hashKey[..Math.Min(8, hashKey.Length)]}";

        var codeInstances = instances.Select(b => new CodeInstance
        {
            File = b.FilePath,
            StartLine = b.StartLine,
            EndLine = b.EndLine,
            Method = b.MethodName,
            Hash = b.NormalizedHash
        }).ToList();

        var avgLines = (int)Math.Round(instances.Average(b => b.LineCount));
        var occurrences = instances.Count;
        var spread = instances.Select(b => b.FilePath).Distinct().Count();
        var score = (avgLines * occurrences * spread) / 100.0;

        // Normalized 0–100 score: capped product of block size, spread, and occurrences.
        // Max bucket: 50 lines × 10 occurrences × 5 files = 2500 → maps to 100.
        var duplicationScore = Math.Round(
            Math.Min(100.0, (Math.Min(avgLines, 50) * Math.Min(occurrences, 10) * Math.Min(spread, 5)) / 25.0),
            2);

        var metrics = new ClusterMetrics
        {
            Lines = avgLines,
            Occurrences = occurrences,
            Spread = spread,
            Score = score,
            DuplicationScore = duplicationScore
        };

        return new DuplicateCluster
        {
            Id = id,
            Instances = codeInstances,
            Metrics = metrics,
            NormalizedSnippet = instances[0].NormalizedText,
            RawSnippets = instances.Select(b => b.RawText).ToList()
        };
    }

    private static HashSet<string> TokenSet(string normalizedText)
    {
        // Tokenize by splitting on whitespace and punctuation
        return normalizedText
            .Split(new[] { ' ', '\t', '\n', '\r', '{', '}', '(', ')', ';', ',', '.', '[', ']' },
                   StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0;
        var intersection = a.Count(x => b.Contains(x));
        var union = a.Union(b).Count();
        return union == 0 ? 0.0 : (double)intersection / union;
    }

    private static int Find(int[] parent, int x)
    {
        if (parent[x] != x) parent[x] = Find(parent, parent[x]);
        return parent[x];
    }

    private static void Union(int[] parent, int x, int y)
    {
        var rx = Find(parent, x);
        var ry = Find(parent, y);
        if (rx != ry) parent[rx] = ry;
    }
}
