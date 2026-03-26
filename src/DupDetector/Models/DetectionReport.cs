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
}

public class ProjectScore
{
    public string Project { get; set; } = "";
    public int DuplicateLines { get; set; }
    public int TotalLines { get; set; }
    /// <summary>Percentage of the project's lines that are duplicated (0–100).</summary>
    public double Score { get; set; }
}
