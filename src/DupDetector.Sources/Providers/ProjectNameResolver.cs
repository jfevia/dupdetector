using DupDetector.Core.Model;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Finds the project that owns a file by walking up to the nearest project file.
/// </summary>
public sealed class ProjectNameResolver
{
    private readonly Dictionary<string, ProjectIdentity> _byDirectory;
    private readonly IDirectoryProbe _probe;

    /// <summary>
    ///     Number of directories resolved so far. Exposed to prove the cache is doing its job.
    /// </summary>
    public int CachedDirectoryCount
    {
        get
        {
            return _byDirectory.Count;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public ProjectNameResolver()
        : this(FileSystemDirectoryProbe.Instance)
    {

        _byDirectory = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="probe"></param>
    public ProjectNameResolver(IDirectoryProbe probe)
    {

        _byDirectory = new(StringComparer.OrdinalIgnoreCase);
        _probe = probe;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="filePath"></param>
    public ProjectIdentity Resolve(string filePath)
    {

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        return directory is null ? ProjectIdentity.Unknown : ResolveDirectory(directory);
    }

    private ProjectIdentity ResolveDirectory(string directory)
    {
        if (_byDirectory.TryGetValue(directory, out var cached))
        {
            return cached;
        }

        var visited = new List<string>();
        var identity = ProjectIdentity.Unknown;

        for (var current = directory; current is not null; current = Path.GetDirectoryName(current))
        {
            if (_byDirectory.TryGetValue(current, out var known))
            {
                identity = known;
                break;
            }

            visited.Add(current);

            var project = _probe.FindProjectFile(current);
            if (project is not null)
            {
                identity = ProjectIdentities.Named(Path.GetFileNameWithoutExtension(project));
                break;
            }
        }

        foreach (var entry in visited)
        {
            _byDirectory[entry] = identity;
        }

        return identity;
    }
}
