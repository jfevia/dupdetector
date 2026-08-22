using System.Text.RegularExpressions;
using DupDetector.Core.Model;

namespace DupDetector.Core.Matching;

/// <summary>
/// Decides whether a file belongs to a test project.
/// </summary>
// Whole-word matching on the path relative to the scan root, so Latest.cs and C:\test\ are not tests.
public static partial class TestFileClassifier
{
    private static readonly string[] TestWords = ["test", "tests", "spec", "specs"];

    public static bool IsTestFile(string relativePath, ProjectIdentity project)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        ArgumentNullException.ThrowIfNull(project);

        if (project.Name is not null && LastWordIsTestWord(project.Name))
        {
            return true;
        }

        var segments = GlobPattern.Normalize(relativePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (LastWordIsTestWord(StripExtension(segment)))
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

    /// <summary>
    /// Splits an identifier into words on case transitions and separators, then compares the final
    /// word against the test vocabulary.
    /// </summary>
    internal static bool LastWordIsTestWord(string name)
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

    [GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|[_\-.\s]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordBoundary();
}
