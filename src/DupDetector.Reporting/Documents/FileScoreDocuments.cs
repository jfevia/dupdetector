using DupDetector.Core.Model.Reporting;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="FileScoreDocument" />.
/// </summary>
public static class FileScoreDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="score"></param>
    public static FileScoreDocument From(FileScore score)
    {

        var fileScoreDocument = new FileScoreDocument
        {
            File = score.Path,
            Project = score.Project.ToString(),
            DuplicateLines = score.DuplicateLines,
            TotalLines = score.TotalLines,
            Percentage = score.Percentage,
            IsTestFile = score.IsTestFile,
            ClusterCount = score.ClusterCount,
            WidestClusterSpread = score.WidestClusterSpread,
            CodeLines = score.CodeLines,
            DuplicateCodeLines = score.DuplicateCodeLines,
        };
        return fileScoreDocument;
    }
}
