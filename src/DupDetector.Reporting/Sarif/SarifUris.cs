namespace DupDetector.Reporting.Sarif;

/// <summary>
///     Converts paths into the URI form SARIF expects.
/// </summary>
public static class SarifUris
{
    /// <summary>
    ///     Converts a path to a URI, passing a relative path through unchanged.
    /// </summary>
    /// <param name="path">The path to convert.</param>
    /// <returns>An absolute file URI, or the path with forward slashes.</returns>
    public static string ToUri(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var absolute)
            ? absolute.AbsoluteUri
            : path.Replace('\\', '/');
    }
}
