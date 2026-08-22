namespace DupDetector.Sources.Tests;

/// <summary>
///     Builds paths under the fixed harvest root.
/// </summary>
public static class TestPaths
{
    /// <summary>
    ///     The root every fixture path hangs off.
    /// </summary>
    public static string Root { get; }

    static TestPaths()
    {
        Root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "harvest"));
    }

    /// <summary>
    ///     A full path for a slash-separated path under the root.
    /// </summary>
    /// <returns></returns>
    /// <param name="relativePath"></param>
    public static string At(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
