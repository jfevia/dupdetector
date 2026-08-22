namespace DupDetector.Core.Matching;

/// <summary>
///     A set of globs evaluated as a single OR. An empty set matches nothing.
/// </summary>
public sealed class GlobSet
{
    private readonly GlobPattern[] _patterns;

    /// <summary>
    ///     Gets a set with no patterns, which never matches.
    /// </summary>
    public static GlobSet Empty { get; }

    /// <summary>
    ///     Gets the number of patterns in the set.
    /// </summary>
    public int Count
    {
        get
        {
            return _patterns.Length;
        }
    }

    static GlobSet()
    {
        var empty = new GlobSet([]);
        Empty = empty;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="patterns"></param>
    public GlobSet(GlobPattern[] patterns)
    {
        _patterns = patterns;
    }

    /// <summary>
    ///     Tests a path against every pattern in the set.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> when any pattern matches.</returns>
    public bool IsMatch(string path)
    {
        var normalized = GlobPatterns.Normalize(path);
        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(normalized))
            {
                return true;
            }
        }

        return false;
    }
}
