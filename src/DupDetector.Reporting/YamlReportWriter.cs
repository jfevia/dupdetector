using DupDetector.Core.Model.Reporting;
using DupDetector.Reporting.Documents;

using YamlDotNet.Serialization;

using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting;

/// <summary>
///     Writes the report as YAML.
/// </summary>
public sealed class YamlReportWriter : IReportWriter
{
    private static readonly ISerializer Serializer;
    private readonly bool _isIncludeRawSnippets;

    /// <summary>
    ///     
    /// </summary>
    public bool IsIncludeRawSnippets
    {
        get
        {
            return _isIncludeRawSnippets;
        }
    }

    static YamlReportWriter()
    {
        var serializerBuilder = new SerializerBuilder();
        Serializer = serializerBuilder.WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithQuotingNecessaryStrings()
        .Build();
    }

    /// <summary>
    ///     
    /// </summary>
    public YamlReportWriter()
        : this(true)
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="includeRawSnippets"></param>
    public YamlReportWriter(bool includeRawSnippets)
    {
        _isIncludeRawSnippets = includeRawSnippets;
    }

    /// <summary>
    ///     
    /// </summary>
    public ReportFormat Format
    {
        get
        {
            return ReportFormat.Yaml;
        }
    }

    /// <summary>
    ///     
    /// </summary>
    public MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    public string Write(DetectionReport report)
    {
        return Serializer.Serialize(ReportDocuments.From(report, _isIncludeRawSnippets, Metadata));
    }
}
