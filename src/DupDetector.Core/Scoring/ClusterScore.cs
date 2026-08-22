using DupDetector.Core.Model;

namespace DupDetector.Core.Scoring;

/// <summary>
///     Converts a cluster's removable-line count into a 0-100 severity score.
/// </summary>
public static class ClusterScore
{
    /// <summary>
    ///     Removable-line count that scores 100.
    /// </summary>
    public const int Anchor = 1000;

    /// <summary>
    ///     Human-readable form of the curve, derived from the constant rather than restated.
    /// </summary>
    public static string Formula
    {
        get
        {
            return $"100 * ln(1 + removableLines) / ln(1 + {Anchor})";
        }
    }

    /// <summary>
    ///     Returns the 0-100 severity of <paramref name="removableLines"/>, rounded to two places.
    /// </summary>
    /// <returns></returns>
    /// <param name="removableLines"></param>
    public static double For(int removableLines)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(removableLines);

        var raw = 100.0 * Math.Log(1 + removableLines) / Math.Log(1 + Anchor);
        return AggregateScorer.RoundPercentage(Math.Min(100.0, raw));
    }

    /// <summary>
    ///     Severity of a cluster's measured metrics.
    /// </summary>
    /// <returns></returns>
    /// <param name="metrics"></param>
    public static double For(ClusterMetrics metrics)
    {
        return For(metrics.RemovableLines);
    }
}
