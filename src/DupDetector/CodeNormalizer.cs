using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector;

/// <summary>
/// Normalizes C# syntax nodes for structural comparison by stripping identifiers
/// and literals, enabling detection of code that differs only in naming or values.
/// </summary>
public class CodeNormalizer
{
    public string Normalize(SyntaxNode node)
    {
        var rewriter = new NormalizingRewriter();
        var normalized = rewriter.Visit(node);
        return normalized?.ToFullString() ?? string.Empty;
    }

    public string GetStructuralHash(SyntaxNode node)
    {
        var normalized = Normalize(node);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private class NormalizingRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, string> _identifierMap = new();
        private int _nextId;

        // Reserved placeholder tokens that should not be remapped through the identifier dict
        private static readonly HashSet<string> _placeholders = new()
        {
            "STR_LIT", "NUM_LIT", "CHAR_LIT", "BOOL_LIT", "LIT", "VAR_TYPE"
        };

        public NormalizingRewriter() : base(visitIntoStructuredTrivia: false) { }

        // Normalize ALL identifier tokens here so declarations and references are both covered.
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken))
            {
                var name = token.ValueText;
                // Don't remap synthesized placeholder tokens
                if (!_placeholders.Contains(name))
                {
                    if (!_identifierMap.TryGetValue(name, out var normalized))
                    {
                        normalized = $"var{_nextId++}";
                        _identifierMap[name] = normalized;
                    }
                    return SyntaxFactory.Identifier(normalized)
                        .WithLeadingTrivia(SyntaxTriviaList.Empty)
                        .WithTrailingTrivia(SyntaxFactory.Space);
                }
            }

            return token
                .WithLeadingTrivia(SyntaxTriviaList.Empty)
                .WithTrailingTrivia(SyntaxFactory.Space);
        }

        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            string placeholder = node.Kind() switch
            {
                SyntaxKind.StringLiteralExpression => "STR_LIT",
                SyntaxKind.NumericLiteralExpression => "NUM_LIT",
                SyntaxKind.CharacterLiteralExpression => "CHAR_LIT",
                SyntaxKind.TrueLiteralExpression => "BOOL_LIT",
                SyntaxKind.FalseLiteralExpression => "BOOL_LIT",
                _ => "LIT"
            };

            // Return a synthetic identifier node; VisitToken is NOT called on it since
            // it is a freshly created node returned from a Visit method (not revisited).
            var token = SyntaxFactory.Identifier(placeholder)
                .WithLeadingTrivia(SyntaxTriviaList.Empty)
                .WithTrailingTrivia(SyntaxFactory.Space);
            return SyntaxFactory.IdentifierName(token);
        }

        public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            // Visit children first (normalizes variable names and initializers)
            var visitedNode = (VariableDeclarationSyntax?)base.VisitVariableDeclaration(node);
            if (visitedNode == null) return null;

            // Replace the type with a uniform placeholder so "int x" and "string x" normalize identically
            var varType = SyntaxFactory.IdentifierName(
                SyntaxFactory.Identifier("VAR_TYPE")
                    .WithLeadingTrivia(SyntaxTriviaList.Empty)
                    .WithTrailingTrivia(SyntaxFactory.Space));
            return visitedNode.WithType(varType);
        }
    }
}
