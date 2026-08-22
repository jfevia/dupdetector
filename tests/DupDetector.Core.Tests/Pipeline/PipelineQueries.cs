using DupDetector.Core.Model.Reporting;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     Helpers for <see cref="AnalysisPipelineTests" />.
/// </summary>
public static class PipelineQueries
{
    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="report"></param>
    public static List<string> ClusterIds(DetectionReport report)
    {
        var ids = new List<string>(report.Clusters.Count);
        foreach (var cluster in report.Clusters)
        {
            ids.Add(cluster.Id);
        }

        return ids;
    }

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="report"></param>
    public static List<string> ScorePaths(DetectionReport report)
    {
        var paths = new List<string>(report.FileScores.Count);
        foreach (var score in report.FileScores)
        {
            paths.Add(score.Path);
        }

        return paths;
    }
}
