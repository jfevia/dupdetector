using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Normalization;

/// <summary>
/// Rewrites a member into its structural form.
/// </summary>
/// <remarks>
/// Declared identifiers become <c>var0</c>, <c>var1</c>, ... in order of first appearance, and
/// literals become kind placeholders. Type names and member-access names are left untouched, so a
/// mapper over one domain type no longer collapses onto a mapper over another.
/// </remarks>
internal sealed class NormalizingRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<string> _declared;
    private readonly Dictionary<string, string> _renames = new(StringComparer.Ordinal);

    internal NormalizingRewriter(HashSet<string> declared)
        : base(visitIntoStructuredTrivia: false)
        => _declared = declared;

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken) && _declared.Contains(token.ValueText) && !IsMemberName(token))
        {
            return SyntaxFactory.Identifier(Rename(token.ValueText)).WithTrailingTrivia(SyntaxFactory.Space);
        }

        return token.WithLeadingTrivia(SyntaxTriviaList.Empty).WithTrailingTrivia(SyntaxFactory.Space);
    }

    public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
    {
        var placeholder = node.Kind() switch
        {
            SyntaxKind.StringLiteralExpression or SyntaxKind.Utf8StringLiteralExpression => "STR",
            SyntaxKind.NumericLiteralExpression => "NUM",
            SyntaxKind.CharacterLiteralExpression => "CHR",
            SyntaxKind.TrueLiteralExpression or SyntaxKind.FalseLiteralExpression => "BOOL",
            SyntaxKind.NullLiteralExpression => "NULL",
            _ => "LIT",
        };

        return SyntaxFactory.IdentifierName(
            SyntaxFactory.Identifier(placeholder).WithTrailingTrivia(SyntaxFactory.Space));
    }

    /// <summary>
    /// True when the token is the member half of <c>a.B</c>, <c>a?.B</c> or <c>A.B</c>, which must
    /// keep its original name even if a local happens to share it.
    /// </summary>
    private static bool IsMemberName(SyntaxToken token)
    {
        if (token.Parent is not IdentifierNameSyntax name)
        {
            return false;
        }

        return name.Parent switch
        {
            MemberAccessExpressionSyntax access => access.Name == name,
            MemberBindingExpressionSyntax binding => binding.Name == name,
            QualifiedNameSyntax qualified => qualified.Right == name,
            _ => false,
        };
    }

    private string Rename(string original)
    {
        if (!_renames.TryGetValue(original, out var renamed))
        {
            renamed = $"var{_renames.Count}";
            _renames[original] = renamed;
        }

        return renamed;
    }
}
