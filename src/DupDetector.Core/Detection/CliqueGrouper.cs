namespace DupDetector.Core.Detection;

/// <summary>
/// Limits on clique enumeration, which is exponential in the worst case.
/// </summary>
/// <param name="MaxGroupSize">
/// Largest connected component that will be enumerated exactly. Larger components fall back.
/// </param>
/// <param name="MaxWork">
/// Ceiling on recursive expansion steps within one component before it falls back.
/// </param>
public readonly record struct CliqueBudget(int MaxGroupSize, int MaxWork)
{
    public static CliqueBudget Default { get; } = new(64, 20_000);
}

/// <summary>
/// A set of mutually similar block indices.
/// </summary>
/// <param name="Members">Block indices, ascending.</param>
/// <param name="IsCohesive">
/// <c>false</c> when the budget was exhausted and this group was produced by connectivity alone,
/// meaning some members may not be similar to one another.
/// </param>
public sealed record SimilarityGroup(IReadOnlyList<int> Members, bool IsCohesive);

/// <summary>
/// Turns similar pairs into groups of mutually similar blocks.
/// </summary>
// Similarity is not transitive, so a connectivity grouping would merge blocks that share nothing.
// Clique enumeration is exponential, so a component exceeding the budget degrades to connectivity.
public static class CliqueGrouper
{
    /// <summary>
    /// Returns every maximal group of at least two mutually similar blocks.
    /// </summary>
    public static IReadOnlyList<SimilarityGroup> Group(
        int blockCount,
        IReadOnlyList<SimilarPair> pairs,
        CliqueBudget budget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(blockCount);
        ArgumentNullException.ThrowIfNull(pairs);

        if (pairs.Count == 0)
        {
            return [];
        }

        var neighbours = BuildAdjacency(blockCount, pairs);
        var groups = new List<SimilarityGroup>();

        foreach (var component in Components(blockCount, neighbours))
        {
            if (component.Count > budget.MaxGroupSize)
            {
                groups.Add(new SimilarityGroup(component, IsCohesive: false));
                continue;
            }

            var work = 0;
            var cliques = new List<List<int>>();
            var candidates = new HashSet<int>(component);

            Expand([], candidates, [], neighbours, cliques, budget, ref work);

            if (work > budget.MaxWork)
            {
                groups.Add(new SimilarityGroup(component, IsCohesive: false));
                continue;
            }

            foreach (var clique in cliques.Where(clique => clique.Count >= 2))
            {
                clique.Sort();
                groups.Add(new SimilarityGroup(clique, IsCohesive: true));
            }
        }

        // Deterministic order regardless of traversal.
        return [.. groups
            .OrderBy(group => group.Members[0])
            .ThenBy(group => group.Members.Count)
            .ThenBy(group => string.Join(',', group.Members), StringComparer.Ordinal)];
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

    private static void Link(Dictionary<int, HashSet<int>> neighbours, int from, int to)
    {
        if (!neighbours.TryGetValue(from, out var set))
        {
            set = [];
            neighbours[from] = set;
        }

        set.Add(to);
    }

    /// <summary>Connected components of the similarity graph, each ascending.</summary>
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

            var component = new List<int> { start };
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
    /// Bron-Kerbosch enumeration of maximal cliques, abandoned once the budget is spent. The budget
    /// is checked at the recursion site rather than on entry, so there is only one guard.
    /// </summary>
    private static void Expand(
        List<int> current,
        HashSet<int> candidates,
        HashSet<int> excluded,
        Dictionary<int, HashSet<int>> neighbours,
        List<List<int>> cliques,
        CliqueBudget budget,
        ref int work)
    {
        work++;

        if (candidates.Count == 0 && excluded.Count == 0)
        {
            cliques.Add([.. current]);
            return;
        }

        foreach (var vertex in candidates.Order().ToArray())
        {
            if (work > budget.MaxWork)
            {
                return;
            }

            var adjacent = neighbours[vertex];
            current.Add(vertex);
            Expand(
                current,
                [.. candidates.Where(adjacent.Contains)],
                [.. excluded.Where(adjacent.Contains)],
                neighbours,
                cliques,
                budget,
                ref work);
            current.RemoveAt(current.Count - 1);

            candidates.Remove(vertex);
            excluded.Add(vertex);
        }
    }
}
