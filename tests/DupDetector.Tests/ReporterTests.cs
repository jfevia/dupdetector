using System.Text.Json;
using Xunit;

namespace DupDetector.Tests;

public class ReporterTests
{
    private readonly Reporter _reporter = new();

    private DetectionReport MakeSampleReport()
    {
        return new DetectionReport
        {
            Summary = new ReportSummary
            {
                TotalFiles = 5,
                TotalDuplicates = 2,
                TotalDuplicateLines = 30
            },
            Clusters = new List<DuplicateCluster>
            {
                new DuplicateCluster
                {
                    Id = "dup-abcd1234",
                    Instances = new List<CodeInstance>
                    {
                        new CodeInstance
                        {
                            File = "src/Foo.cs",
                            StartLine = 10,
                            EndLine = 20,
                            Method = "DoWork",
                            Hash = "abcd1234ef567890"
                        },
                        new CodeInstance
                        {
                            File = "src/Bar.cs",
                            StartLine = 50,
                            EndLine = 60,
                            Method = "DoWork",
                            Hash = "abcd1234ef567890"
                        }
                    },
                    Metrics = new ClusterMetrics
                    {
                        Lines = 10,
                        Occurrences = 2,
                        Spread = 2,
                        Score = 0.4
                    },
                    NormalizedSnippet = "void var0 () { }",
                    RawSnippets = new List<string> { "void DoWork() { }", "void DoWork() { }" }
                }
            }
        };
    }

    [Fact]
    public void JsonOutput_IsValidJson()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        // Should not throw
        var doc = JsonDocument.Parse(output);
        Assert.NotNull(doc);
    }

    [Fact]
    public void JsonOutput_MatchesExpectedSchema()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("summary", out var summary));
        Assert.True(summary.TryGetProperty("totalFiles", out _));
        Assert.True(summary.TryGetProperty("totalDuplicates", out _));
        Assert.True(summary.TryGetProperty("totalDuplicateLines", out _));

        Assert.True(root.TryGetProperty("clusters", out var clusters));
        Assert.Equal(JsonValueKind.Array, clusters.ValueKind);
    }

    [Fact]
    public void JsonOutput_UsesCamelCasePropertyNames()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        // camelCase checks
        Assert.Contains("\"totalFiles\"", output);
        Assert.Contains("\"totalDuplicates\"", output);
        Assert.Contains("\"totalDuplicateLines\"", output);
        Assert.Contains("\"startLine\"", output);
        Assert.Contains("\"endLine\"", output);
        Assert.Contains("\"normalizedSnippet\"", output);
        Assert.Contains("\"rawSnippets\"", output);

        // PascalCase should NOT appear in the keys
        Assert.DoesNotContain("\"TotalFiles\"", output);
        Assert.DoesNotContain("\"StartLine\"", output);
    }

    [Fact]
    public void JsonOutput_ContainsCorrectValues()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = JsonDocument.Parse(output);
        var summary = doc.RootElement.GetProperty("summary");

        Assert.Equal(5, summary.GetProperty("totalFiles").GetInt32());
        Assert.Equal(2, summary.GetProperty("totalDuplicates").GetInt32());
        Assert.Equal(30, summary.GetProperty("totalDuplicateLines").GetInt32());
    }

    [Fact]
    public void JsonOutput_ClustersHaveCorrectStructure()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = JsonDocument.Parse(output);
        var cluster = doc.RootElement.GetProperty("clusters")[0];

        Assert.Equal("dup-abcd1234", cluster.GetProperty("id").GetString());
        Assert.True(cluster.TryGetProperty("instances", out var instances));
        Assert.Equal(2, instances.GetArrayLength());
        Assert.True(cluster.TryGetProperty("metrics", out var metrics));
        Assert.True(metrics.TryGetProperty("score", out _));
        Assert.True(cluster.TryGetProperty("normalizedSnippet", out _));
        Assert.True(cluster.TryGetProperty("rawSnippets", out _));
    }

    [Fact]
    public void YamlOutput_ContainsSummaryFields()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "yaml");

        Assert.Contains("totalFiles:", output);
        Assert.Contains("totalDuplicates:", output);
        Assert.Contains("clusters:", output);
    }
}
