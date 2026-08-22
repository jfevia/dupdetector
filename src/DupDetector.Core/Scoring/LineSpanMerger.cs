using DupDetector.Core.Model;

namespace DupDetector.Core.Scoring;

/// <summary>
/// Counts the distinct lines covered by a set of ranges.
/// </summary>
public static class LineSpanMerger
{
    /// <summary>
    /// Merges overlapping and touching ranges into a minimal, ordered set, so a line covered by
    /// several clusters appears exactly once.
    /// </summary>
    public static IReadOnlyList<LineRange> Merge(IEnumerable<LineRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);

        var ordered = ranges.OrderBy(range => range.Start).ToArray();
        if (ordered.Length == 0)
        {
            return [];
        }

        var merged = new List<LineRange>();
        var start = ordered[0].Start;
        var end = ordered[0].End;

        foreach (var range in ordered.Skip(1))
        {
            if (range.Start <= end + 1)
            {
                end = Math.Max(end, range.End);
            }
            else
            {
                merged.Add(new LineRange(start, end));
                start = range.Start;
                end = range.End;
            }
        }

        merged.Add(new LineRange(start, end));
        return merged;
    }

    /// <summary>
    /// Returns how many distinct lines the given ranges cover. Overlapping and touching ranges are
    /// merged first, so a line counted by several clusters is still only counted once.
    /// </summary>
    public static int CountDistinctLines(IEnumerable<LineRange> ranges) =>
        Merge(ranges).Sum(range => range.Count);
}
