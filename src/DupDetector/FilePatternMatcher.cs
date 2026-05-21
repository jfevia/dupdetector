using System.Text;
using System.Text.RegularExpressions;

namespace DupDetector;

/// <summary>
/// Case-insensitive glob pattern matcher for file paths.
/// Supports <c>*</c> (any characters within a single path segment) and
/// <c>**</c> (any characters across zero or more path segments).
/// Used by <c>--exclude-file-pattern</c> to suppress clusters whose instances
/// all reside in files matching the given pattern.
/// </summary>
internal static class FilePatternMatcher
{
    // Pre-compiled regex cache — patterns are repeated per cluster × instance; compile once.
    private static readonly Dictionary<string, Regex> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock CacheLock = new();

    /// <summary>
    /// Returns <c>true</c> when <paramref name="filePath"/> matches <paramref name="pattern"/>.
    /// Path separators are normalised to forward-slash before matching.
    /// </summary>
    public static bool IsMatch(string pattern, string filePath)
    {
        var regex = GetOrAdd(pattern);
        var normalized = filePath.Replace('\\', '/');
        return regex.IsMatch(normalized);
    }

    private static Regex GetOrAdd(string pattern)
    {
        lock (CacheLock)
        {
            if (Cache.TryGetValue(pattern, out var cached))
                return cached;

            var regexStr = GlobToRegex(pattern.Replace('\\', '/'));
            var regex = new Regex(regexStr, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            Cache[pattern] = regex;
            return regex;
        }
    }

    internal static string GlobToRegex(string glob)
    {
        var sb = new StringBuilder("^");
        int i = 0;
        while (i < glob.Length)
        {
            if (glob[i] == '*' && i + 1 < glob.Length && glob[i + 1] == '*')
            {
                // "**/" — matches zero or more path segments (including none).
                // "foo/**/bar" matches "foo/bar", "foo/x/bar", "foo/x/y/bar", etc.
                sb.Append("(?:.*/)?");
                i += 2;
                // Consume the optional trailing slash that follows "**"
                if (i < glob.Length && glob[i] == '/') i++;
            }
            else if (glob[i] == '*')
            {
                // "*" — matches any chars within a single segment (no /)
                sb.Append("[^/]*");
                i++;
            }
            else if (glob[i] == '?')
            {
                sb.Append("[^/]");
                i++;
            }
            else
            {
                sb.Append(Regex.Escape(glob[i].ToString()));
                i++;
            }
        }
        sb.Append('$');
        return sb.ToString();
    }
}
