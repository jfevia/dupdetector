using System.Text;
using System.Text.RegularExpressions;
using DupDetector.Core.Internal;

namespace DupDetector.Core.Matching;

/// <summary>
/// The single glob engine. Path exclusion and cluster exclusion share it, so a pattern means the
/// same thing everywhere.
/// </summary>
// gitignore semantics: * stays within a segment, ** spans segments, a directory matches its contents.
public sealed class GlobPattern
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly Regex _regex;

    private GlobPattern(string pattern, Regex regex)
    {
        Pattern = pattern;
        _regex = regex;
    }

    /// <summary>The original pattern text.</summary>
    public string Pattern { get; }

    public static GlobPattern Parse(string pattern)
    {
        Require.NotBlank(pattern, nameof(pattern));

        var regex = new Regex(
            Translate(pattern),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            MatchTimeout);

        return new GlobPattern(pattern, regex);
    }

    /// <summary>Replaces backslashes with forward slashes and drops any trailing separator.</summary>
    public static string Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Replace('\\', '/').TrimEnd('/');
    }

    public bool IsMatch(string path) => _regex.IsMatch(Normalize(path));

    public override string ToString() => Pattern;

    internal static string Translate(string pattern)
    {
        var glob = Normalize(pattern.Trim()).TrimStart('/');

        // Every pattern matches at any depth; there is no anchored form.
        var builder = new StringBuilder("^(?:.*/)?");
        var endsOpen = false;
        var index = 0;

        while (index < glob.Length)
        {
            switch (glob[index])
            {
                case '*' when index + 1 < glob.Length && glob[index + 1] == '*':
                    index = AppendGlobStar(builder, glob, index, ref endsOpen);
                    break;

                case '*':
                    builder.Append("[^/]*");
                    index++;
                    break;

                case '?':
                    builder.Append("[^/]");
                    index++;
                    break;

                default:
                    builder.Append(Regex.Escape(glob[index].ToString()));
                    index++;
                    break;
            }
        }

        if (!endsOpen)
        {
            // Naming a directory also matches everything beneath it.
            builder.Append("(?:/.*)?");
        }

        return builder.Append('$').ToString();
    }

    private static int AppendGlobStar(StringBuilder builder, string glob, int index, ref bool endsOpen)
    {
        while (index < glob.Length && glob[index] == '*')
        {
            index++;
        }

        if (index == glob.Length)
        {
            // A trailing "**" consumes the rest of the path. The separator that preceded it has
            // already been emitted, so this must not add another one.
            builder.Append(".*");
            endsOpen = true;
            return index;
        }

        if (glob[index] == '/')
        {
            builder.Append("(?:.*/)?");
            return index + 1;
        }

        builder.Append(".*");
        return index;
    }
}
