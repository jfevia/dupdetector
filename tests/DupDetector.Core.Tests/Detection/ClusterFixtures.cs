using DupDetector.Core.Model;

using DupDetector.TestKit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Helpers for <see cref="ClusterFiltersTests" />.
/// </summary>
public static class ClusterFixtures
{
    /// <summary>
    ///     Builds a cluster whose instances live at the given paths and projects.
    /// </summary>
    /// <returns></returns>
    /// <param name="instances"></param>
    public static DuplicateCluster Make(IReadOnlyList<InstanceSpec> instances)
    {
        var codeInstances = new List<CodeInstance>(instances.Count);
        foreach (var instance in instances)
        {
            var identity = ProjectIdentities.Named(instance.Project);
            var lineRange = new LineRange(1, 3);
            var location = new CodeLocation(instance.Path, identity, false, lineRange);
            var codeInstance = new CodeInstance(location, "M", "h");
            codeInstances.Add(codeInstance);
        }

        var clusterSpread = new ClusterSpread(instances.Count, 1, true);
        var metrics = new ClusterMetrics(3, instances.Count, clusterSpread);
        var value = new DuplicateCluster()
        {
            Id = "dup-1",
            Instances = codeInstances,
            Metrics = metrics,
            NormalizedSnippet = "n",
            RawSnippets = ["public void IArchRule() { }"],
            IsCohesive = true,
            IsProductionDuplicate = false,
        };
        return value;
    }
}
