using System.Collections.Concurrent;

namespace DupDetector.Core.Detection;

/// <summary>
///     Finds every pair of blocks whose multiset Jaccard similarity meets a threshold.
/// </summary>
public static class SimilarityJoin
{
    /// <summary>
    ///     Returns all qualifying pairs in a deterministic order.
    /// </summary>
    /// <param name="blocks">The token multisets to compare.</param>
    /// <param name="threshold">The similarity a pair must reach.</param>
    /// <returns>The qualifying pairs, ordered by left index then right index.</returns>
    public static IReadOnlyList<SimilarPair> FindPairs(IReadOnlyList<TokenMultiset> blocks, double threshold)
    {
        if (blocks.Count < 2)
        {
            return [];
        }

        var index = BuildIndex(blocks);
        var found = new ConcurrentBag<SimilarPair>();
        var probe = new Probe(blocks, index, threshold, found);

        Parallel.For(0, blocks.Count, probe.Run);

        var pairs = found.ToArray();
        Array.Sort(pairs, ComparePairs);
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

    private static int ComparePairs(SimilarPair first, SimilarPair second)
    {
        return first.Left == second.Left
            ? first.Right.CompareTo(second.Right)
            : first.Left.CompareTo(second.Left);
    }

    /// <summary>
    ///     Compares one block against every candidate that shares a token with it.
    /// </summary>
    private sealed class Probe
    {
        private readonly IReadOnlyList<TokenMultiset> _blocks;
        private readonly ConcurrentBag<SimilarPair> _found;
        private readonly Dictionary<int, List<int>> _index;
        private readonly double _threshold;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Probe"/> class.
        /// </summary>
        /// <param name="blocks">The token multisets to compare.</param>
        /// <param name="index">The inverted token index.</param>
        /// <param name="threshold">The similarity a pair must reach.</param>
        /// <param name="found">The bag qualifying pairs are added to.</param>
        public Probe(
            IReadOnlyList<TokenMultiset> blocks,
            Dictionary<int, List<int>> index,
            double threshold,
            ConcurrentBag<SimilarPair> found)
        {
            _blocks = blocks;
            _index = index;
            _threshold = threshold;
            _found = found;
        }

        /// <summary>
        ///     Compares the block at <paramref name="left"/> against every later candidate.
        /// </summary>
        /// <param name="left">The index of the block to probe.</param>
        public void Run(int left)
        {
            var source = _blocks[left];
            var candidates = new HashSet<int>();

            foreach (var token in source.Ids)
            {
                foreach (var candidate in _index[token])
                {
                    if (candidate > left &&
                        Similarity.UpperBound(source.Cardinality, _blocks[candidate].Cardinality) >= _threshold)
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                var similarity = Similarity.Jaccard(source, _blocks[candidate]);
                if (similarity >= _threshold)
                {
                    var pair = new SimilarPair(left, candidate, similarity);
                    _found.Add(pair);
                }
            }
        }
    }
}
