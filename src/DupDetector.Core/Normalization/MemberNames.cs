using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Normalization;

/// <summary>
///     Recognises identifiers that name a member rather than a declaration.
/// </summary>
public static class MemberNames
{
    /// <summary>
    ///     Reports whether a token names a member access rather than a declared identifier.
    /// </summary>
    /// <param name="token">The identifier token to test.</param>
    /// <returns><c>true</c> when the token is a member name.</returns>
    public static bool IsMemberName(SyntaxToken token)
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
}
