using Xunit;

using YamlDotNet.Serialization;

using YamlDotNet.Serialization.NamingConventions;

namespace DupDetector.Reporting.Tests;

/// <summary>
///     Helpers for <see cref="YamlReportWriterTests" />.
/// </summary>
public static class YamlFixtures
{
    private static readonly IDeserializer Reader;

    static YamlFixtures()
    {
        var deserializerBuilder = new DeserializerBuilder();
        Reader = deserializerBuilder
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    ///     One cluster from a parsed report.
    /// </summary>
    /// <returns></returns>
    /// <param name="parsed"></param>
    /// <param name="index"></param>
    public static IDictionary<object, object> Cluster(Dictionary<object, object> parsed, int index)
    {
        var clusters = Assert.IsType<IList<object>>(parsed["clusters"], exactMatch: false);
        return Assert.IsType<IDictionary<object, object>>(clusters[index], exactMatch: false);
    }

    /// <summary>
    ///     Parses YAML into a dictionary.
    /// </summary>
    /// <returns></returns>
    /// <param name="yaml"></param>
    public static Dictionary<object, object> Parse(string yaml)
    {
        return Reader.Deserialize<Dictionary<object, object>>(yaml);
    }
}
