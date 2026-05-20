namespace DupDetector;

public class ClusterMetrics
{
    public int Lines { get; set; }
    public int Occurrences { get; set; }
    public int Spread { get; set; }
    /// <summary>Number of distinct projects containing at least one instance of this cluster.</summary>
    public int ProjectSpread { get; set; }
    /// <summary>Raw uncapped score product (lines × occurrences × spread) / 100. May exceed 100.</summary>
    public double RawScore { get; set; }
    /// <summary>Normalized duplication score 0–100. Higher is worse. This is the authoritative severity field.</summary>
    public double Score { get; set; }
}
