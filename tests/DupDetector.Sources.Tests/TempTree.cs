using System.Text;

namespace DupDetector.Sources.Tests;

/// <summary>
///     A disposable temporary directory tree.
/// </summary>
public sealed class TempTree : IDisposable
{

    /// <summary>
    ///     
    /// </summary>
    public string Root { get; }

    /// <summary>
    ///     
    /// </summary>
    public TempTree()
    {
        Root = Path.Combine(Path.GetTempPath(), "dupdetector-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>
    ///     
    /// </summary>
    public void Dispose()
    {
        _ = CanTryDelete();
    }

    /// <summary>
    ///     Creates a subdirectory and returns its full path.
    /// </summary>
    /// <returns></returns>
    /// <param name="relativePath"></param>
    public string AddDirectory(string relativePath)
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(full);
        return full;
    }

    /// <summary>
    ///     A path inside the tree that no file occupies.
    /// </summary>
    /// <returns></returns>
    /// <param name="fileName"></param>
    public string Missing(string fileName)
    {
        return Path.Combine(Root, "missing", fileName);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="content"></param>
    /// <param name="relativePath"></param>
    public string Write(string relativePath, string content)
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var encoding = new UTF8Encoding(false);
        File.WriteAllText(full, content, encoding);
        return full;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="content"></param>
    /// <param name="relativePath"></param>
    public string WriteBytes(string relativePath, byte[] content)
    {
        var full = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, content);
        return full;
    }

    private bool CanTryDelete()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
