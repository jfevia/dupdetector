using DupDetector.Core.Model.Reporting;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Helpers for <see cref="AggregateScorerTests" />.
/// </summary>
public static class ScoreQueries
{

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="scores"></param>
    /// <param name="name"></param>
    public static ProjectScore ProjectFor(IReadOnlyList<ProjectScore> scores, string name)
    {
        foreach (var score in scores)
        {
            if (score.Project.Name == name)
            {
                return score;
            }
        }

        var invalidOperationException2 = new InvalidOperationException($"No score for {name}.");
        throw invalidOperationException2;
    }

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="scores"></param>
    public static FileScore ScoreFor(IReadOnlyList<FileScore> scores, string path)
    {
        foreach (var score in scores)
        {
            if (score.Path == path)
            {
                return score;
            }
        }

        var invalidOperationException = new InvalidOperationException($"No score for {path}.");
        throw invalidOperationException;
    }
}
