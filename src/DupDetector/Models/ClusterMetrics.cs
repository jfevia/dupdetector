namespace DupDetector;

public class ClusterMetrics
{
    public int Lines { get; set; }
    public int Occurrences { get; set; }
    public int Spread { get; set; }
    public double Score { get; set; }
    /// <summary>Normalized duplication score 0–100. Higher is worse.</summary>
    public double DuplicationScore { get; set; }
}
