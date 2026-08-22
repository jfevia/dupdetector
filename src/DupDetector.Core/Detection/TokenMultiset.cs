namespace DupDetector.Core.Detection;

/// <summary>
/// Maps token text to dense integer ids so similarity can work on sorted integer arrays.
/// </summary>
public sealed class TokenInterner
{
    private readonly Dictionary<string, int> _ids = new(StringComparer.Ordinal);

    public int Count => _ids.Count;

    public int Intern(string token)
    {
        if (!_ids.TryGetValue(token, out var id))
        {
            id = _ids.Count;
            _ids[token] = id;
        }

        return id;
    }
}

/// <summary>
/// A block's tokens as a multiset: distinct ids in ascending order with their repeat counts.
/// </summary>
/// <remarks>
/// Keeping counts rather than collapsing to a set means a short member and a long one built from
/// the same few identifiers no longer score as identical.
/// </remarks>
public sealed class TokenMultiset
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', '{', '}', '(', ')', ';', ',', '.', '[', ']'];

    private TokenMultiset(int[] ids, int[] counts, int cardinality)
    {
        Ids = ids;
        Counts = counts;
        Cardinality = cardinality;
    }

    /// <summary>Distinct token ids, ascending.</summary>
    public int[] Ids { get; }

    /// <summary>Repeat count for each entry of <see cref="Ids"/>.</summary>
    public int[] Counts { get; }

    /// <summary>Total token count, including repeats.</summary>
    public int Cardinality { get; }

    public static TokenMultiset Create(string normalizedText, TokenInterner interner)
    {
        ArgumentNullException.ThrowIfNull(normalizedText);
        ArgumentNullException.ThrowIfNull(interner);

        var counts = new Dictionary<int, int>();
        var cardinality = 0;

        foreach (var token in normalizedText.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var id = interner.Intern(token);
            counts[id] = counts.TryGetValue(id, out var existing) ? existing + 1 : 1;
            cardinality++;
        }

        var ids = new int[counts.Count];
        counts.Keys.CopyTo(ids, 0);
        Array.Sort(ids);

        var ordered = new int[ids.Length];
        for (var index = 0; index < ids.Length; index++)
        {
            ordered[index] = counts[ids[index]];
        }

        return new TokenMultiset(ids, ordered, cardinality);
    }
}
