using DupDetector.Core.Model;
using DupDetector.TestKit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Helpers for <see cref="TypeExtractionTests" />.
/// </summary>
public static class TypeFixtures
{
    /// <returns></returns>
    /// <param name="source"></param>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="kinds"></param>
    /// <param name="minTypeLines"></param>
    public static IReadOnlyList<CodeBlock> Extract(string source, DetectionKind kinds, int minTypeLines)
    {
        var detectionSettings = new DetectionSettings
        {
            MinLines = 1,
            MinTypeLines = minTypeLines,
            Kinds = kinds
        };
        return Code.Blocks(source, detectionSettings);
    }

    /// <summary>
    ///     Extracts blocks using the default minimum type size.
    /// </summary>
    /// <returns></returns>
    /// <param name="source"></param>
    /// <param name="kinds"></param>
    public static IReadOnlyList<CodeBlock> Extract(string source, DetectionKind kinds)
    {
        return Extract(source, kinds, 3);
    }
}
