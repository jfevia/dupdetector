using DupDetector.Core.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting;

/// <summary>
/// Writes the report as YAML.
/// </summary>
// Delegated to YamlDotNet, which makes quoting and culture-invariant numbers correct by construction.
public sealed class YamlReportWriter(bool includeRawSnippets = true) : IReportWriter
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithQuotingNecessaryStrings()
        .Build();

    public ReportFormat Format => ReportFormat.Yaml;

    public bool IncludeRawSnippets => includeRawSnippets;

    public MetadataDocument? Metadata { get; init; }

    public string Write(DetectionReport report) =>
        Serializer.Serialize(ReportDocument.From(report, includeRawSnippets, Metadata));
}
