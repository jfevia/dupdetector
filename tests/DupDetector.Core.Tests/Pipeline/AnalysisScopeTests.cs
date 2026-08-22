using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;
using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
/// Covers the disclosure surface added after the report audit.
/// </summary>
public class AnalysisScopeTests
{
    private static AnalysisScope Scope(DetectionSettings settings, SuppressionCounts? suppressed = null) =>
        new() { Settings = settings, Suppressed = suppressed ?? SuppressionCounts.Empty };

    [Fact]
    public void PermissiveSettingsProduceOnlyTheAlwaysApplicableNotes()
    {
        var notes = Scope(new DetectionSettings
        {
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MaxFileSpread = 0,
            MaxOccurrences = 0,
        }).Limitations;

        Assert.DoesNotContain(notes, note => note.Contains("fewer than", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("more than", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("Test files", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("Whole-type duplication", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryRestrictiveSettingIsDisclosed()
    {
        var notes = Scope(
            new DetectionSettings
            {
                MinFileSpread = 2,
                MinProjectSpread = 3,
                MaxFileSpread = 20,
                MaxOccurrences = 50,
                ExcludeTestFiles = true,
                Kinds = DetectionKind.Members,
            },
            new SuppressionCounts { BelowFileSpread = 4 }).Limitations;

        Assert.Contains(notes, note => note.Contains("fewer than 2 files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("fewer than 3 projects", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("more than 20 files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("more than 50 copies", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("Test files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("Whole-type duplication", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("4 further clusters", StringComparison.Ordinal));
    }

    [Fact]
    public void SuppressionTotalSumsEveryReason()
    {
        var counts = new SuppressionCounts
        {
            BelowFileSpread = 1,
            BelowProjectSpread = 2,
            AboveFileSpread = 3,
            AboveOccurrences = 4,
            ContainedInLargerCluster = 5,
            ExcludedBySnippetPattern = 6,
            ExcludedByFileGlob = 7,
            ExcludedByProjectPattern = 8,
        };

        Assert.Equal(36, counts.Total);
        Assert.Equal(0, SuppressionCounts.Empty.Total);
    }
}

/// <summary>
/// Covers classification of physical lines as code or not.
/// </summary>
public class CodeLineMapTests
{
    [Fact]
    public void AnEmptyFileHasNoCodeLines()
    {
        var unit = Code.Unit(string.Empty);

        Assert.Equal(0, CodeLineMap.Create(unit.Tree, 0).Total);
        Assert.Equal(0, CodeLineMap.Empty.Total);
        Assert.Equal(0, CodeLineMap.Empty.CountIn(new LineRange(1, 10)));
    }

    [Fact]
    public void BlanksAndCommentsAreNotCodeButStringContentIs()
    {
        const string source = """
            // leading comment

            class C
            {
                string M() => "// not a comment";
            }
            """;

        var unit = Code.Unit(source);
        var map = CodeLineMap.Create(unit.Tree, LineCounter.Count(source));

        Assert.Equal(4, map.Total);
        Assert.Equal(4, map.CountIn(new LineRange(1, 6)));
    }

    [Fact]
    public void CountingIsClampedToTheFileRatherThanThrowing()
    {
        const string source = """
            class C
            {
                int M() => 1;
            }
            """;

        var map = CodeLineMap.Create(Code.Unit(source).Tree, LineCounter.Count(source));

        Assert.Equal(4, map.CountIn(new LineRange(1, 999)));
        Assert.Equal(0, map.CountIn(new LineRange(50, 60)));
    }
}

/// <summary>
/// Covers merging of duplicated line ranges.
/// </summary>
public class LineSpanMergerTests
{
    [Fact]
    public void EmptyInputMergesToNothing() =>
        Assert.Empty(Scoring.LineSpanMerger.Merge([]));

    [Fact]
    public void OverlappingAndTouchingRangesCollapseButDisjointOnesDoNot()
    {
        var merged = Scoring.LineSpanMerger.Merge(
            [new LineRange(1, 5), new LineRange(4, 8), new LineRange(9, 10), new LineRange(20, 22)]);

        Assert.Equal(2, merged.Count);
        Assert.Equal(new LineRange(1, 10), merged[0]);
        Assert.Equal(new LineRange(20, 22), merged[1]);
    }
}
