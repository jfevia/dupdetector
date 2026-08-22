using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Core.Pipeline;

using DupDetector.Reporting.Documents;

using DupDetector.TestKit;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     Fixtures for the report audit output tests.
/// </summary>
public static class AuditFixtures
{
    private const string Duplicated = """
        internal sealed class Repeated
        {
            public int Compute(int a)
            {
                var total = a;
                total += 1;
                total *= 2;
                return total;
            }
        }
        """;

    /// <summary>
    ///     Provenance with fixed values so output comparisons stay stable.
    /// </summary>
    /// <returns></returns>
    public static MetadataDocument Metadata()
    {
        var metadata = new MetadataDocument()
        {
            ToolVersion = "9.9.9",
            GeneratedAtUtc = "2024-01-01T00:00:00.0000000Z",
            TargetPath = "/repo",
            CommandLine = "/repo",
        };

        return metadata;
    }

    /// <summary>
    ///     A report built from three copies of the same class.
    /// </summary>
    /// <returns></returns>
    public static DetectionReport Report()
    {
        return Report(3, null);
    }

    /// <summary>
    ///     A report built from a given number of copies of the same class.
    /// </summary>
    /// <returns></returns>
    /// <param name="copies"></param>
    public static DetectionReport Report(int copies)
    {
        return Report(copies, null);
    }

    /// <summary>
    ///     A report built from a given number of copies of the same class.
    /// </summary>
    /// <returns></returns>
    /// <param name="copies"></param>
    /// <param name="settings"></param>
    public static DetectionReport Report(int copies, DetectionSettings? settings)
    {
        var units = new List<SourceUnit>(copies);
        for (var index = 0; index < copies; index++)
        {
            units.Add(Code.Unit(Duplicated, $"/repo/P{index}/File{index}.cs", $"Proj{index}"));
        }

        var fallback = new DetectionSettings
        {
            MinLines = 5,
            MinTypeLines = 8,
        };

        return AnalysisPipeline.Run(units, settings ?? fallback, DiscoveryStats.Empty).Report;
    }

    /// <summary>
    ///     Units built from a given number of copies of the same class, all in one project.
    /// </summary>
    /// <returns></returns>
    /// <param name="copies"></param>
    /// <param name="project"></param>
    public static IReadOnlyList<SourceUnit> Units(int copies, string project)
    {
        var units = new List<SourceUnit>(copies);
        for (var index = 0; index < copies; index++)
        {
            units.Add(Code.Unit(Duplicated, $"/repo/F{index}.cs", project));
        }

        return units;
    }
}
