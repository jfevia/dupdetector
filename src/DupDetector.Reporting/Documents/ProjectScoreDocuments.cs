using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="ProjectScoreDocument" />.
/// </summary>
public static class ProjectScoreDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="score"></param>
    public static ProjectScoreDocument From(ProjectScore score)
    {
        var value = new ProjectScoreDocument()
        {
            Project = score.Project.ToString(),
            DuplicateLines = score.DuplicateLines,
            TotalLines = score.TotalLines,
            Percentage = score.Percentage,
        };
        return value;
    }
}
