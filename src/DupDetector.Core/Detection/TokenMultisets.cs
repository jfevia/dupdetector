namespace DupDetector.Core.Detection;

/// <summary>
///     Creates <see cref="TokenMultiset"/> values.
/// </summary>
public static class TokenMultisets
{
    /// <summary>
    ///     Gets the characters that separate tokens in normalized text.
    /// </summary>
    private static char[] Separators
    {
        get
        {
            return [' ', '\t', '\n', '\r', '{', '}', '(', ')', ';', ',', '.', '[', ']'];
        }
    }

    /// <summary>
    ///     Builds a multiset from normalized text.
    /// </summary>
    /// <param name="normalizedText">The structural form of a block.</param>
    /// <param name="interner">The interner assigning token ids.</param>
    /// <returns>The multiset of tokens.</returns>
    public static TokenMultiset Create(string normalizedText, TokenInterner interner)
    {
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

        var multiset = new TokenMultiset(ids, ordered, cardinality);
        return multiset;
    }
}
