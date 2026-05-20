namespace DupDetector;

public class DetectionReport
{
    public ReportSummary Summary { get; set; } = new();
    public List<DuplicateCluster> Clusters { get; set; } = new();
    public List<FileScore> FileScores { get; set; } = new();
    public List<ProjectScore> ProjectScores { get; set; } = new();
}

public class ReportSummary
{
    public int TotalFiles { get; set; }
    public int TotalDuplicates { get; set; }
    public int TotalDuplicateLines { get; set; }
    /// <summary>Normalized solution-level duplication score 0–100.</summary>
    public double DuplicationScore { get; set; }
    /// <summary>Human-readable label: low / medium / high / critical.</summary>
    public string ScoreLabel { get; set; } = "low";
}

public class FileScore
{
    public string File { get; set; } = "";
    public int DuplicateLines { get; set; }
    public int TotalLines { get; set; }
    /// <summary>Percentage of the file's lines that are duplicated (0–100).</summary>
    public double Score { get; set; }
    /// <summary>
    /// True when the file matches common test-project path heuristics
    /// (path segment contains Tests/Test/Specs, or filename ends in Tests.cs/Spec.cs).
    /// </summary>
    public bool IsTestFile { get; set; }
    /// <summary>
    /// Number of distinct duplicate clusters that contain at least one instance in this file.
    /// Provides context alongside the percentage score — e.g., 85% from 1 cluster (spread=2)
    /// is a very different finding from 85% from 12 clusters (avg spread=15).
    /// </summary>
    public int ClusterCount { get; set; }
    /// <summary>
    /// File-spread of the widest cluster affecting this file.
    /// Helps distinguish a high-score caused by a single broad cluster vs many local clusters.
    /// </summary>
    public int TopClusterSpread { get; set; }
}

public class ProjectScore
{
    public string Project { get; set; } = "";
    public int DuplicateLines { get; set; }
    public int TotalLines { get; set; }
    /// <summary>Percentage of the project's lines that are duplicated (0–100).</summary>
    public double Score { get; set; }
}
