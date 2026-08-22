using Microsoft.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace DupDetector.Core.Normalization;

/// <summary>
///     Produces the structural form of a member and its hash in a single rewrite.
/// </summary>
public static class StructuralNormalizer
{
    /// <summary>
    ///     Normalizes a node, returning its text and hash together so it is rewritten exactly once.
    /// </summary>
    /// <param name="node">The syntax node to normalize.</param>
    /// <returns>The structural form and its hash.</returns>
    public static NormalizedBlock Normalize(SyntaxNode node)
    {
        var declared = DeclaredNames.Collect(node);
        var rewriter = new NormalizingRewriter(declared);
        var rewritten = rewriter.Visit(node);
        var text = rewritten.ToFullString().Trim();
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var hash = Convert.ToHexString(digest).ToLowerInvariant();
        var block = new NormalizedBlock(text, hash);

        return block;
    }
}
