using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector;

/// <summary>
/// Detects exact and near-duplicate code blocks using hash grouping and Jaccard similarity.
/// </summary>
public class DuplicateDetector
{
    /// <summary>
    /// Detects duplicate clusters from the given code blocks.
    /// </summary>
    /// <param name="blocks">All extracted code blocks to analyse.</param>
    /// <param name="similarityThreshold">Jaccard threshold for near-duplicate grouping.</param>
    /// <param name="maxClusterSpread">
    /// Discard near-duplicate clusters whose file spread exceeds this value.
    /// 0 means no limit. Applies only to near-duplicate clusters, not exact-match clusters.
    /// </param>
    /// <param name="maxClusterOccurrences">
    /// Discard near-duplicate clusters whose occurrence count exceeds this value.
    /// 0 means no limit. Applies only to near-duplicate clusters, not exact-match clusters.
    /// </param>
    /// <param name="minClusterSpread">
    /// Discard clusters whose file spread is below this value.
    /// Default: 1 (keep all clusters). Applies to both exact-match and near-duplicate clusters.
    /// </param>
    public List<DuplicateCluster> Detect(
        List<CodeBlock> blocks,
        double similarityThreshold,
        int maxClusterSpread = 0,
        int maxClusterOccurrences = 0,
        int minClusterSpread = 1)
    {
        var clusters = new List<DuplicateCluster>();

        // Step 1: Exact match detection — group by normalized hash
        var exactGroups = blocks
            .GroupBy(b => b.NormalizedHash)
            .Where(g => g.Count() >= 2)
            .ToList();

        var assignedBlocks = new HashSet<CodeBlock>();

        foreach (var group in exactGroups)
        {
            var instances = group.OrderBy(b => b.FilePath).ThenBy(b => b.StartLine).ToList();
            var cluster = BuildCluster(instances, group.Key);
            // Exact-match clusters below minClusterSpread are skipped but NOT added to assignedBlocks.
            // This keeps their blocks eligible for the near-dup phase, where they may form a larger
            // cross-file cluster that does meet the spread requirement.
            if (cluster.Metrics.Spread < minClusterSpread) continue;
            clusters.Add(cluster);
            foreach (var b in instances) assignedBlocks.Add(b);
        }

        // Step 2: Near-duplicate detection for unassigned blocks
        if (similarityThreshold < 1.0)
        {
            var remaining = blocks.Where(b => !assignedBlocks.Contains(b)).ToList();
            var nearDupClusters = DetectNearDuplicates(remaining, similarityThreshold);

            // Filter out oversized clusters that are likely generic-pattern false positives
            foreach (var cluster in nearDupClusters)
            {
                bool tooLarge =
                    (maxClusterSpread > 0 && cluster.Metrics.Spread > maxClusterSpread) ||
                    (maxClusterOccurrences > 0 && cluster.Metrics.Occurrences > maxClusterOccurrences);
                bool tooSmall = cluster.Metrics.Spread < minClusterSpread;

                if (!tooLarge && !tooSmall)
                    clusters.Add(cluster);
            }
        }

        // Step 3: Rank clusters by score descending
        return clusters.OrderByDescending(c => c.Metrics.Score).ToList();
    }

    private List<DuplicateCluster> DetectNearDuplicates(List<CodeBlock> blocks, double threshold)
    {
        if (blocks.Count < 2) return new List<DuplicateCluster>();

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

        // Normalized 0-100 score: product of block size, occurrence count, and spread.
        // Inner caps are set higher (occ=25, spread=10) than the old formula (occ=10, spread=5)
        // so that clusters exceeding the old thresholds are differentiated rather than all
        // saturating at 100.  The divisor (50) is adjusted proportionally to keep "normal"
        // clusters in a sensible range.
        var duplicationScore = Math.Round(
            Math.Min(100.0, (Math.Min(avgLines, 50) * Math.Min(occurrences, 25) * Math.Min(spread, 10)) / 50.0),
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
