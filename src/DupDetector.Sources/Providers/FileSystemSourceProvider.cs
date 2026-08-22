using DupDetector.Core.Model;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Loads C# files from a directory or a single file.
/// </summary>
public sealed class FileSystemSourceProvider : ISourceProvider
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken)
    {
        var full = Path.GetFullPath(path);

        if (File.Exists(full))
        {
            var root = Path.GetDirectoryName(full)!;
            return FileSystemSources.Read([full], root, settings, cancellationToken);
        }

        if (!Directory.Exists(full))
        {
            return SourceLoadResult.Empty with
            {
                Diagnostics = [SourceDiagnostics.Error($"Path does not exist: {full}", full)],
            };
        }

        var files = Directory.EnumerateFiles(full, "*.cs", FileSystemSources.Enumeration);
        return FileSystemSources.Read(files, full, settings, cancellationToken);
    }
}
