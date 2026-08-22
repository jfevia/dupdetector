using DupDetector.Core.Model;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     Helpers for <see cref="ModelProjectionTests" />.
/// </summary>
public static class ProjectionFixtures
{

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="hashes"></param>
    public static DuplicateCluster Cluster(IReadOnlyList<string> hashes)
    {
        var instances = new List<CodeInstance>(hashes.Count);
        for (var index = 0; index < hashes.Count; index++)
        {
            var lineRange = new LineRange(1, 2);
            var location = new CodeLocation($"/f{index}.cs", ProjectIdentity.Unknown, false, lineRange);
            var codeInstance = new CodeInstance(location, "M", hashes[index]);
            instances.Add(codeInstance);
        }

        var clusterSpread2 = new ClusterSpread(hashes.Count, 0, false);
        var metrics = new ClusterMetrics(2, hashes.Count, clusterSpread2);
        var value2 = new DuplicateCluster()
        {
            Id = "dup-1",
            Instances = instances,
            Metrics = metrics,
            NormalizedSnippet = "n",
            RawSnippets = ["r"],
            IsCohesive = true,
            IsProductionDuplicate = false,
        };
        return value2;
    }
}
