using DupDetector.Core.Model;
using DupDetector.Core.Scoring;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="ClusterDocument" />.
/// </summary>
public static class ClusterDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="cluster"></param>
    /// <param name="includeRawSnippets"></param>
    public static ClusterDocument From(DuplicateCluster cluster, bool includeRawSnippets)
    {

        var instances = new List<InstanceDocument>(cluster.Instances.Count);
        foreach (var instance in cluster.Instances)
        {
            instances.Add(InstanceDocuments.From(instance));
        }

        var clusterDocument = new ClusterDocument
        {
            Id = cluster.Id,
            Lines = cluster.Metrics.Lines,
            Occurrences = cluster.Metrics.Occurrences,
            FileSpread = cluster.Metrics.FileSpread,
            ProjectSpread = cluster.Metrics.ProjectSpread,
            IsProjectSpreadKnown = cluster.Metrics.IsProjectSpreadKnown,
            RemovableLines = cluster.Metrics.RemovableLines,
            Score = ClusterScore.For(cluster.Metrics),
            IsExact = cluster.IsExact,
            IsCohesive = cluster.IsCohesive,
            IsProductionDuplicate = cluster.IsProductionDuplicate,
            NormalizedSnippet = cluster.NormalizedSnippet,
            Instances = instances,
            RawSnippets = includeRawSnippets ? cluster.RawSnippets : null,
        };
        return clusterDocument;
    }
}
