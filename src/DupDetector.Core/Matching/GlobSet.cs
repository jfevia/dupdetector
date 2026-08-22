namespace DupDetector.Core.Matching;

/// <summary>
/// A set of globs evaluated as a single OR. An empty set matches nothing.
/// </summary>
public sealed class GlobSet
{
    private readonly GlobPattern[] _patterns;

    private GlobSet(GlobPattern[] patterns) => _patterns = patterns;

    /// <summary>A set with no patterns, which never matches.</summary>
    public static GlobSet Empty { get; } = new([]);

    public int Count => _patterns.Length;

    public static GlobSet Parse(IEnumerable<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        return new GlobSet([.. patterns.Select(GlobPattern.Parse)]);
    }

    public bool IsMatch(string path)
    {
        var normalized = GlobPattern.Normalize(path);
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
