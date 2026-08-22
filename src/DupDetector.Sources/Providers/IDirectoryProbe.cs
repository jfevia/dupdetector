namespace DupDetector.Sources.Providers;

/// <summary>
///     Directory lookup seam, so resolution can be tested without touching a disk.
/// </summary>
public interface IDirectoryProbe
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="directory"></param>
    string? FindProjectFile(string directory);
}
