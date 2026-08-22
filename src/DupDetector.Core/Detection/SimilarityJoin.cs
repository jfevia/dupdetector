using System.Collections.Concurrent;

namespace DupDetector.Core.Detection;

/// <summary>A pair of block indices found to meet the similarity threshold.</summary>
public readonly record struct SimilarPair(int Left, int Right, double Similarity);

/// <summary>
/// Finds every pair of blocks whose multiset Jaccard similarity meets a threshold.
/// </summary>
/// <remarks>
/// <para>
/// Candidates come from an inverted token index instead of comparing all pairs. Two pruning rules
/// make that safe, and both are exact rather than probabilistic:
/// </para>
/// <list type="bullet">
/// <item>a pair sharing no token has similarity zero, so it cannot meet a positive threshold;</item>
/// <item>similarity can never exceed <c>min(|a|,|b|) / max(|a|,|b|)</c>, so pairs whose sizes differ
/// by more than the threshold allows are skipped.</item>
/// </list>
/// <para>
/// Neither rule can discard a qualifying pair, so the result is identical to comparing every pair.
/// Nothing here samples, hashes or approximates.
/// </para>
/// </remarks>
public static class SimilarityJoin
{
    /// <summary>
    /// Returns all qualifying pairs, ordered by <see cref="SimilarPair.Left"/> then
    /// <see cref="SimilarPair.Right"/> so results never depend on scheduling.
    /// </summary>
    public static IReadOnlyList<SimilarPair> FindPairs(IReadOnlyList<TokenMultiset> blocks, double threshold)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        if (blocks.Count < 2)
        {
            return [];
        }

        var index = BuildIndex(blocks);
        var found = new ConcurrentBag<SimilarPair>();

        Parallel.For(0, blocks.Count, left => Probe(blocks, index, left, threshold, found));

        var pairs = found.ToArray();
        Array.Sort(pairs, static (a, b) => a.Left == b.Left ? a.Right.CompareTo(b.Right) : a.Left.CompareTo(b.Left));
        return pairs;
    }

    private static Dictionary<int, List<int>> BuildIndex(IReadOnlyList<TokenMultiset> blocks)
    {
        var index = new Dictionary<int, List<int>>();
        for (var position = 0; position < blocks.Count; position++)
        {
            foreach (var token in blocks[position].Ids)
            {
                if (!index.TryGetValue(token, out var postings))
                {
                    postings = [];
                    index[token] = postings;
                }

                postings.Add(position);
            }
        }

        return index;
    }

    private static void Probe(
        IReadOnlyList<TokenMultiset> blocks,
        Dictionary<int, List<int>> index,
        int left,
        double threshold,
        ConcurrentBag<SimilarPair> found)
    {
        var source = blocks[left];
        var candidates = new HashSet<int>();

        foreach (var token in source.Ids)
        {
            foreach (var candidate in index[token])
            {
                // Each unordered pair is examined once, from its lower index.
                if (candidate > left && Similarity.UpperBound(source.Cardinality, blocks[candidate].Cardinality) >= threshold)
                {
                    candidates.Add(candidate);
                }
            }
        }

        foreach (var candidate in candidates)
        {
            var similarity = Similarity.Jaccard(source, blocks[candidate]);
            if (similarity >= threshold)
            {
                found.Add(new SimilarPair(left, candidate, similarity));
            }
        }
    }
}
