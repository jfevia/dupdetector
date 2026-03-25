namespace DupDetector;

public class DuplicateCluster
{
    public string Id { get; set; } = "";
    public List<CodeInstance> Instances { get; set; } = new();
    public ClusterMetrics Metrics { get; set; } = new();
    public string NormalizedSnippet { get; set; } = "";
    public List<string> RawSnippets { get; set; } = new();
}
