using System.Text.RegularExpressions;

namespace DupDetector.Core.Matching;

/// <summary>
///     The single glob engine, shared by path exclusion and cluster exclusion.
/// </summary>
public sealed class GlobPattern
{
    private readonly Regex _regex;

    /// <summary>
    ///     Gets the original pattern text.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="regex"></param>
    public GlobPattern(string pattern, Regex regex)
    {
        Pattern = pattern;
        _regex = regex;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Pattern;
    }

    /// <summary>
    ///     Tests a path against this pattern.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> when the pattern matches.</returns>
    public bool IsMatch(string path)
    {
        return _regex.IsMatch(GlobPatterns.Normalize(path));
    }
}
