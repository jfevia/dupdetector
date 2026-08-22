namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     Maps a 0-100 percentage onto a <see cref="ScoreLabel"/>.
/// </summary>
public static class ScoreLabels
{
    /// <summary>
    ///     Maps a percentage onto its severity band.
    /// </summary>
    /// <param name="percentage">The duplication percentage.</param>
    /// <returns>The band the percentage falls into.</returns>
    public static ScoreLabel For(double percentage)
    {
        return percentage switch
        {
            < 3 => ScoreLabel.Low,
            < 10 => ScoreLabel.Medium,
            < 20 => ScoreLabel.High,
            _ => ScoreLabel.Critical,
        };
    }
}
