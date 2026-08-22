using DupDetector.Core.Model;
using System.Text.RegularExpressions;

namespace DupDetector.Core.Matching;

/// <summary>
///     Decides whether a file belongs to a test project.
/// </summary>
public static partial class TestFileClassifier
{
    /// <summary>
    ///     Gets the vocabulary that marks a name as test code.
    /// </summary>
    private static string[] TestWords
    {
        get
        {
            return ["test", "tests", "spec", "specs"];
        }
    }

    /// <summary>
    ///     Splits an identifier into words on case transitions and separators, then compares the final
    ///     word against the test vocabulary.
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    public static bool CanLastWordIsTestWord(string name)
    {
        var words = WordBoundary().Split(name);
        for (var index = words.Length - 1; index >= 0; index--)
        {
            if (words[index].Length == 0)
            {
                continue;
            }

            return TestWords.Contains(words[index], StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="relativePath"></param>
    /// <param name="project"></param>
    public static bool IsTestFile(string relativePath, ProjectIdentity project)
    {

        if (project.Name is not null && CanLastWordIsTestWord(project.Name))
        {
            return true;
        }

        var segments = GlobPatterns.Normalize(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (CanLastWordIsTestWord(StripExtension(segment)))
            {
                return true;
            }
        }

        return false;
    }

    private static string StripExtension(string segment)
    {
        var dot = segment.LastIndexOf('.');
        return dot > 0 && segment.AsSpan(dot).Equals(".cs", StringComparison.OrdinalIgnoreCase)
            ? segment[..dot]
            : segment;
    }

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|[_\-.\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordBoundary();
}
