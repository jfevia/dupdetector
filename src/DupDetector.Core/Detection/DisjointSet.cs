namespace DupDetector.Core.Detection;

/// <summary>
/// Union-find with path halving and union by rank. Iterative, so no input size can exhaust the stack.
/// </summary>
public sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    public DisjointSet(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        _parent = new int[count];
        _rank = new int[count];
        for (var index = 0; index < count; index++)
        {
            _parent[index] = index;
        }
    }

    public int Count => _parent.Length;

    public int Find(int element)
    {
        while (_parent[element] != element)
        {
            _parent[element] = _parent[_parent[element]];
            element = _parent[element];
        }

        return element;
    }

    /// <summary>Merges two sets. Returns <c>false</c> when they were already joined.</summary>
    public bool Union(int left, int right)
    {
        var rootLeft = Find(left);
        var rootRight = Find(right);

        if (rootLeft == rootRight)
        {
            return false;
        }

        if (_rank[rootLeft] < _rank[rootRight])
        {
            (rootLeft, rootRight) = (rootRight, rootLeft);
        }

        _parent[rootRight] = rootLeft;
        if (_rank[rootLeft] == _rank[rootRight])
        {
            _rank[rootLeft]++;
        }

        return true;
    }

    /// <summary>Groups element indices by their representative, each group in ascending order.</summary>
    public IReadOnlyList<List<int>> Groups()
    {
        var groups = new Dictionary<int, List<int>>();
        for (var index = 0; index < _parent.Length; index++)
        {
            var root = Find(index);
            if (!groups.TryGetValue(root, out var members))
            {
                members = [];
                groups[root] = members;
            }

            members.Add(index);
        }

        return [.. groups.Values];
    }
}
