using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Normalization;

/// <summary>
///     Rewrites a member into its structural form.
/// </summary>
public sealed class NormalizingRewriter : CSharpSyntaxRewriter
{
    private readonly HashSet<string> _declared;
    private readonly Dictionary<string, string> _renames;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NormalizingRewriter"/> class.
    /// </summary>
    /// <param name="declared">The identifiers the block declares.</param>
    public NormalizingRewriter(HashSet<string> declared)
        : base(visitIntoStructuredTrivia: false)
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        _declared = declared;
        _renames = renames;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken) && _declared.Contains(token.ValueText) && !MemberNames.IsMemberName(token))
        {
            return SyntaxFactory.Identifier(Rename(token.ValueText)).WithTrailingTrivia(SyntaxFactory.Space);
        }

        return token.WithLeadingTrivia(SyntaxTriviaList.Empty).WithTrailingTrivia(SyntaxFactory.Space);
    }

    /// <summary>
    ///     True when the token is the member half of <c>a.B</c>, <c>a?.B</c> or <c>A.B</c>, which must
    ///     keep its original name even if a local happens to share it.
    /// </summary>
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
