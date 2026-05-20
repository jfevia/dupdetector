using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for <see cref="LineCountHelper.CountUniqueLines"/>.
/// Verifies that overlapping and adjacent intervals are merged so that
/// the same line is never counted twice (addresses GAP-1 and GAP-2).
/// </summary>
public class LineCountingTests
{
    // ──── CountUniqueLines unit tests ────────────────────────────────────────

    [Fact]
    public void NoIntervals_ReturnsZero()
    {
        Assert.Equal(0, LineCountHelper.CountUniqueLines([]));
    }

    [Fact]
    public void SingleInterval_ReturnsCorrectCount()
    {
        Assert.Equal(5, LineCountHelper.CountUniqueLines([(1, 5)]));
    }

    [Fact]
    public void NonOverlappingIntervals_ReturnsSumOfLengths()
    {
        // Lines 1-3 = 3, lines 10-12 = 3 → total 6
        Assert.Equal(6, LineCountHelper.CountUniqueLines([(1, 3), (10, 12)]));
    }

    [Fact]
    public void FullyOverlappingIntervals_CountsOnce()
    {
        // Both intervals cover lines 5-10 — should count 6 unique lines
        Assert.Equal(6, LineCountHelper.CountUniqueLines([(5, 10), (5, 10)]));
    }

    [Fact]
    public void PartiallyOverlappingIntervals_MergesCorrectly()
    {
        // [1,5] and [3,8] → merged [1,8] = 8 lines
        Assert.Equal(8, LineCountHelper.CountUniqueLines([(1, 5), (3, 8)]));
    }

    [Fact]
    public void AdjacentIntervals_AreMerged()
    {
        // [1,5] and [6,10] are adjacent — merged [1,10] = 10 lines
        Assert.Equal(10, LineCountHelper.CountUniqueLines([(1, 5), (6, 10)]));
    }

    [Fact]
    public void SubsetInterval_DoesNotInflateCount()
    {
        // [1,10] fully contains [3,7] — unique count still 10
        Assert.Equal(10, LineCountHelper.CountUniqueLines([(1, 10), (3, 7)]));
    }

    [Fact]
    public void UnsortedIntervals_AreHandledCorrectly()
    {
        // Provide out-of-order intervals; result should be same as sorted
        Assert.Equal(8, LineCountHelper.CountUniqueLines([(3, 8), (1, 5)]));
    }

    [Fact]
    public void ManyOverlappingIntervals_ProducesUniqueCount()
    {
        // Simulate sliding-window scenario: windows 1-5, 2-6, 3-7, 4-8, 5-9 all overlap
        var intervals = Enumerable.Range(1, 5).Select(s => (s, s + 4)).ToList();
        // All cover lines 1-9 = 9 unique lines
        Assert.Equal(9, LineCountHelper.CountUniqueLines(intervals));
    }

    [Fact]
    public void MultipleDisjointAndOverlapping_MergesCorrectly()
    {
        // [1,3], [2,5], [10,12], [11,15] → [1,5]=5, [10,15]=6 → total 11
        Assert.Equal(11, LineCountHelper.CountUniqueLines([(1, 3), (2, 5), (10, 12), (11, 15)]));
    }

    // ──── Integration: file scores never exceed 100% ─────────────────────────

    [Fact]
    public void FileScore_NeverExceedsOneHundred_WhenMultipleClustersOverlap()
    {
        // Create a file with 10 lines
        // Simulate two clusters that each cover the entire file (overlap completely)
        // Result: fileDuplicateLines should equal 10, not 20
        var fileIntervals = new List<(int, int)>
        {
            (1, 10),  // cluster A covers entire file
            (1, 10),  // cluster B covers exact same range
        };
        var unique = LineCountHelper.CountUniqueLines(fileIntervals);
        var totalLines = 10;
        var score = Math.Min(100.0, unique * 100.0 / totalLines);

        Assert.Equal(10, unique);
        Assert.Equal(100.0, score, precision: 1);
    }

    [Fact]
    public void FileScore_WhenNoOverlap_IsCorrectFraction()
    {
        // File has 20 lines; two non-overlapping clusters cover lines 1-5 and 10-14
        // = 5 + 5 = 10 unique lines → 50%
        var fileIntervals = new List<(int, int)> { (1, 5), (10, 14) };
        var unique = LineCountHelper.CountUniqueLines(fileIntervals);
        var totalLines = 20;
        var score = Math.Round(unique * 100.0 / totalLines, 2);

        Assert.Equal(10, unique);
        Assert.Equal(50.0, score, precision: 1);
    }

    // ──── Integration: solution score uses unique lines ───────────────────────

    [Fact]
    public void SolutionScore_UsesUniqueLines_NotLinesTimesOccurrences()
    {
        // If the old formula (lines × occurrences) were used:
        //   4 occurrences × 10 lines = 40 duplicate lines (total file lines = 20 → 200% = capped 100%)
        // With unique-line merging: 4 occurrences in same file, all covering lines 1-10
        //   = 10 unique duplicate lines / 20 total lines = 50%

        var fileIntervals = new List<(int, int)>
        {
            (1, 10), (1, 10), (1, 10), (1, 10)   // 4 occurrences, same 10 lines
        };
        var unique = LineCountHelper.CountUniqueLines(fileIntervals);
        var totalLines = 20;
        var score = Math.Min(100.0, unique * 100.0 / totalLines);

        Assert.Equal(10, unique);
        Assert.Equal(50.0, score, precision: 1);
    }
}
