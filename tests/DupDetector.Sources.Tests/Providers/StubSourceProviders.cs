using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     Creates stub source providers.
/// </summary>
public static class StubSourceProviders
{
    /// <summary>
    ///     A provider that reports the workspace mode for workspace paths and file system otherwise.
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    public static ISourceProvider ByPath(string path)
    {
        var mode = path.EndsWith("workspace", StringComparison.Ordinal)
            ? DiscoveryMode.Workspace
            : DiscoveryMode.FileSystem;

        var provider = new StubSourceProvider(mode);
        return provider;
    }
}
