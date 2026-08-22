namespace DupDetector.Core.Model;

/// <summary>
/// Measured facts about a cluster. Deliberately contains no 0-100 severity score: metrics are
/// observations, severity is policy and lives in the scoring layer.
/// </summary>
public sealed record ClusterMetrics(
    int Lines,
    int Occurrences,
    int FileSpread,
    int ProjectSpread,
    bool ProjectSpreadKnown)
{
    /// <summary>
    /// Lines that would disappear if every copy but one were removed. This is the concrete debt the
    /// cluster represents, and it is what the severity score is derived from.
    /// </summary>
    public int RemovableLines => Lines * (Occurrences - 1);
}
