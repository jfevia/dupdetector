namespace DupDetector.Sources.Providers;

/// <summary>
///     Helpers for the MSBuild source provider.
/// </summary>
public static class MicrosoftBuildSources
{
    /// <summary>
    ///     Extensions the MSBuild provider understands.
    /// </summary>
    public static IReadOnlyList<string> Extensions { get; }

    static MicrosoftBuildSources()
    {
        Extensions = [".sln", ".slnf", ".csproj"];
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    public static bool CanHandle(string path)
    {
        var extension = Path.GetExtension(path);
        foreach (var candidate in Extensions)
        {
            if (string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="diagnostic"></param>
    public static bool IsError(SourceDiagnostic diagnostic)
    {
        return diagnostic.Severity == SourceDiagnosticSeverity.Error;
    }
}
