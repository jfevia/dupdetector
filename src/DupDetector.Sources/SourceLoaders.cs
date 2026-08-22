using DupDetector.Sources.Providers;

namespace DupDetector.Sources;

/// <summary>
///     Chooses the source provider for a path.
/// </summary>
public static class SourceLoaders
{
    /// <summary>
    ///     Chooses the provider for a path by its extension.
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    public static ISourceProvider Resolve(string path)
    {
        if (SolutionXmlSources.CanHandle(path))
        {
            var solutionXmlSourceProvider = new SolutionXmlSourceProvider();
            return solutionXmlSourceProvider;
        }

        if (MicrosoftBuildSources.CanHandle(path))
        {
            var microsoftBuildSourceProvider = new MicrosoftBuildSourceProvider();
            return microsoftBuildSourceProvider;
        }

        var fileSystemSourceProvider = new FileSystemSourceProvider();
        return fileSystemSourceProvider;
    }
}
