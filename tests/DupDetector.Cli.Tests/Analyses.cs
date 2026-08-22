using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Core.Pipeline;

using DupDetector.TestKit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     Runs the analysis pipeline over in-memory sources.
/// </summary>
public static class Analyses
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="settings"></param>
    /// <param name="sources"></param>
    public static DetectionReport Run(DetectionSettings settings, IReadOnlyList<string> sources)
    {
        var units = new List<SourceUnit>(sources.Count);
        for (var index = 0; index < sources.Count; index++)
        {
            units.Add(Code.Unit(sources[index], $"/repo/P{index}/File{index}.cs", $"Proj{index}"));
        }

        return AnalysisPipeline.Run(units, settings, DiscoveryStats.Empty).Report;
    }
}
