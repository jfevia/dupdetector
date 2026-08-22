using DupDetector.Core.Detection;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;

namespace DupDetector.Core.Tests.Pipeline;

/// <summary>
///     Helpers for <see cref="AnalysisScopeTests" />.
/// </summary>
public static class ScopeFixtures
{
    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="suppressed"></param>
    public static AnalysisScope Scope(DetectionSettings settings, SuppressionCounts? suppressed)
    {
        var value = new AnalysisScope()
        {
            Settings = settings,
            Suppressed = suppressed ?? SuppressionCounts.Empty
        };
        return value;
    }

    /// <summary>
    ///     A scope that suppressed nothing.
    /// </summary>
    /// <returns></returns>
    /// <param name="settings"></param>
    public static AnalysisScope Scope(DetectionSettings settings)
    {
        return Scope(settings, null);
    }
}
