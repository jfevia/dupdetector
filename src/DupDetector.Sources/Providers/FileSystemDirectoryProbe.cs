namespace DupDetector.Sources.Providers;

/// <summary>
///     
/// </summary>
public sealed class FileSystemDirectoryProbe : IDirectoryProbe
{
    /// <summary>
    ///     
    /// </summary>
    public static FileSystemDirectoryProbe Instance { get; }

    static FileSystemDirectoryProbe()
    {
        var probe = new FileSystemDirectoryProbe();
        Instance = probe;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="directory"></param>
    public string? FindProjectFile(string directory)
    {
        try
        {
            using var files = Directory.EnumerateFiles(directory, "*.csproj").GetEnumerator();
            return files.MoveNext() ? files.Current : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
