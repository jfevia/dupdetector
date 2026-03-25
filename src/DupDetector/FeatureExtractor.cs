using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DupDetector;

public record CodeBlock(
    string FilePath,
    int StartLine,
    int EndLine,
    string MethodName,
    string NormalizedHash,
    string NormalizedText,
    string RawText,
    int LineCount
);

/// <summary>
/// Extracts method-level and sub-method code blocks from C# syntax trees.
/// </summary>
public class FeatureExtractor
{
    private readonly CodeNormalizer _normalizer = new();

    public List<CodeBlock> Extract(string filePath, SyntaxTree syntaxTree, string sourceText, int minLines)
    {
        var root = syntaxTree.GetRoot();
        var blocks = new List<CodeBlock>();

        // Extract top-level method/constructor/local function declarations
        var methodNodes = root.DescendantNodes().Where(n =>
            n is MethodDeclarationSyntax or
               ConstructorDeclarationSyntax or
               LocalFunctionStatementSyntax);

        foreach (var node in methodNodes)
        {
            var span = node.GetLocation().GetLineSpan();
            var startLine = span.StartLinePosition.Line + 1;
            var endLine = span.EndLinePosition.Line + 1;
            var lineCount = endLine - startLine + 1;

            if (lineCount < minLines) continue;

            var methodName = node switch
            {
                MethodDeclarationSyntax m => m.Identifier.ValueText,
                ConstructorDeclarationSyntax c => c.Identifier.ValueText,
                LocalFunctionStatementSyntax lf => lf.Identifier.ValueText,
                _ => "unknown"
            };

            var rawText = ExtractText(sourceText, node.Span);
            var hash = _normalizer.GetStructuralHash(node);
            var normalizedText = _normalizer.Normalize(node);

            blocks.Add(new CodeBlock(filePath, startLine, endLine, methodName, hash, normalizedText, rawText, lineCount));

            // Sliding window over statements inside the body
            var body = GetBody(node);
            if (body != null)
            {
                var stmtBlocks = ExtractSlidingWindowBlocks(filePath, body, sourceText, minLines);
                blocks.AddRange(stmtBlocks);
            }
        }

        return blocks;
    }

    private List<CodeBlock> ExtractSlidingWindowBlocks(string filePath, BlockSyntax body, string sourceText, int minLines)
    {
        var statements = body.Statements;
        if (statements.Count < minLines) return new List<CodeBlock>();

        var results = new List<CodeBlock>();

        for (int start = 0; start <= statements.Count - minLines; start++)
        {
            var windowStmts = statements.Skip(start).Take(minLines).ToList();
            var first = windowStmts.First();
            var last = windowStmts.Last();

            var span = first.GetLocation().GetLineSpan();
            var startLine = span.StartLinePosition.Line + 1;
            var endSpan = last.GetLocation().GetLineSpan();
            var endLine = endSpan.EndLinePosition.Line + 1;
            var lineCount = endLine - startLine + 1;

            if (lineCount < minLines) continue;

            // Build a synthetic block node from the window statements
            var syntheticBlock = SyntaxFactory.Block(new SyntaxList<StatementSyntax>(windowStmts));
            var rawText = string.Join("\n", windowStmts.Select(s => ExtractText(sourceText, s.Span)));
            var hash = _normalizer.GetStructuralHash(syntheticBlock);
            var normalizedText = _normalizer.Normalize(syntheticBlock);

            results.Add(new CodeBlock(filePath, startLine, endLine, $"<window@{startLine}>", hash, normalizedText, rawText, lineCount));
        }

        return results;
    }

    private static BlockSyntax? GetBody(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax m => m.Body,
        ConstructorDeclarationSyntax c => c.Body,
        LocalFunctionStatementSyntax lf => lf.Body,
        _ => null
    };

    private static string ExtractText(string sourceText, TextSpan span)
    {
        if (span.Start >= sourceText.Length) return string.Empty;
        var end = Math.Min(span.End, sourceText.Length);
        return sourceText[span.Start..end];
    }
}
