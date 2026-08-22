using DupDetector.Core.Model;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     Helpers for <see cref="DetectorCliqueIntegrationTests" />.
/// </summary>
public static class CliqueAssertions
{

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="cluster"></param>
    /// <param name="path"></param>
    public static bool CanTouches(DuplicateCluster cluster, string path)
    {
        foreach (var instance in cluster.Instances)
        {
            if (instance.FilePath == path)
            {
                return true;
            }
        }

        return false;
    }
}
