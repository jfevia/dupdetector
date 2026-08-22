using DupDetector.Core.Internal;
using System.Text;
using System.Text.RegularExpressions;

namespace DupDetector.Core.Matching;

/// <summary>
///     Parses and translates glob patterns.
/// </summary>
public static class GlobPatterns
{
    /// <summary>
    ///     Gets the ceiling on a single match, so a pathological pattern cannot hang a scan.
    /// </summary>
    private static TimeSpan MatchTimeout
    {
        get
        {
            return TimeSpan.FromSeconds(1);
        }
    }

    /// <summary>
    ///     Replaces backslashes with forward slashes and drops any trailing separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>
    ///     Parses one glob pattern.
    /// </summary>
    /// <param name="pattern">The pattern text, which must not be blank.</param>
    /// <returns>The compiled pattern.</returns>
    public static GlobPattern Parse(string pattern)
    {
        Require.NotBlank(pattern, nameof(pattern));

        var regex = new Regex(
            Translate(pattern),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
            MatchTimeout);

        var parsed = new GlobPattern(pattern, regex);
        return parsed;
    }

    /// <summary>
    ///     Translates a glob into the equivalent regular expression.
    /// </summary>
    /// <param name="pattern">The pattern text.</param>
    /// <returns>The regular expression source.</returns>
    public static string Translate(string pattern)
    {
        var glob = Normalize(pattern.Trim()).TrimStart('/');
        var state = new Translation(glob);

        while (state.Index < glob.Length)
        {
            state.Step();
        }

        return state.Finish();
    }

    /// <summary>
    ///     Carries the cursor and output while one glob is translated.
    /// </summary>
    private sealed class Translation
    {
        private readonly StringBuilder _builder;
        private readonly string _glob;
        private bool _isEndsOpen;

        /// <summary>
        ///     Gets the position of the next character to translate.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Translation"/> class.
        /// </summary>
        /// <param name="glob">The normalized glob to translate.</param>
        public Translation(string glob)
        {
            var builder = new StringBuilder("^(?:.*/)?");
            _builder = builder;
            _glob = glob;
        }

        /// <summary>
        ///     Closes the expression and returns it.
        /// </summary>
        /// <returns>The regular expression source.</returns>
        public string Finish()
        {
            if (!_isEndsOpen)
            {
                _builder.Append("(?:/.*)?");
            }

            return _builder.Append('$').ToString();
        }

        /// <summary>
        ///     Translates the character at the cursor and advances.
        /// </summary>
        public void Step()
        {
            switch (_glob[Index])
            {
                case '*' when Index + 1 < _glob.Length && _glob[Index + 1] == '*':
                    AppendGlobStar();
                    break;

                case '*':
                    _builder.Append("[^/]*");
                    Index++;
                    break;

                case '?':
                    _builder.Append("[^/]");
                    Index++;
                    break;

                default:
                    _builder.Append(Regex.Escape(_glob[Index].ToString()));
                    Index++;
                    break;
            }
        }

        private void AppendGlobStar()
        {
            while (Index < _glob.Length && _glob[Index] == '*')
            {
                Index++;
            }

            if (Index == _glob.Length)
            {
                _builder.Append(".*");
                _isEndsOpen = true;
                return;
            }

            if (_glob[Index] == '/')
            {
                _builder.Append("(?:.*/)?");
                Index++;
                return;
            }

            _builder.Append(".*");
        }
    }
}
