using DupDetector.Core.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting;

/// <summary>
/// Writes the report as YAML.
/// </summary>
/// <remarks>
/// Serialization is delegated to YamlDotNet rather than hand-rolled, which is what makes quoting,
/// culture-invariant numbers and empty sequences correct by construction instead of by inspection.
/// </remarks>
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
