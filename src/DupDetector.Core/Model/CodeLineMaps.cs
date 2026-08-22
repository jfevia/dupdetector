using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.Core.Model;

/// <summary>
///     Builds <see cref="CodeLineMap"/> values from parsed source.
/// </summary>
public static class CodeLineMaps
{
    /// <summary>
    ///     Builds the map from a parsed file.
    /// </summary>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="lineCount">The physical line count of the file.</param>
    /// <returns>The map of code-carrying lines.</returns>
    public static CodeLineMap Create(SyntaxTree tree, int lineCount)
    {
        if (lineCount <= 0)
        {
            return CodeLineMap.Empty;
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

        var map = new CodeLineMap(isCode);
        return map;
    }
}
