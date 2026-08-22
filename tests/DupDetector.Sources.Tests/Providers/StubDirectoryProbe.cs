using DupDetector.Sources.Providers;

namespace DupDetector.Sources.Tests.Providers;

/// <summary>
///     A directory probe backed by a fixed map, so lookups are counted and deterministic.
/// </summary>
public sealed class StubDirectoryProbe : IDirectoryProbe
{
    private readonly Dictionary<string, string?> _projects;

    /// <summary>
    ///     How many times the probe was asked.
    /// </summary>
    public int Calls { get; private set; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="projects">The project file for each directory.</param>
    public StubDirectoryProbe(Dictionary<string, string?> projects)
    {
        _projects = projects;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="directory"></param>
    public string? FindProjectFile(string directory)
    {
        Calls++;
        return _projects.TryGetValue(directory, out var project) ? project : null;
    }
}
