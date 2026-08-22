using DupDetector.Core.Model;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Loads source files from one input path.
/// </summary>
public interface ISourceProvider
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="path"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken);
}
