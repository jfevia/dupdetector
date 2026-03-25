namespace DupDetector;

public class DetectionReport
{
    public ReportSummary Summary { get; set; } = new();
    public List<DuplicateCluster> Clusters { get; set; } = new();
}

public class ReportSummary
{
    public int TotalFiles { get; set; }
    public int TotalDuplicates { get; set; }
    public int TotalDuplicateLines { get; set; }
}
