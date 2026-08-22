using DupDetector.Core.Model.Reporting;

using DupDetector.Reporting.Documents;

using DupDetector.Reporting.Sarif.Model;

namespace DupDetector.Reporting.Sarif;

/// <summary>
///     Builds the SARIF document shape.
/// </summary>
public static class SarifLog
{
    /// <summary>
    ///     Builds the whole log for one report.
    /// </summary>
    /// <param name="report">The report to render.</param>
    /// <param name="metadata">The provenance, when the caller has any.</param>
    /// <returns>The object serialized as SARIF.</returns>
    public static SarifDocument Build(DetectionReport report, MetadataDocument? metadata)
    {
        var run = BuildRun(report, metadata);
        var log = new SarifDocument
        {
            Schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            Version = "2.1.0",
            Runs = [run],
        };

        return log;
    }

    private static SarifDriver BuildDriver(MetadataDocument? metadata)
    {
        var shortDescription = new SarifText
        {
            Text = "Duplicated code"
        };
        var fullDescription = new SarifText
        {
            Text = "This block is structurally identical to code elsewhere in the solution.",
        };

        var defaultConfiguration = new SarifConfiguration
        {
            Level = "note"
        };
        var rule = new SarifRule
        {
            Id = "DUP001",
            Name = "DuplicatedCode",
            ShortDescription = shortDescription,
            FullDescription = fullDescription,
            DefaultConfiguration = defaultConfiguration,
            HelpUri = "https://github.com/jfevia/dupdetector/blob/main/docs/scoring.md",
        };

        var driver = new SarifDriver
        {
            Name = "DupDetector",
            InformationUri = "https://github.com/jfevia/dupdetector",
            Version = metadata?.ToolVersion ?? "0.0.0",
            Rules = [rule],
        };

        return driver;
    }

    private static SarifRun BuildRun(DetectionReport report, MetadataDocument? metadata)
    {
        var results = new List<SarifResult>(report.Clusters.Count);
        foreach (var cluster in report.Clusters)
        {
            results.Add(SarifResults.ToResult(cluster));
        }

        SarifArtifactLocation? workingDirectory = null;
        if (metadata is not null)
        {
            workingDirectory = new SarifArtifactLocation
            {
                Uri = SarifUris.ToUri(metadata.TargetPath)
            };
        }

        var invocation = new SarifInvocation
        {
            IsExecutionSuccessful = true,
            CommandLine = metadata?.CommandLine,
            StartTimeUtc = metadata?.GeneratedAtUtc,
            WorkingDirectory = workingDirectory,
            Properties = SarifProperties.Settings(report),
        };

        var tool = new SarifTool
        {
            Driver = BuildDriver(metadata)
        };
        var run = new SarifRun
        {
            Tool = tool,
            Results = results,
            Invocations = [invocation],
            Properties = SarifProperties.Summary(report),
        };

        return run;
    }
}
