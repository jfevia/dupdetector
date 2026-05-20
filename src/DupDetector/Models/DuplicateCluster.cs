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
}
