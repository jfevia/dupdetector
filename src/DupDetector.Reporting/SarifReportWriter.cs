using System.Text.Json;
using DupDetector.Core.Model;

namespace DupDetector.Reporting;

/// <summary>
/// Writes the report as SARIF 2.1.0.
/// </summary>
// One result per cluster: the first instance is the location and the rest are related locations.
public sealed class SarifReportWriter : IReportWriter
{
    public ReportFormat Format => ReportFormat.Sarif;

    public MetadataDocument? Metadata { get; init; }

    public string Write(DetectionReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var log = new
        {
            schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "DupDetector",
                            informationUri = "https://github.com/jfevia/dupdetector",
                            version = Metadata?.ToolVersion ?? "0.0.0",
                            rules = new[]
                            {
                                new
                                {
                                    id = "DUP001",
                                    name = "DuplicatedCode",
                                    shortDescription = new { text = "Duplicated code" },
                                    fullDescription = new
                                    {
                                        text = "This block is structurally identical to code elsewhere in the solution.",
                                    },
                                    defaultConfiguration = new { level = "note" },
                                    helpUri = "https://github.com/jfevia/dupdetector/blob/main/docs/scoring.md",
                                },
                            },
                        },
                    },
                    results = report.Clusters.Select(ToResult).ToArray(),
                    invocations = new[]
                    {
                        new
                        {
                            executionSuccessful = true,
                            commandLine = Metadata?.CommandLine,
                            startTimeUtc = Metadata?.GeneratedAtUtc,
                            workingDirectory = Metadata is null ? null : new { uri = Uri(Metadata.TargetPath) },
                            properties = Settings(report),
                        },
                    },
                    properties = Properties(report),
                },
            },
        };

        // "$schema" cannot be written as a C# identifier, so it is patched into the serialized form.
        return JsonSerializer.Serialize(log, JsonReportWriter.Standalone)
            .Replace("\"schema\":", "\"$schema\":", StringComparison.Ordinal);
    }

    /// <summary>
    /// The thresholds the run applied, so a consumer can tell a clean report from a narrow one.
    /// </summary>
    private static object? Settings(DetectionReport report) => report.Scope is not { } scope ? null : new
    {
        minLines = scope.Settings.MinLines,
        minTypeLines = scope.Settings.MinTypeLines,
        minFileSpread = scope.Settings.MinFileSpread,
        minProjectSpread = scope.Settings.MinProjectSpread,
        maxFileSpread = scope.Settings.MaxFileSpread,
        maxOccurrences = scope.Settings.MaxOccurrences,
        similarity = scope.Settings.Similarity,
        kinds = scope.Settings.Kinds.ToString().ToLowerInvariant(),
        excludeTestFiles = scope.Settings.ExcludeTestFiles,
    };

    /// <summary>Run totals and what the thresholds withheld.</summary>
    private static object Properties(DetectionReport report) => new
    {
        duplicationPercentage = report.Summary.DuplicationPercentage,
        codeDuplicationPercentage = report.Summary.CodeDuplicationPercentage,
        label = report.Summary.Label.ToString().ToLowerInvariant(),
        totalClusters = report.Summary.TotalClusters,
        suppressedClusters = report.Scope?.Suppressed.Total ?? 0,
        limitations = report.Scope?.Limitations,
    };

    private static object ToResult(DuplicateCluster cluster)
    {
        var first = cluster.Instances[0];

        return new
        {
            ruleId = "DUP001",
            level = Level(cluster),
            message = new
            {
                text =
                    $"'{first.MemberName}' is duplicated {cluster.Metrics.Occurrences} times across " +
                    $"{cluster.Metrics.FileSpread} file(s); removing the copies saves " +
                    $"{cluster.Metrics.RemovableLines} lines.",
            },
            partialFingerprints = new { dupDetectorClusterId = cluster.Id },
            locations = new[] { Location(first) },
            relatedLocations = cluster.Instances.Skip(1).Select(Location).ToArray(),
        };
    }

    /// <summary>
    /// Severity tracks reach, not size: code copied across projects is the costlier kind to leave.
    /// </summary>
    private static string Level(DuplicateCluster cluster) => cluster switch
    {
        { IsProductionDuplicate: true } => "warning",
        { Metrics.FileSpread: >= 5 } => "warning",
        _ => "note",
    };

    private static object Location(CodeInstance instance) => new
    {
        physicalLocation = new
        {
            artifactLocation = new { uri = Uri(instance.FilePath) },
            region = new { startLine = instance.Lines.Start, endLine = instance.Lines.End },
        },
        logicalLocations = new[] { new { fullyQualifiedName = instance.MemberName } },
    };

    /// <summary>
    /// SARIF accepts a relative URI, so a path that is not rooted is passed through rather than
    /// failing the whole report.
    /// </summary>
    private static string Uri(string path) =>
        System.Uri.TryCreate(path, UriKind.Absolute, out var absolute) ? absolute.AbsoluteUri : path.Replace('\\', '/');
}
