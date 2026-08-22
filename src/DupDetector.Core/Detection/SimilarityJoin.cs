using System.Collections.Concurrent;

namespace DupDetector.Core.Detection;

/// <summary>A pair of block indices found to meet the similarity threshold.</summary>
public readonly record struct SimilarPair(int Left, int Right, double Similarity);

/// <summary>
/// Finds every pair of blocks whose multiset Jaccard similarity meets a threshold.
/// </summary>
// Candidates come from an inverted token index, pruned by shared tokens and by size ratio.
// Both pruning rules are exact, so the result equals an all-pairs comparison.
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
