using DupDetector.Core.Model;

namespace DupDetector.Core.Tests.Normalization;

/// <summary>
///     Helpers for <see cref="MemberBlockExtractorTests" />.
/// </summary>
public static class ExtractorFixtures
{
    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="kinds"></param>
    /// <param name="minLines"></param>
    public static DetectionSettings Settings(DetectionKind kinds, int minLines)
    {
        var value = new DetectionSettings()
        {
            Kinds = kinds,
            MinLines = minLines
        };
        return value;
    }

    /// <summary>
    ///     Settings covering every kind with no minimum size.
    /// </summary>
    /// <returns></returns>
    public static DetectionSettings Settings()
    {
        return Settings(DetectionKind.All, 1);
    }

    /// <summary>
    ///     Settings covering one kind with no minimum size.
    /// </summary>
    /// <returns></returns>
    /// <param name="kinds"></param>
    public static DetectionSettings Settings(DetectionKind kinds)
    {
        return Settings(kinds, 1);
    }
}
