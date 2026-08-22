using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.Core.Model;

/// <summary>
/// Which physical lines of a file carry code rather than blanks or comments.
/// </summary>
/// <remarks>
/// Derived from the parsed tokens rather than from text heuristics, so string literals containing
/// <c>//</c> and multi-line verbatim strings are classified correctly. Lets duplication be expressed
/// against analysable lines, which is what comparable tools report.
/// </remarks>
public sealed class CodeLineMap
{
    private readonly bool[] _isCode;

    private CodeLineMap(bool[] isCode)
    {
        _isCode = isCode;
        Total = isCode.Count(value => value);
    }

    /// <summary>A file with no analysable lines.</summary>
    public static CodeLineMap Empty { get; } = new([]);

    /// <summary>Number of lines carrying code.</summary>
    public int Total { get; }

    /// <summary>Builds the map from a parsed file.</summary>
    public static CodeLineMap Create(SyntaxTree tree, int lineCount)
    {
        ArgumentNullException.ThrowIfNull(tree);

        if (lineCount <= 0)
        {
            return Empty;
        }

        var isCode = new bool[lineCount];
        var text = tree.GetText();

        foreach (var token in tree.GetRoot().DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.EndOfFileToken) || token.Span.IsEmpty)
            {
                continue;
            }

            var first = text.Lines.GetLineFromPosition(token.Span.Start).LineNumber;
            var last = text.Lines.GetLineFromPosition(token.Span.End).LineNumber;

            for (var line = first; line <= last && line < isCode.Length; line++)
            {
                isCode[line] = true;
            }
        }

        return new CodeLineMap(isCode);
    }

    /// <summary>Counts code lines inside a one-based, inclusive range.</summary>
    public int CountIn(LineRange range)
    {
        var count = 0;
        var last = Math.Min(range.End, _isCode.Length);

        for (var line = range.Start; line <= last; line++)
        {
            if (_isCode[line - 1])
            {
                count++;
            }
        }

        return count;
    }
}
