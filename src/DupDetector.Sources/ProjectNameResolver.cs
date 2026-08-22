using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
/// Finds the project that owns a file by walking up to the nearest project file.
/// </summary>
/// <remarks>
/// Results are memoised per directory. Without that cache every file in a directory repeats the
/// same walk, which measured as the single largest cost in a directory scan.
/// </remarks>
public sealed class ProjectNameResolver
{
    private readonly Dictionary<string, ProjectIdentity> _byDirectory = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDirectoryProbe _probe;

    public ProjectNameResolver()
        : this(FileSystemDirectoryProbe.Instance)
    {
    }

    internal ProjectNameResolver(IDirectoryProbe probe) => _probe = probe;

    /// <summary>Number of directories resolved so far. Exposed to prove the cache is doing its job.</summary>
    public int CachedDirectoryCount => _byDirectory.Count;

    public ProjectIdentity Resolve(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        return directory is null ? ProjectIdentity.Unknown : ResolveDirectory(directory);
    }

    private ProjectIdentity ResolveDirectory(string directory)
    {
        if (_byDirectory.TryGetValue(directory, out var cached))
        {
            return cached;
        }

        // Remember the whole chain, so a sibling file never repeats this walk.
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
                identity = ProjectIdentity.Named(Path.GetFileNameWithoutExtension(project));
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

/// <summary>Directory lookup seam, so resolution can be tested without touching a disk.</summary>
internal interface IDirectoryProbe
{
    string? FindProjectFile(string directory);
}

internal sealed class FileSystemDirectoryProbe : IDirectoryProbe
{
    internal static FileSystemDirectoryProbe Instance { get; } = new();

    public string? FindProjectFile(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*.csproj").FirstOrDefault();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // An unreadable directory simply has no project file to offer.
            return null;
        }
    }
}
