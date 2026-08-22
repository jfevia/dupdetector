using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     Covers the disclosure surface added after the report audit.
/// </summary>
public class AnalysisScopeTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryRestrictiveSettingIsDisclosed()
    {
        var detectionSettings = new DetectionSettings
        {
            MinFileSpread = 2,
            MinProjectSpread = 3,
            MaxFileSpread = 20,
            MaxOccurrences = 50,
            IsExcludeTestFiles = true,
            Kinds = DetectionKind.Members,
        };
        var suppressionCounts = new SuppressionCounts
        {
            BelowFileSpread = 4
        };
        var notes = ScopeFixtures.Scope(
detectionSettings,
suppressionCounts).Limitations;

        Assert.Contains(notes, note => note.Contains("fewer than 2 files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("fewer than 3 projects", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("more than 20 files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("more than 50 copies", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("Test files", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("Whole-type duplication", StringComparison.Ordinal));
        Assert.Contains(notes, note => note.Contains("4 further clusters", StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void PermissiveSettingsProduceOnlyTheAlwaysApplicableNotes()
    {
        var detectionSettings2 = new DetectionSettings
        {
            MinFileSpread = 1,
            MinProjectSpread = 1,
            MaxFileSpread = 0,
            MaxOccurrences = 0,
        };
        var notes = ScopeFixtures.Scope(detectionSettings2).Limitations;

        Assert.DoesNotContain(notes, note => note.Contains("fewer than", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("more than", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("Test files", StringComparison.Ordinal));
        Assert.DoesNotContain(notes, note => note.Contains("Whole-type duplication", StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
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
