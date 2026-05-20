namespace DupDetector;

public class DuplicateCluster
{
    public string Id { get; set; } = "";
    public List<CodeInstance> Instances { get; set; } = new();
    public ClusterMetrics Metrics { get; set; } = new();
    public string NormalizedSnippet { get; set; } = "";
    public List<string> RawSnippets { get; set; } = new();
    /// <summary>
    /// True when every instance in this cluster is a verbatim (hash-exact) match.
    /// False for near-duplicate clusters detected by Jaccard similarity.
    /// </summary>
    public bool IsExact { get; set; }
    /// <summary>
    /// True when <see cref="IsExact"/> is true and <c>avgLines × fileSpread ≥ 100</c>.
    /// Surfaces large verbatim copies that the occurrence multiplier would otherwise bury.
    /// For example, a 71-line method copied into 2 files (71 × 2 = 142 ≥ 100) is high-impact
    /// even though it only appears twice.
    /// </summary>
    public bool IsHighImpact { get; set; }
    /// <summary>
    /// True when this is a verbatim exact-match cluster spanning at least 2 distinct projects
    /// and at least one instance is in a production (non-test) file.
    /// Production source duplicates are always high-priority regardless of line count or
    /// occurrence count, since copy-pasted production code creates maintenance debt that
    /// cannot be dismissed as intentional test boilerplate.
    /// </summary>
    public bool IsProductionDuplicate { get; set; }
}
