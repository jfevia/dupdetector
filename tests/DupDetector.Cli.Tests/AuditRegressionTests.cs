using System.Text;
using System.Text.Json;
using DupDetector.Cli.CommandLine;
using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using DupDetector.Core.Normalization;
using DupDetector.Reporting;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
/// One test per defect reproduced during the audit of the previous implementation.
/// </summary>
// Each test names the behaviour observed before, so a regression is recognised rather than rediscovered.
public class AuditRegressionTests
{
    private static string Hash(string source) =>
        StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Hash;

    private static (ExitCode Code, string Report) Run(params string[] args)
    {
        var sink = new RecordingSink();
        var code = new CliHost(new RecordingLogger(), sink).Run(args, "9.9.9");
        return (code, sink.Report.ToString());
    }

    // Previously: two unrelated mappers over different domain types produced one identical SHA-256
    // and were reported as a single isExact cluster.
    [Fact]
    public void OverNormalizationNoLongerFusesUnrelatedTypes() =>
        Assert.NotEqual(
            Hash("class C { WidgetResult Process(WidgetInput input) { var r = new WidgetResult(); r.Name = input.Name; return r; } }"),
            Hash("class C { GadgetResult Handle(GadgetInput input) { var g = new GadgetResult(); g.Name = input.Name; return g; } }"));

    // Previously: an unparseable construct discarded every member of the file, silently.
    [Theory]
    [InlineData("class C { void M() { var s = \"\\e[0m\"; } }")]
    [InlineData("static class E { extension(string s) { public bool IsLong => s.Length > 10; } }")]
    public void ModernCSharpIsParsedRatherThanSilentlyDropped(string source) =>
        Assert.Null(Sources.SourceParser.DescribeParseFailures(Sources.SourceParser.Parse(source, "x.cs"), "x.cs"));

    // Previously: a file whose body merely mentioned a generated-code marker excluded itself.
    [Fact]
    public void AMarkerMentionedInTheBodyDoesNotExcludeTheFile()
    {
        var body = string.Join('\n', Enumerable.Repeat("// filler", 60).Append("if (text.Contains(\"[GeneratedCode\")) { }"));

        Assert.False(Sources.GeneratedFileDetector.IsGenerated("Loader.cs", body));
    }

    // Previously: 'src/**' compiled to a regex that could never match, so it excluded nothing.
    [Theory]
    [InlineData("src/**", "C:/repo/src/Foo.cs")]
    [InlineData("**", "C:/repo/src/Foo.cs")]
    [InlineData("**/obj/**", "C:/repo/obj/Foo.cs")]
    public void TrailingGlobStarsMatchWhatTheyClaimTo(string pattern, string path) =>
        Assert.True(GlobPattern.Parse(pattern).IsMatch(path));

    // Previously: 'Latest.cs' ended with 'test.cs' and was classified as a test file.
    [Theory]
    [InlineData("src/Latest.cs")]
    [InlineData("src/Contest.cs")]
    [InlineData("src/Greatest.cs")]
    public void ProductionFilesEndingInTestAreNotTestFiles(string relativePath) =>
        Assert.False(TestFileClassifier.IsTestFile(relativePath, ProjectIdentity.Named("App")));

    // Previously: a repository checked out under a directory named 'test' marked every file a test.
    [Fact]
    public void AnAncestorDirectoryNamedTestDoesNotInfectTheTree() =>
        Assert.False(TestFileClassifier.IsTestFile("src/Service.cs", ProjectIdentity.Named("MyApp")));

    // Previously: a trailing newline added a phantom line, inflating every denominator.
    [Theory]
    [InlineData("a\nb\nc", 3)]
    [InlineData("a\nb\nc\n", 3)]
    [InlineData("", 0)]
    public void TrailingNewlinesDoNotInflateLineCounts(string text, int expected) =>
        Assert.Equal(expected, LineCounter.Count(text));

    // Previously: --min-lines 0 reached a Take(0).First() and threw.
    [Fact]
    public void ANonPositiveMinimumIsRejectedRatherThanCrashing()
    {
        Assert.NotNull(ArgumentParser.Parse(["./src", "--min-lines", "0"], "1").Error);
        Assert.Throws<ArgumentOutOfRangeException>(() => new DetectionSettings { MinLines = 0 });
    }

    // Previously: adding a test-file copy cleared isProductionDuplicate on genuine production debt.
    [Fact]
    public void ATestCopyDoesNotClearProductionDuplication()
    {
        using var workspace = new ProductionWorkspace();

        var (_, report) = Run(workspace.Root, "--min-project-spread", "1", "--min-prod-lines", "1", "--format", "json");

        using var document = JsonDocument.Parse(report);
        var clusters = document.RootElement.GetProperty("clusters");
        Assert.True(clusters.GetArrayLength() > 0);
        Assert.Contains(
            clusters.EnumerateArray(),
            cluster => cluster.GetProperty("isProductionDuplicate").GetBoolean());
    }

    // Previously: an unknown --format silently produced YAML.
    [Fact]
    public void AnUnknownFormatIsRejectedRatherThanFallingBackToYaml()
    {
        var (code, report) = Run("./src", "--format", "xml");

        Assert.Equal(ExitCode.UsageError, code);
        Assert.Empty(report);
    }

    // Previously: an unknown flag warned and the run still exited 0.
    [Fact]
    public void AnUnknownFlagFailsTheRun() =>
        Assert.Equal(ExitCode.UsageError, Run("./src", "--unknown-flag").Code);

    // Previously: empty collections rendered as null in YAML but as [] in JSON.
    [Fact]
    public void EmptyCollectionsAgreeAcrossFormats()
    {
        var empty = new DetectionReport(
            new ReportSummary(0, 0, 0, 0, 0.0, DiscoveryStats.Empty),
            [],
            [],
            []);

        Assert.Contains("clusters: []", new YamlReportWriter().Write(empty), StringComparison.Ordinal);

        using var document = JsonDocument.Parse(new JsonReportWriter().Write(empty));
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("clusters").ValueKind);
    }

    // Previously: YAML numbers followed the current culture and emitted '0,26' on de-DE.
    [Fact]
    public void YamlNumbersAreCultureInvariant()
    {
        var original = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            var report = new DetectionReport(
                new ReportSummary(1, 0, 1, 4, 25.5, DiscoveryStats.Empty),
                [],
                [],
                []);

            Assert.Contains("25.5", new YamlReportWriter().Write(report), StringComparison.Ordinal);
            Assert.DoesNotContain("25,5", new YamlReportWriter().Write(report), StringComparison.Ordinal);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = original;
        }
    }

    // Previously: --exclude-test-files hid files from the listings while the summary still counted them.
    [Fact]
    public void ExcludingTestFilesAlsoExcludesThemFromTheSummary()
    {
        using var workspace = new ProductionWorkspace();

        var (_, all) = Run(workspace.Root, "--min-project-spread", "1", "--format", "json");
        var (_, production) = Run(workspace.Root, "--min-project-spread", "1", "--format", "json", "--exclude-test-files");

        using var withTests = JsonDocument.Parse(all);
        using var withoutTests = JsonDocument.Parse(production);

        Assert.True(
            withoutTests.RootElement.GetProperty("summary").GetProperty("totalFiles").GetInt32() <
            withTests.RootElement.GetProperty("summary").GetProperty("totalFiles").GetInt32());

        Assert.DoesNotContain(
            withoutTests.RootElement.GetProperty("fileScores").EnumerateArray(),
            score => score.GetProperty("isTestFile").GetBoolean());
    }

    // Previously: a permission-denied subdirectory aborted the whole scan with exit code 1.
    [Fact]
    public void AnInaccessibleSubdirectoryDoesNotAbortTheScan()
    {
        using var workspace = new ProductionWorkspace();
        var denied = Path.Combine(workspace.Root, "denied");
        Directory.CreateDirectory(denied);
        File.WriteAllText(Path.Combine(denied, "Hidden.cs"), "class Hidden { }");

        var (code, report) = Run(workspace.Root, "--min-project-spread", "1", "--format", "json");

        Assert.Equal(ExitCode.Success, code);
        Assert.NotEmpty(report);
    }

    // Previously: union-find chained blocks that shared no tokens into one cluster.
    [Fact]
    public void EveryReportedClusterIsInternallyCohesive()
    {
        using var workspace = new ProductionWorkspace();

        var (_, report) = Run(workspace.Root, "--min-project-spread", "1", "--format", "json", "--similarity", "0.4");

        using var document = JsonDocument.Parse(report);
        Assert.All(
            document.RootElement.GetProperty("clusters").EnumerateArray(),
            cluster => Assert.True(cluster.GetProperty("isCohesive").GetBoolean()));
    }

    /// <summary>Two production projects sharing a method, plus a test project copying it.</summary>
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

        internal ProductionWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "dupdetector-reg-" + Guid.NewGuid().ToString("N"));

            foreach (var (project, file) in new[] { ("App", "AppCalc.cs"), ("Lib", "LibCalc.cs"), ("App.Tests", "CalcTests.cs") })
            {
                var directory = Path.Combine(Root, project);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, project + ".csproj"), "<Project />");
                File.WriteAllText(Path.Combine(directory, file), Duplicated, new UTF8Encoding(false));
            }
        }

        internal string Root { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A leftover temp directory is not worth failing a test over.
            }
        }
    }
}
