namespace DupDetector.Core.Model;

/// <summary>
/// Severity band for a percentage duplication score.
/// </summary>
public enum ScoreLabel
{
    Low,
    Medium,
    High,
    Critical,
}

/// <summary>
/// Maps a 0-100 percentage onto a <see cref="ScoreLabel"/>.
/// </summary>
/// <remarks>
/// Aligned with the SonarQube "Sonar way" gate, which fails at 3% duplicated lines. Wider bands
/// would let a codebase read <c>Low</c> here while failing the gate everywhere else.
/// </remarks>
public static class ScoreLabels
{
    public static ScoreLabel For(double percentage) => percentage switch
    {
        < 3 => ScoreLabel.Low,
        < 10 => ScoreLabel.Medium,
        < 20 => ScoreLabel.High,
        _ => ScoreLabel.Critical,
    };
}
