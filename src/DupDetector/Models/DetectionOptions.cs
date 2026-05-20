namespace DupDetector;

public class DetectionOptions
{
    public int MinLines { get; set; } = 5;
    /// <summary>
    /// Jaccard similarity threshold for near-duplicate detection. Default raised to 0.90
    /// to reduce generic structural false positives (e.g., any short method with a guard + return).
    /// </summary>
    public double Similarity { get; set; } = 0.90;
    public string Format { get; set; } = "yaml";
    public List<string> Exclude { get; set; } = new();
    public bool IncludeGenerated { get; set; } = false;
    public DetectionKind DetectionKinds { get; set; } = DetectionKind.All;
    public List<string> InputPaths { get; set; } = new();
    public string OutputPath { get; set; } = "";
    /// <summary>
    /// Discard near-duplicate clusters whose file spread exceeds this value.
    /// Prevents generic structural patterns from forming mega-clusters.
    /// Default: 20. Set to 0 to disable filtering.
    /// </summary>
    public int MaxClusterSpread { get; set; } = 20;
    /// <summary>
    /// Discard near-duplicate clusters whose occurrence count exceeds this value.
    /// Default: 50. Set to 0 to disable filtering.
    /// </summary>
    public int MaxClusterOccurrences { get; set; } = 50;
    /// <summary>
    /// When true, test files are excluded from fileScores and projectScores output.
    /// Test files are identified by path heuristics (contains /Tests/, /Test/, /Specs/, etc.).
    /// </summary>
    public bool ExcludeTestFiles { get; set; } = false;
}
