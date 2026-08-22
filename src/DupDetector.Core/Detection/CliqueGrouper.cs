namespace DupDetector.Core.Detection;

/// <summary>
///     Turns similar pairs into groups of mutually similar blocks.
/// </summary>
public static class CliqueGrouper
{
    /// <summary>
    ///     Returns every maximal group of at least two mutually similar blocks.
    /// </summary>
    /// <param name="blockCount">The number of blocks the pair indices refer to.</param>
    /// <param name="pairs">The similar pairs found by the join.</param>
    /// <param name="budget">The ceiling on enumeration work.</param>
    /// <returns>The groups, ordered deterministically.</returns>
    public static IReadOnlyList<SimilarityGroup> Group(
        int blockCount,
        IReadOnlyList<SimilarPair> pairs,
        CliqueBudget budget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blockCount);

        if (pairs.Count == 0)
        {
            return [];
        }

        var neighbours = BuildAdjacency(blockCount, pairs);
        var groups = new List<SimilarityGroup>();

        foreach (var component in Components(blockCount, neighbours))
        {
            AddComponent(component, neighbours, budget, groups);
        }

        groups.Sort(CompareGroups);
        return groups;
    }

    private static void AddComponent(
        List<int> component,
        Dictionary<int, HashSet<int>> neighbours,
        CliqueBudget budget,
        List<SimilarityGroup> groups)
    {
        if (component.Count > budget.MaxGroupSize)
        {
            var degraded = new SimilarityGroup(component, isCohesive: false);
            groups.Add(degraded);
            return;
        }

        var state = new Enumeration(neighbours, budget);
        var candidates = new HashSet<int>(component);
        var current = new List<int>();
        var excluded = new HashSet<int>();

        Expand(current, candidates, excluded, state);

        if (state.IsExhausted)
        {
            var degraded = new SimilarityGroup(component, isCohesive: false);
            groups.Add(degraded);
            return;
        }

        foreach (var clique in state.Cliques)
        {
            if (clique.Count < 2)
            {
                continue;
            }

            clique.Sort();
            var group = new SimilarityGroup(clique, isCohesive: true);
            groups.Add(group);
        }
    }

    private static Dictionary<int, HashSet<int>> BuildAdjacency(int blockCount, IReadOnlyList<SimilarPair> pairs)
    {
        var neighbours = new Dictionary<int, HashSet<int>>();

        foreach (var pair in pairs)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(pair.Left, blockCount);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(pair.Right, blockCount);

            Link(neighbours, pair.Left, pair.Right);
            Link(neighbours, pair.Right, pair.Left);
        }

        return neighbours;
    }

    private static int CompareGroups(SimilarityGroup left, SimilarityGroup right)
    {
        var byFirst = left.Members[0].CompareTo(right.Members[0]);
        if (byFirst != 0)
        {
            return byFirst;
        }

        var bySize = left.Members.Count.CompareTo(right.Members.Count);
        return bySize != 0
            ? bySize
            : string.CompareOrdinal(string.Join(',', left.Members), string.Join(',', right.Members));
    }

    /// <summary>
    ///     Connected components of the similarity graph, each ascending.
    /// </summary>
    /// <param name="blockCount">The number of blocks to consider.</param>
    /// <param name="neighbours">The adjacency map.</param>
    /// <returns>The components, each sorted ascending.</returns>
    private static List<List<int>> Components(int blockCount, Dictionary<int, HashSet<int>> neighbours)
    {
        var seen = new HashSet<int>();
        var components = new List<List<int>>();

        for (var start = 0; start < blockCount; start++)
        {
            if (!neighbours.ContainsKey(start) || !seen.Add(start))
            {
                continue;
            }

            var component = new List<int>
            {
                start
            };
            var queue = new Queue<int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                foreach (var neighbour in neighbours[queue.Dequeue()])
                {
                    if (seen.Add(neighbour))
                    {
                        component.Add(neighbour);
                        queue.Enqueue(neighbour);
                    }
                }
            }

            component.Sort();
            components.Add(component);
        }

        return components;
    }

    /// <summary>
    ///     Bron-Kerbosch enumeration of maximal cliques, abandoned once the budget is spent.
    /// </summary>
    /// <param name="current">The clique built so far.</param>
    /// <param name="candidates">Vertices that may still extend the clique.</param>
    /// <param name="excluded">Vertices already tried at this level.</param>
    /// <param name="state">The shared enumeration state.</param>
    private static void Expand(
        List<int> current,
        HashSet<int> candidates,
        HashSet<int> excluded,
        Enumeration state)
    {
        state.RecordStep();

        if (candidates.Count == 0 && excluded.Count == 0)
        {
            var clique = new List<int>(current);
            state.Cliques.Add(clique);
            return;
        }

        var ordered = new List<int>(candidates);
        ordered.Sort();

        foreach (var vertex in ordered)
        {
            if (state.IsExhausted)
            {
                return;
            }

            var adjacent = state.Neighbours[vertex];
            current.Add(vertex);
            Expand(current, Intersect(candidates, adjacent), Intersect(excluded, adjacent), state);
            current.RemoveAt(current.Count - 1);

            candidates.Remove(vertex);
            excluded.Add(vertex);
        }
    }

    private static HashSet<int> Intersect(HashSet<int> source, HashSet<int> adjacent)
    {
        var result = new HashSet<int>();
        foreach (var value in source)
        {
            if (adjacent.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static void Link(Dictionary<int, HashSet<int>> neighbours, int from, int to)
    {
        if (!neighbours.TryGetValue(from, out var set))
        {
            set = [];
            neighbours[from] = set;
        }

        set.Add(to);
    }

    /// <summary>
    ///     The mutable state one component's enumeration carries.
    /// </summary>
    private sealed class Enumeration
    {
        private readonly CliqueBudget _budget;
        private int _work;

        /// <summary>
        ///     Gets the maximal cliques found so far.
        /// </summary>
        public List<List<int>> Cliques { get; }

        /// <summary>
        ///     Gets a value indicating whether the budget has been spent.
        /// </summary>
        public bool IsExhausted
        {
            get
            {
                return _work > _budget.MaxWork;
            }
        }

        /// <summary>
        ///     Gets the adjacency map.
        /// </summary>
        public Dictionary<int, HashSet<int>> Neighbours { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Enumeration"/> class.
        /// </summary>
        /// <param name="neighbours">The adjacency map.</param>
        /// <param name="budget">The ceiling on enumeration work.</param>
        public Enumeration(Dictionary<int, HashSet<int>> neighbours, CliqueBudget budget)
        {
            var cliques = new List<List<int>>();
            Cliques = cliques;
            Neighbours = neighbours;
            _budget = budget;
        }

        /// <summary>
        ///     Counts one recursive expansion step against the budget.
        /// </summary>
        public void RecordStep()
        {
            _work++;
        }
    }
}
