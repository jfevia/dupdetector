using DupDetector.Reporting.Documents;
using Xunit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     
/// </summary>
public class ReportDocumentTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void From_OmitsRawSnippetsUnlessRequested()
    {
        Assert.Null(ReportDocuments.From(Reports.Sample(), includeRawSnippets: false).Clusters[0].RawSnippets);
        Assert.NotNull(ReportDocuments.From(Reports.Sample(), includeRawSnippets: true).Clusters[0].RawSnippets);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void From_ProjectsEveryMeasuredValue()
    {
        var document = ReportDocuments.From(Reports.Sample(), includeRawSnippets: true);

        Assert.Equal("dup-abc123abc123", document.Clusters[0].Id);
        Assert.Equal(10, document.Clusters[0].Lines);
        Assert.Equal(2, document.Clusters[0].Occurrences);
        Assert.Equal(10, document.Clusters[0].RemovableLines);
        Assert.True(document.Clusters[0].IsExact);
        Assert.True(document.Clusters[0].IsProductionDuplicate);
        Assert.False(document.Clusters[0].IsProjectSpreadKnown);

        Assert.Equal("Alpha", document.Clusters[0].Instances[0].Project);
        Assert.Equal("<unknown>", document.Clusters[0].Instances[1].Project);
        Assert.True(document.Clusters[0].Instances[1].IsTestFile);
        Assert.Equal(4, document.Clusters[0].Instances[1].StartLine);
        Assert.Equal(13, document.Clusters[0].Instances[1].EndLine);
        Assert.Equal("M", document.Clusters[0].Instances[0].Member);
        Assert.Equal("h", document.Clusters[0].Instances[0].Hash);

        Assert.Equal("critical", document.Summary.Label);
        Assert.Equal("filesystem", document.Summary.DiscoveryMode);
        Assert.Equal(5, document.Summary.DiscoveredFiles);
        Assert.Equal(3, document.Summary.ExcludedFiles);
        Assert.Equal(2, document.Summary.TotalFiles);
        Assert.Equal(1, document.Summary.TotalClusters);
        Assert.Equal(20, document.Summary.TotalDuplicateLines);
        Assert.Equal(80, document.Summary.TotalLines);
        Assert.Equal(25.0, document.Summary.DuplicationPercentage);

        Assert.Equal("/repo/a.cs", document.FileScores[0].File);
        Assert.Equal("Alpha", document.FileScores[0].Project);
        Assert.Equal(10, document.FileScores[0].DuplicateLines);
        Assert.Equal(40, document.FileScores[0].TotalLines);
        Assert.Equal(25.0, document.FileScores[0].Percentage);
        Assert.Equal(1, document.FileScores[0].ClusterCount);
        Assert.Equal(2, document.FileScores[0].WidestClusterSpread);
        Assert.True(document.FileScores[1].IsTestFile);

        Assert.Equal("Alpha", document.ProjectScores[0].Project);
        Assert.Equal(10, document.ProjectScores[0].DuplicateLines);
        Assert.Equal(40, document.ProjectScores[0].TotalLines);
        Assert.Equal(25.0, document.ProjectScores[0].Percentage);
    }
}
