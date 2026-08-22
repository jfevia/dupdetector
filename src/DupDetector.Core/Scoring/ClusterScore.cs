using DupDetector.Core.Model;

namespace DupDetector.Core.Scoring;

/// <summary>
/// Converts a cluster's removable-line count into a 0-100 severity score.
/// </summary>
/// <remarks>
/// <para>
/// Severity is derived from <see cref="ClusterMetrics.RemovableLines"/>: the lines that disappear if
/// every copy but one is deleted. That is the concrete debt a cluster represents, so unlike a
/// product of size, occurrences and spread it cannot rate a large verbatim copy and a small
/// widespread one identically.
/// </para>
/// <para>
/// The curve is logarithmic so that ordinary duplication occupies the middle of the range and the
/// top is reserved for genuinely pervasive debt.
/// </para>
/// </remarks>
public static class ClusterScore
{
    /// <summary>Removable-line count that scores 100.</summary>
    public const int Anchor = 1000;

    /// <summary>Human-readable form of the curve, derived from the constant rather than restated.</summary>
    public static string Formula => $"100 * ln(1 + removableLines) / ln(1 + {Anchor})";

    /// <summary>
    /// Returns the 0-100 severity of <paramref name="removableLines"/>, rounded to two places.
    /// </summary>
    public static double For(int removableLines)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(removableLines);

        var raw = 100.0 * Math.Log(1 + removableLines) / Math.Log(1 + Anchor);
        return AggregateScorer.RoundPercentage(Math.Min(100.0, raw));
    }

    /// <summary>Severity of a cluster's measured metrics.</summary>
    public static double For(ClusterMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        return For(metrics.RemovableLines);
    }
}
