namespace DupDetector;

/// <summary>
/// Utility for computing the number of unique lines covered by a set of line-range intervals.
/// Used to prevent overcounting when multiple clusters overlap in the same file.
/// </summary>
public static class LineCountHelper
{
    /// <summary>
    /// Returns the total number of unique 1-based line numbers covered by the given intervals.
    /// Overlapping and adjacent intervals are merged before counting.
    /// </summary>
    /// <param name="intervals">A sequence of (inclusive start line, inclusive end line) pairs.</param>
    public static int CountUniqueLines(IEnumerable<(int Start, int End)> intervals)
    {
        var sorted = intervals
            .Where(r => r.Start <= r.End)
            .OrderBy(r => r.Start)
            .ToList();

        if (sorted.Count == 0) return 0;

        int totalLines = 0;
        int mergedStart = sorted[0].Start;
        int mergedEnd = sorted[0].End;

        for (int i = 1; i < sorted.Count; i++)
        {
            var (start, end) = sorted[i];
            if (start <= mergedEnd + 1)
            {
                // Overlapping or adjacent — extend the current merged range
                if (end > mergedEnd) mergedEnd = end;
            }
            else
            {
                totalLines += mergedEnd - mergedStart + 1;
                mergedStart = start;
                mergedEnd = end;
            }
        }

        totalLines += mergedEnd - mergedStart + 1;
        return totalLines;
    }
}
