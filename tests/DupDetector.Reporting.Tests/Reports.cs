using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     Report fixtures shared by the reporting tests.
/// </summary>
public static class Reports
{
    /// <summary>
    ///     A report with nothing in it.
    /// </summary>
    /// <returns></returns>
    public static DetectionReport Empty()
    {
        var reportSummary = new ReportSummary
        {
            TotalFiles = 0,
            TotalClusters = 0,
            TotalDuplicateLines = 0,
            TotalLines = 0,
            DuplicationPercentage = 0.0,
            Discovery = DiscoveryStats.Empty,
        };

        var report = new DetectionReport
        {
            Summary = reportSummary,
            Clusters = [],
            FileScores = [],
            ProjectScores = [],
        };

        return report;
    }

    /// <summary>
    ///     A report with one two-instance cluster, using the default snippets.
    /// </summary>
    /// <returns></returns>
    public static DetectionReport Sample()
    {
        return Sample("public void M() { }", "public void var0 ( ) { }");
    }

    /// <summary>
    ///     A report with one two-instance cluster.
    /// </summary>
    /// <returns></returns>
    /// <param name="rawSnippet"></param>
    /// <param name="normalizedSnippet"></param>
    public static DetectionReport Sample(string rawSnippet, string normalizedSnippet)
    {
        var cluster = SampleCluster(rawSnippet, normalizedSnippet);
        var projectScore = new ProjectScore
        {
            Project = ProjectIdentities.Named("Alpha"),
            DuplicateLines = 10,
            TotalLines = 40,
            Percentage = 25.0,
        };

        var report = new DetectionReport
        {
            Summary = SampleSummary(),
            Clusters = [cluster],
            FileScores = [SampleScore("/repo/a.cs", "Alpha"), SampleScore("/repo/b.cs", null)],
            ProjectScores = [projectScore],
        };

        return report;
    }

    private static DuplicateCluster SampleCluster(string rawSnippet, string normalizedSnippet)
    {
        var firstLines = new LineRange(1, 10);
        var firstLocation = new CodeLocation("/repo/a.cs", ProjectIdentities.Named("Alpha"), false, firstLines);
        var firstInstance = new CodeInstance(firstLocation, "M", "h");
        var secondLines = new LineRange(4, 13);
        var secondLocation = new CodeLocation("/repo/b.cs", ProjectIdentity.Unknown, true, secondLines);
        var secondInstance = new CodeInstance(secondLocation, "M", "h");
        var spread = new ClusterSpread(2, 1, false);
        var metrics = new ClusterMetrics(10, 2, spread);
        var cluster = new DuplicateCluster
        {
            Id = "dup-abc123abc123",
            Instances = [firstInstance, secondInstance],
            Metrics = metrics,
            NormalizedSnippet = normalizedSnippet,
            RawSnippets = [rawSnippet, rawSnippet],
            IsCohesive = true,
            IsProductionDuplicate = true,
        };

        return cluster;
    }

    private static FileScore SampleScore(string path, string? project)
    {
        var identity = project is null ? ProjectIdentity.Unknown : ProjectIdentities.Named(project);
        var score = new FileScore
        {
            Path = path,
            Project = identity,
            DuplicateLines = 10,
            TotalLines = 40,
            Percentage = 25.0,
            IsTestFile = project is null,
            ClusterCount = 1,
            WidestClusterSpread = 2,
        };

        return score;
    }

    private static ReportSummary SampleSummary()
    {
        var discoveryStats = new DiscoveryStats
        {
            Discovered = 5,
            Excluded = 3,
            Mode = DiscoveryMode.FileSystem,
        };

        var reportSummary = new ReportSummary
        {
            TotalFiles = 2,
            TotalClusters = 1,
            TotalDuplicateLines = 20,
            TotalLines = 80,
            DuplicationPercentage = 25.0,
            Discovery = discoveryStats,
        };

        return reportSummary;
    }
}
