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

    [Fact]
    public void JsonOutput_FileScores_IncludeIsTestFileField()
    {
        var report = MakeSampleReport();
        report.FileScores = new List<FileScore>
        {
            new FileScore { File = "tests/Foo/BarTests.cs", DuplicateLines = 5, TotalLines = 20, Score = 25.0, IsTestFile = true },
            new FileScore { File = "src/Core/Baz.cs", DuplicateLines = 2, TotalLines = 30, Score = 6.67, IsTestFile = false }
        };
        var output = _reporter.Render(report, "json");

        Assert.Contains("isTestFile", output);
    }

    [Fact]
    public void YamlOutput_FileScores_IncludeIsTestFileField()
    {
        var report = MakeSampleReport();
        report.FileScores = new List<FileScore>
        {
            new FileScore { File = "tests/Foo/BarTests.cs", DuplicateLines = 5, TotalLines = 20, Score = 25.0, IsTestFile = true }
        };
        var output = _reporter.Render(report, "yaml");

        Assert.Contains("isTestFile:", output);
    }
}

// Tests for new features

public class ReporterHtmlTests
{
    private readonly Reporter _reporter = new();

    private DetectionReport MakeSampleReport()
    {
        return new DetectionReport
        {
            Summary = new ReportSummary
            {
                TotalFiles = 3,
                TotalDuplicates = 1,
                TotalDuplicateLines = 20,
                DuplicationScore = 25.5,
                ScoreLabel = "medium"
            },
            Clusters = new List<DuplicateCluster>
            {
                new DuplicateCluster
                {
                    Id = "dup-html0001",
                    Instances = new List<CodeInstance>
                    {
                        new CodeInstance { File = "A.cs", StartLine = 1, EndLine = 10, Method = "M", Hash = "aabb" },
                        new CodeInstance { File = "B.cs", StartLine = 5, EndLine = 14, Method = "M", Hash = "aabb" }
                    },
                    Metrics = new ClusterMetrics { Lines = 10, Occurrences = 2, Spread = 2, Score = 0.4, DuplicationScore = 1.6 },
                    NormalizedSnippet = "void var0() { }",
                    RawSnippets = new List<string> { "void M() { }", "void M() { }" }
                }
            },
            FileScores = new List<FileScore>
            {
                new FileScore { File = "A.cs", DuplicateLines = 10, TotalLines = 50, Score = 20.0 },
                new FileScore { File = "B.cs", DuplicateLines = 10, TotalLines = 100, Score = 10.0 }
            },
            ProjectScores = new List<ProjectScore>
            {
                new ProjectScore { Project = "src", DuplicateLines = 20, TotalLines = 150, Score = 13.3 }
            }
        };
    }

    [Fact]
    public void HtmlOutput_IsValidHtml()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "html");

        Assert.StartsWith("<!DOCTYPE html>", output, StringComparison.Ordinal);
        Assert.Contains("</html>", output, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlOutput_ContainsSummaryValues()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "html");

        Assert.Contains("25.5", output, StringComparison.Ordinal);
        Assert.Contains("Medium", output, StringComparison.Ordinal);
        Assert.Contains("3", output, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlOutput_ContainsClustersJson()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "html");

        Assert.Contains("dup-html0001", output, StringComparison.Ordinal);
        Assert.Contains("duplicationScore", output, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlOutput_ContainsInteractiveScript()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "html");

        Assert.Contains("<script>", output, StringComparison.Ordinal);
        Assert.Contains("refresh2", output, StringComparison.Ordinal);
        Assert.Contains("sort2", output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonOutput_ContainsDuplicationScoreInSummary()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = System.Text.Json.JsonDocument.Parse(output);
        var summary = doc.RootElement.GetProperty("summary");

        Assert.True(summary.TryGetProperty("duplicationScore", out var ds));
        Assert.Equal(25.5, ds.GetDouble(), 1);
        Assert.True(summary.TryGetProperty("scoreLabel", out var sl));
        Assert.Equal("medium", sl.GetString());
    }

    [Fact]
    public void JsonOutput_ContainsFileAndProjectScores()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = System.Text.Json.JsonDocument.Parse(output);
        Assert.True(doc.RootElement.TryGetProperty("fileScores", out var fs));
        Assert.Equal(2, fs.GetArrayLength());
        Assert.True(doc.RootElement.TryGetProperty("projectScores", out var ps));
        Assert.Equal(1, ps.GetArrayLength());
    }

    [Fact]
    public void ClusterMetrics_ContainsDuplicationScore()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "json");

        var doc = System.Text.Json.JsonDocument.Parse(output);
        var metrics = doc.RootElement.GetProperty("clusters")[0].GetProperty("metrics");
        Assert.True(metrics.TryGetProperty("duplicationScore", out _));
    }

    [Fact]
    public void YamlOutput_ContainsDuplicationScore()
    {
        var report = MakeSampleReport();
        var output = _reporter.Render(report, "yaml");

        Assert.Contains("duplicationScore:", output, StringComparison.Ordinal);
        Assert.Contains("scoreLabel:", output, StringComparison.Ordinal);
        Assert.Contains("fileScores:", output, StringComparison.Ordinal);
        Assert.Contains("projectScores:", output, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlOutput_ContainsTestFileBadge_ForTestFiles()
    {
        var report = MakeSampleReport();
        report.FileScores = new List<FileScore>
        {
            new FileScore { File = "tests/FooTests.cs", DuplicateLines = 10, TotalLines = 40, Score = 25.0, IsTestFile = true },
            new FileScore { File = "src/Foo.cs", DuplicateLines = 5, TotalLines = 40, Score = 12.5, IsTestFile = false }
        };

        var output = _reporter.Render(report, "html");

        // The "test" badge CSS class must be present in the HTML
        Assert.Contains("tag tf", output, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonOutput_FileScores_HaveIsTestFileField()
    {
        var report = MakeSampleReport();
        report.FileScores = new List<FileScore>
        {
            new FileScore { File = "tests/FooTests.cs", DuplicateLines = 10, TotalLines = 40, Score = 25.0, IsTestFile = true }
        };
        var output = _reporter.Render(report, "json");

        var doc = System.Text.Json.JsonDocument.Parse(output);
        var fileScore = doc.RootElement.GetProperty("fileScores")[0];
        Assert.True(fileScore.TryGetProperty("isTestFile", out var itf));
        Assert.True(itf.GetBoolean());
    }
}

public class SlnxParserTests
{
    [Fact]
    public void SlnxFile_ProjectPathsAreExtracted()
    {
        // Write a minimal .slnx to a temp location and validate parsing logic
        var slnxContent = """
            <Solution>
              <Folder Name="/src/">
                <Project Path="src/MyProject/MyProject.csproj" />
              </Folder>
              <Folder Name="/tests/">
                <Project Path="tests/MyProject.Tests/MyProject.Tests.csproj" />
              </Folder>
            </Solution>
            """;

        var tmpDir = Path.Combine(Path.GetTempPath(), "slnx-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var slnxPath = Path.Combine(tmpDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the XML
            var xml = System.Xml.Linq.XDocument.Load(slnxPath);
            var projectPaths = xml.Descendants("Project")
                .Select(e => e.Attribute("Path")?.Value)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            Assert.Equal(2, projectPaths.Count);
            Assert.Contains("src/MyProject/MyProject.csproj", projectPaths);
            Assert.Contains("tests/MyProject.Tests/MyProject.Tests.csproj", projectPaths);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void SlnxFile_NestedFolders_AllProjectsFound()
    {
        var slnxContent = """
            <Solution>
              <Folder Name="/code/">
                <Folder Name="/src/">
                  <Project Path="ProjectA/ProjectA.csproj" />
                  <Project Path="ProjectB/ProjectB.csproj" />
                </Folder>
              </Folder>
            </Solution>
            """;

        var xml = System.Xml.Linq.XDocument.Parse(slnxContent);
        var paths = xml.Descendants("Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        Assert.Equal(2, paths.Count);
    }
}

public class ScoringTests
{
    private readonly DuplicateDetector _detector = new();
    private readonly CodeNormalizer _normalizer = new();

    private CodeBlock MakeBlock(string code, string file = "test.cs", int start = 1, int end = 10)
    {
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        return new CodeBlock(file, start, end, "M", hash, normalized, code, end - start + 1);
    }

    [Fact]
    public void DuplicationScore_IsNormalized_BetweenZeroAndHundred()
    {
        var code = """
            void DoWork() {
                var x = 1;
                var y = 2;
                var z = x + y;
                Console.WriteLine(z);
            }
            """;

        var b1 = MakeBlock(code, "f1.cs", 1, 7);
        var b2 = MakeBlock(code, "f2.cs", 1, 7);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.85);
        Assert.Single(clusters);

        var ds = clusters[0].Metrics.DuplicationScore;
        Assert.True(ds >= 0 && ds <= 100, $"DuplicationScore {ds} is out of range 0-100");
    }

    [Fact]
    public void DuplicationScore_IncreasesWithMoreOccurrences()
    {
        var code = """
            void DoWork() {
                var x = 1;
                var y = 2;
                var z = x + y;
                Console.WriteLine(z);
            }
            """;

        var b1 = MakeBlock(code, "f1.cs", 1, 7);
        var b2 = MakeBlock(code, "f2.cs", 1, 7);
        var b3 = MakeBlock(code, "f3.cs", 1, 7);
        var b4 = MakeBlock(code, "f4.cs", 1, 7);

        var clusters2 = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.85);
        var clusters4 = _detector.Detect(new List<CodeBlock> { b1, b2, b3, b4 }, 0.85);

        Assert.Single(clusters2);
        Assert.Single(clusters4);

        var score2 = clusters2[0].Metrics.DuplicationScore;
        var score4 = clusters4[0].Metrics.DuplicationScore;
        Assert.True(score4 >= score2, $"Score with 4 occurrences ({score4}) should be >= score with 2 ({score2})");
    }

    [Fact]
    public void SolutionScore_WithOverlappingClusters_DoesNotExceed100()
    {
        // Simulate a file where two clusters cover overlapping line ranges.
        // Even if the additive count would exceed totalLines, the unique-line
        // count must stay within bounds (score ≤ 100%).
        var fileIntervals = new List<(int, int)>
        {
            (1, 50),  // cluster A covers half the file
            (25, 80), // cluster B overlaps with A and extends further
        };
        var unique = LineCountHelper.CountUniqueLines(fileIntervals);
        var totalLines = 100;
        var score = Math.Min(100.0, unique * 100.0 / totalLines);

        // Merged [1,80] = 80 unique lines
        Assert.Equal(80, unique);
        Assert.Equal(80.0, score, precision: 1);
        Assert.True(score <= 100.0);
    }
}

