using DupDetector.Core.Normalization;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.Core.Tests.Normalization;

/// <summary>
///     Helpers for <see cref="StructuralNormalizerTests" />.
/// </summary>
public static class NormalizerFixtures
{

    /// <returns></returns>
    /// <param name="source"></param>
    /// <summary>
    ///     
    /// </summary>
    public static string Hash(string source)
    {
        return StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Hash;
    }

    /// <returns></returns>
    /// <param name="source"></param>
    /// <summary>
    ///     
    /// </summary>
    public static string Normalize(string source)
    {
        return StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Text;
    }
}
