using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DupDetector.Core.Normalization;

/// <summary>
/// The normalized text of a member and its structural hash.
/// </summary>
public readonly record struct NormalizedBlock(string Text, string Hash);

/// <summary>
/// Produces the structural form of a member and its hash in a single rewrite.
/// </summary>
public static class StructuralNormalizer
{
    /// <summary>
    /// Normalizes <paramref name="node"/>, returning its text and hash together. Callers never need
    /// a second pass, so a member is rewritten exactly once.
    /// </summary>
    public static NormalizedBlock Normalize(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var declared = DeclaredNameCollector.Collect(node);
        var rewritten = new NormalizingRewriter(declared).Visit(node);
        var text = rewritten.ToFullString().Trim();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

        return new NormalizedBlock(text, hash);
    }
}
