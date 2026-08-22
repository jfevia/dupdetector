using DupDetector.Core.Model;

namespace DupDetector.Core.Scoring;

/// <summary>
///     Counts the distinct lines covered by a set of ranges.
/// </summary>
public static class LineSpanMerger
{
    /// <summary>
    ///     Returns how many distinct lines the given ranges cover, counting a line shared by several
    ///     ranges only once.
    /// </summary>
    /// <param name="ranges">The ranges to count, in any order.</param>
    /// <returns>The number of distinct lines the ranges cover.</returns>
    public static int CountDistinctLines(IEnumerable<LineRange> ranges)
    {
        var total = 0;
        foreach (var range in Merge(ranges))
        {
            total += range.Count;
        }

        return total;
    }

    /// <summary>
    ///     Merges overlapping and touching ranges into a minimal, ordered set.
    /// </summary>
    /// <param name="ranges">The ranges to merge, in any order.</param>
    /// <returns>The merged ranges, ordered by start line.</returns>
    public static IReadOnlyList<LineRange> Merge(IEnumerable<LineRange> ranges)
    {

        var ordered = new List<LineRange>(ranges);
        ordered.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        if (ordered.Count == 0)
        {
            return [];
        }

        var merged = new List<LineRange>();
        var start = ordered[0].Start;
        var end = ordered[0].End;

        for (var index = 1; index < ordered.Count; index++)
        {
            var range = ordered[index];
            if (range.Start <= end + 1)
            {
                end = Math.Max(end, range.End);
            }
            else
            {
                var completed = new LineRange(start, end);
                merged.Add(completed);
                start = range.Start;
                end = range.End;
            }
        }

        var last = new LineRange(start, end);
        merged.Add(last);
        return merged;
    }
}
