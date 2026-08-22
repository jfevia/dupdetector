namespace DupDetector.Core.Matching;

/// <summary>
///     Creates <see cref="GlobSet"/> values.
/// </summary>
public static class GlobSets
{
    /// <summary>
    ///     Parses a sequence of glob patterns into a set.
    /// </summary>
    /// <param name="patterns">The patterns to parse.</param>
    /// <returns>The parsed set.</returns>
    public static GlobSet Parse(IEnumerable<string> patterns)
    {
        var parsed = new List<GlobPattern>();
        foreach (var pattern in patterns)
        {
            parsed.Add(GlobPatterns.Parse(pattern));
        }

        var set = new GlobSet([.. parsed]);
        return set;
    }
}
