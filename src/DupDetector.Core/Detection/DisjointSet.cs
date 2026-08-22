namespace DupDetector.Core.Detection;

/// <summary>
///     Union-find with path halving and union by rank. Iterative, so no input size can exhaust the stack.
/// </summary>
public sealed class DisjointSet
{
    private readonly int[] _parent;
    private readonly int[] _rank;

    /// <summary>
    ///     
    /// </summary>
    public int Count
    {
        get
        {
            return _parent.Length;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="count"></param>
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

    /// <summary>
    ///     Merges two sets. Returns <c>false</c> when they were already joined.
    /// </summary>
    /// <returns></returns>
    /// <param name="left"></param>
    /// <param name="right"></param>
    public bool CanUnion(int left, int right)
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

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="element"></param>
    public int Find(int element)
    {
        while (_parent[element] != element)
        {
            _parent[element] = _parent[_parent[element]];
            element = _parent[element];
        }

        return element;
    }

    /// <summary>
    ///     Groups element indices by their representative, each group in ascending order.
    /// </summary>
    /// <returns></returns>
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
