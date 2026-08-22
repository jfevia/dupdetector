using DupDetector.Core.Detection;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     Helpers for <see cref="SimilarityTests" />.
/// </summary>
public static class TokenFixtures
{
    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="text"></param>
    /// <param name="interner"></param>
    public static TokenMultiset Set(string text, TokenInterner interner)
    {
        return TokenMultisets.Create(text, interner);
    }
}
