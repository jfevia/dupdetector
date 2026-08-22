using DupDetector.Core.Model;

using DupDetector.Reporting.Sarif.Model;

namespace DupDetector.Reporting.Sarif;

/// <summary>
///     Projects clusters into SARIF results.
/// </summary>
public static class SarifResults
{
    /// <summary>
    ///     Projects one cluster into a SARIF result.
    /// </summary>
    /// <param name="cluster">The cluster to project.</param>
    /// <returns>The SARIF result.</returns>
    public static SarifResult ToResult(DuplicateCluster cluster)
    {
        var first = cluster.Instances[0];
        var related = new List<SarifLocation>();
        for (var index = 1; index < cluster.Instances.Count; index++)
        {
            related.Add(Location(cluster.Instances[index]));
        }

        var message = new SarifText
        {
            Text =
                $"'{first.MemberName}' is duplicated {cluster.Metrics.Occurrences} times across " +
                $"{cluster.Metrics.FileSpread} file(s); removing the copies saves " +
                $"{cluster.Metrics.RemovableLines} lines.",
        };

        var partialFingerprints = new SarifFingerprints
        {
            DupDetectorClusterId = cluster.Id
        };
        var result = new SarifResult
        {
            RuleId = "DUP001",
            Level = Level(cluster),
            Message = message,
            PartialFingerprints = partialFingerprints,
            Locations = [Location(first)],
            RelatedLocations = related,
        };

        return result;
    }

    /// <summary>
    ///     Severity tracks reach, because code copied across projects costs more to leave.
    /// </summary>
    /// <param name="cluster">The cluster to rate.</param>
    /// <returns>The SARIF level.</returns>
    private static string Level(DuplicateCluster cluster)
    {
        return cluster switch
        {
            { IsProductionDuplicate: true } => "warning",
            { Metrics.FileSpread: >= 5 } => "warning",
            _ => "note",
        };
    }

    private static SarifLocation Location(CodeInstance instance)
    {
        var artifactLocation = new SarifArtifactLocation
        {
            Uri = SarifUris.ToUri(instance.FilePath)
        };
        var region = new SarifRegion
        {
            StartLine = instance.Lines.Start,
            EndLine = instance.Lines.End
        };
        var physicalLocation = new SarifPhysicalLocation
        {
            ArtifactLocation = artifactLocation,
            Region = region,
        };

        var logicalLocation = new SarifLogicalLocation
        {
            FullyQualifiedName = instance.MemberName
        };
        var location = new SarifLocation
        {
            PhysicalLocation = physicalLocation,
            LogicalLocations = [logicalLocation],
        };

        return location;
    }
}
