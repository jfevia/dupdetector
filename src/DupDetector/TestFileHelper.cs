namespace DupDetector;

/// <summary>
/// Heuristics for detecting whether a source file belongs to a test project.
/// Used to annotate <see cref="FileScore.IsTestFile"/> and optionally exclude
/// test files from score output via <c>--exclude-test-files</c>.
/// </summary>
public static class TestFileHelper
{
    private static readonly string[] TestSegments =
    [
        "tests", "test", "specs", "spec"
    ];

    private static readonly string[] TestSuffixes =
    [
        "tests.cs", "test.cs", "specs.cs", "spec.cs"
    ];

    /// <summary>
    /// Returns <c>true</c> when <paramref name="filePath"/> matches common test-project
    /// path heuristics: the path contains a directory segment named Tests/Test/Specs/Spec,
    /// or the filename ends with Tests.cs / Test.cs / Specs.cs / Spec.cs.
    /// </summary>
    public static bool IsTestFile(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');

        // Check file name suffix (case-insensitive)
        var fileName = Path.GetFileName(normalized).ToLowerInvariant();
        foreach (var suffix in TestSuffixes)
        {
            if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Check directory path segments (case-insensitive)
        var segments = normalized.Split('/');
        foreach (var segment in segments)
        {
            var lower = segment.ToLowerInvariant();
            // Exact match: e.g., directory literally named "tests" or "test"
            foreach (var testSeg in TestSegments)
            {
                if (lower == testSeg)
                    return true;
            }
            // Suffix match: e.g., "MyProject.Tests" or "Client.Core.Test"
            if (lower.EndsWith(".tests", StringComparison.Ordinal) ||
                lower.EndsWith(".test", StringComparison.Ordinal) ||
                lower.EndsWith(".specs", StringComparison.Ordinal) ||
                lower.EndsWith(".spec", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
