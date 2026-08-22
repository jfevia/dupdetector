using DupDetector.Cli.CommandLine;
using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using DupDetector.Core.Model.Reporting;
using DupDetector.Reporting;
using DupDetector.Sources;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     One test per defect reproduced during the audit of the previous implementation.
/// </summary>
public class AuditRegressionTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnAncestorDirectoryNamedTestDoesNotInfectTheTree()
    {
        Assert.False(TestFileClassifier.IsTestFile("src/Service.cs", ProjectIdentities.Named("MyApp")));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnInaccessibleSubdirectoryDoesNotAbortTheScan()
    {
        using var workspace = new ProductionWorkspace();
        var denied = Path.Combine(workspace.Root, "denied");
        Directory.CreateDirectory(denied);
        File.WriteAllText(Path.Combine(denied, "Hidden.cs"), "class Hidden { }");

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--format", "json"]);
        var code = run.Code;
        var report = run.Report;

        Assert.Equal(ExitCode.Success, code);
        Assert.NotEmpty(report);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnUnknownFlagFailsTheRun()
    {
        Assert.Equal(ExitCode.UsageError, CliRunner.Run(["./src", "--unknown-flag"]).Code);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnUnknownFormatIsRejectedRatherThanFallingBackToYaml()
    {
        var run = CliRunner.Run(["./src", "--format", "xml"]);
        var code = run.Code;
        var report = run.Report;

        Assert.Equal(ExitCode.UsageError, code);
        Assert.Empty(report);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EmptyCollectionsAgreeAcrossFormats()
    {
        var summary = new ReportSummary
        {
            TotalFiles = 0,
            TotalClusters = 0,
            TotalDuplicateLines = 0,
            TotalLines = 0,
            DuplicationPercentage = 0.0,
            Discovery = DiscoveryStats.Empty
        };

        var empty = new DetectionReport
        {
            Summary = summary,
            Clusters = [],
            FileScores = [],
            ProjectScores = [],
        };

        var yamlReportWriter = new YamlReportWriter();
        Assert.Contains("clusters: []", yamlReportWriter.Write(empty), StringComparison.Ordinal);

        var jsonReportWriter = new JsonReportWriter();
        using var document = JsonDocument.Parse(jsonReportWriter.Write(empty));
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("clusters").ValueKind);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryReportedClusterIsInternallyCohesive()
    {
        using var workspace = new ProductionWorkspace();

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--format", "json", "--similarity", "0.4"]);
        var report = run.Report;

        using var document = JsonDocument.Parse(report);
        Assert.All(
            document.RootElement.GetProperty("clusters").EnumerateArray(),
            cluster => Assert.True(cluster.GetProperty("isCohesive").GetBoolean()));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ExcludingTestFilesAlsoExcludesThemFromTheSummary()
    {
        using var workspace = new ProductionWorkspace();

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--format", "json"]);
        var all = run.Report;
        var run2 = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--format", "json", "--exclude-test-files"]);
        var production = run2.Report;

        using var withTests = JsonDocument.Parse(all);
        using var withoutTests = JsonDocument.Parse(production);

        Assert.True(
            withoutTests.RootElement.GetProperty("summary").GetProperty("totalFiles").GetInt32() <
            withTests.RootElement.GetProperty("summary").GetProperty("totalFiles").GetInt32());

        Assert.DoesNotContain(
            withoutTests.RootElement.GetProperty("fileScores").EnumerateArray(),
            score => score.GetProperty("isTestFile").GetBoolean());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MarkerMentionedInTheBodyDoesNotExcludeTheFile()
    {
        var lines = new List<string>(61);
        for (var index = 0; index < 60; index++)
        {
            lines.Add("// filler");
        }

        lines.Add("if (text.Contains(\"[GeneratedCode\")) { }");
        var body = string.Join('\n', lines);

        Assert.False(GeneratedFileDetector.IsGenerated("Loader.cs", body));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="source"></param>
    [Theory]
    [InlineData("class C { void M() { var s = \"\\e[0m\"; } }")]
    [InlineData("static class E { extension(string s) { public bool IsLong => s.Length > 10; } }")]
    public void ModernLanguageFeaturesAreParsedRatherThanSilentlyDropped(string source)
    {
        Assert.Null(SourceParser.DescribeParseFailures(SourceParser.Parse(source, "x.cs"), "x.cs"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NonPositiveMinimumIsRejectedRatherThanCrashing()
    {
        Assert.NotNull(ArgumentParser.Parse(["./src", "--min-lines", "0"], "1").Error);
        Assert.Throws<ArgumentOutOfRangeException>(BuildInvalidSettings);

        static void BuildInvalidSettings()
        {
            var settings = new DetectionSettings
            {
                MinLines = 0
            };

            Assert.NotNull(settings);
        }
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OverNormalizationNoLongerFusesUnrelatedTypes()
    {
        Assert.NotEqual(
            CliFixtures.Hash("class C { WidgetResult Process(WidgetInput input) { var r = new WidgetResult(); r.Name = input.Name; return r; } }"),
            CliFixtures.Hash("class C { GadgetResult Handle(GadgetInput input) { var g = new GadgetResult(); g.Name = input.Name; return g; } }"));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="relativePath"></param>
    [Theory]
    [InlineData("src/Latest.cs")]
    [InlineData("src/Contest.cs")]
    [InlineData("src/Greatest.cs")]
    public void ProductionFilesEndingInTestAreNotTestFiles(string relativePath)
    {
        Assert.False(TestFileClassifier.IsTestFile(relativePath, ProjectIdentities.Named("App")));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TestCopyDoesNotClearProductionDuplication()
    {
        using var workspace = new ProductionWorkspace();

        var run = CliRunner.Run([workspace.Root, "--min-project-spread", "1", "--min-prod-lines", "1", "--format", "json"]);
        var report = run.Report;

        using var document = JsonDocument.Parse(report);
        var clusters = document.RootElement.GetProperty("clusters");
        Assert.True(clusters.GetArrayLength() > 0);
        Assert.Contains(
            clusters.EnumerateArray(),
            cluster => cluster.GetProperty("isProductionDuplicate").GetBoolean());
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="pattern"></param>
    [Theory]
    [InlineData("src/**", "C:/repo/src/Foo.cs")]
    [InlineData("**", "C:/repo/src/Foo.cs")]
    [InlineData("**/obj/**", "C:/repo/obj/Foo.cs")]
    public void TrailingGlobStarsMatchWhatTheyClaimTo(string pattern, string path)
    {
        Assert.True(GlobPatterns.Parse(pattern).IsMatch(path));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="text"></param>
    [Theory]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\nb\nc\n", 3)]
    [InlineData("", 0)]
    public void TrailingNewlinesDoNotInflateLineCounts(string text, int expected)
    {
        Assert.Equal(expected, LineCounter.Count(text));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void YamlNumbersAreCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var summary = new ReportSummary
            {
                TotalFiles = 1,
                TotalClusters = 0,
                TotalDuplicateLines = 1,
                TotalLines = 4,
                DuplicationPercentage = 25.5,
                Discovery = DiscoveryStats.Empty
            };

            var report = new DetectionReport
            {
                Summary = summary,
                Clusters = [],
                FileScores = [],
                ProjectScores = [],
            };

            var yamlReportWriter = new YamlReportWriter();
            Assert.Contains("25.5", yamlReportWriter.Write(report), StringComparison.Ordinal);
            Assert.DoesNotContain("25,5", yamlReportWriter.Write(report), StringComparison.Ordinal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Two production projects sharing a method, plus a test project copying it.
    /// </summary>
    private sealed class ProductionWorkspace : IDisposable
    {
        private const string Duplicated = """
            namespace Sample;

            public class Calculator
            {
                public int Total(Order order)
                {
                    var running = order.Price;
                    var adjusted = running;
                    var doubled = adjusted;
                    var final = doubled;
                    return final;
                }
            }
            """;

        public string Root { get; }

        public ProductionWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "dupdetector-reg-" + Guid.NewGuid().ToString("N"));

            var app = new ProjectFile("App", "AppCalc.cs");
            var lib = new ProjectFile("Lib", "LibCalc.cs");
            var tests = new ProjectFile("App.Tests", "CalcTests.cs");
            ProjectFile[] files = [app, lib, tests];

            foreach (var entry in files)
            {
                var directory = Path.Combine(Root, entry.Project);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, entry.Project + ".csproj"), "<Project />");
                var encoding = new UTF8Encoding(false);
                File.WriteAllText(Path.Combine(directory, entry.File), Duplicated, encoding);
            }
        }

        public void Dispose()
        {
            _ = CanTryDelete();
        }

        private bool CanTryDelete()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
