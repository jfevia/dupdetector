using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for Run 7 gap fixes:
/// GAP-K2 — isProductionDuplicate min-lines threshold
/// GAP-Q  — absolute paths in output
/// GAP-R  — scoreFormula/rawScoreFormula in summary
/// GAP-S  — discoveryMode/discoveredFiles/excludedFiles in summary
/// GAP-A2 — --exclude-project-pattern filter
/// </summary>
public class Run7GapTests
{
    private readonly DuplicateDetector _detector = new();
    private readonly CodeNormalizer _normalizer = new();

    private CodeBlock MakeBlock(string code, string file, string projectName = "", int start = 1, int end = 10)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        return new CodeBlock(file, start, end, "M", hash, normalized, code, end - start + 1)
        {
            ProjectName = projectName
        };
    }

    // ──── GAP-K2: IsProductionDuplicate min-lines threshold ───────────────────

    [Fact]
    public void ShortExactDuplicate_BelowThreshold_IsNotProductionDuplicate()
    {
        // 5-line constructor (LineCount=5 < default threshold 10) should NOT be isProductionDuplicate
        var code = """
            void Ctor(ServiceA a, ServiceB b) {
                _a = a;
                _b = b;
            }
            """;
        // Use end=5 so LineCount = end - start + 1 = 5
        var b1 = MakeBlock(code, @"src\ProjectA\ServiceA.cs", "ProjectA", start: 1, end: 5);
        var b2 = MakeBlock(code, @"src\ProjectB\ServiceB.cs", "ProjectB", start: 1, end: 5);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1, minProdDupLines: 10);

        Assert.Single(clusters);
        var c = clusters[0];
        Assert.True(c.IsExact);
        Assert.False(c.IsProductionDuplicate,
            "5-line cluster should not be isProductionDuplicate when minProdDupLines=10");
    }

    [Fact]
    public void LongExactDuplicate_AtThreshold_IsProductionDuplicate()
    {
        // 10-line method exactly at threshold should be isProductionDuplicate
        var code = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                svc.AddSingleton<IDb, Db>();
                svc.AddSingleton<ICache, Cache>();
                svc.AddSingleton<IQueue, Queue>();
                svc.AddSingleton<ILogger, Logger>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"src\ProjectA\Host.cs", "ProjectA", 1, 10);
        var b2 = MakeBlock(code, @"src\ProjectB\Host.cs", "ProjectB", 1, 10);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1, minProdDupLines: 10);

        Assert.Single(clusters);
        var c = clusters[0];
        Assert.True(c.IsExact);
        Assert.True(c.IsProductionDuplicate,
            "10-line cluster exactly at minProdDupLines=10 should be isProductionDuplicate");
    }

    [Fact]
    public void MinProdDupLines_SetToOne_ShortClusterFlagged()
    {
        // With threshold=1, even short clusters can be isProductionDuplicate
        var code = """
            void Ctor(ServiceA a, ServiceB b) {
                _a = a;
                _b = b;
            }
            """;
        var b1 = MakeBlock(code, @"src\ProjectA\A.cs", "ProjectA", start: 1, end: 5);
        var b2 = MakeBlock(code, @"src\ProjectB\B.cs", "ProjectB", start: 1, end: 5);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1, minProdDupLines: 1);

        Assert.Single(clusters);
        Assert.True(clusters[0].IsProductionDuplicate,
            "With minProdDupLines=1, short cluster with non-test files should still be flagged");
    }

    [Fact]
    public void CodeInstance_HasProjectName_SetFromCodeBlock()
    {
        // Verify ProjectName propagates from CodeBlock to CodeInstance
        var code = """
            void Build() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"src\Alpha\Host.cs", "Alpha", 1, 10);
        var b2 = MakeBlock(code, @"src\Beta\Host.cs", "Beta", 1, 10);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1);

        Assert.Single(clusters);
        var inst1 = clusters[0].Instances.Find(i => i.File.Contains("Alpha"));
        var inst2 = clusters[0].Instances.Find(i => i.File.Contains("Beta"));
        Assert.NotNull(inst1);
        Assert.NotNull(inst2);
        Assert.Equal("Alpha", inst1!.ProjectName);
        Assert.Equal("Beta", inst2!.ProjectName);
    }

    // ──── GAP-Q: Absolute paths in output ─────────────────────────────────────

    [Fact]
    public async Task LoadFromDirectory_WithRelativeInput_ReturnsAbsolutePaths()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "abstest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Foo.cs"), "public class Foo { }");
            var options = new DetectionOptions { IncludeGenerated = true };
            var loader = new ProjectLoader(options);
            var docs = loader.LoadFromDirectoryInternal(tmpDir);

            Assert.NotEmpty(docs);
            foreach (var doc in docs)
            {
                Assert.True(Path.IsPathRooted(doc.FilePath),
                    $"Path should be absolute: {doc.FilePath}");
            }
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
        await Task.CompletedTask;
    }

    [Fact]
    public async Task LoadDetailedAsync_NormalizesRelativePath_ReturnsAbsolutePaths()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "abstest2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Bar.cs"), "public class Bar { }");

            var options = new DetectionOptions { IncludeGenerated = true };
            var loader = new ProjectLoader(options);

            var (docs, _) = await loader.LoadDetailedAsync(tmpDir);

            Assert.NotEmpty(docs);
            foreach (var doc in docs)
            {
                Assert.True(Path.IsPathRooted(doc.FilePath),
                    $"Path should be absolute: {doc.FilePath}");
            }
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    // ──── GAP-R: scoreFormula / rawScoreFormula in summary ────────────────────

    [Fact]
    public void ReportSummary_HasScoreFormulaFields()
    {
        var summary = new ReportSummary();
        Assert.False(string.IsNullOrWhiteSpace(summary.ScoreFormula),
            "ScoreFormula should have a default value");
        Assert.False(string.IsNullOrWhiteSpace(summary.RawScoreFormula),
            "RawScoreFormula should have a default value");
    }

    [Fact]
    public void ReportSummary_ScoreFormula_ContainsCappingIndicators()
    {
        var summary = new ReportSummary();
        // Formula should mention the capping parameters
        Assert.Contains("50", summary.ScoreFormula, StringComparison.Ordinal);
        Assert.Contains("25", summary.ScoreFormula, StringComparison.Ordinal);
        Assert.Contains("125", summary.ScoreFormula, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportSummary_ScoreFormulas_AppearsInYamlOutput()
    {
        var report = new DetectionReport
        {
            Summary = new ReportSummary
            {
                TotalFiles = 10,
                TotalDuplicates = 2,
                DuplicationScore = 5.0,
                ScoreLabel = "low"
            }
        };

        var yaml = new Reporter().Render(report, "yaml");
        Assert.Contains("scoreFormula:", yaml, StringComparison.Ordinal);
        Assert.Contains("rawScoreFormula:", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportSummary_ScoreFormulas_AppearsInJsonOutput()
    {
        var report = new DetectionReport
        {
            Summary = new ReportSummary
            {
                TotalFiles = 10,
                TotalDuplicates = 2,
                DuplicationScore = 5.0,
                ScoreLabel = "low"
            }
        };

        var json = new Reporter().Render(report, "json");
        Assert.Contains("scoreFormula", json, StringComparison.Ordinal);
        Assert.Contains("rawScoreFormula", json, StringComparison.Ordinal);
    }

    // ──── GAP-S: discoveredFiles / excludedFiles / discoveryMode ──────────────

    [Fact]
    public async Task LoadDetailedAsync_FilesystemMode_ReturnsStats()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "statstest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "A.cs"), "public class A { }");
            File.WriteAllText(Path.Combine(tmpDir, "B.cs"), "public class B { }");

            var options = new DetectionOptions { IncludeGenerated = true };
            var loader = new ProjectLoader(options);
            var (docs, stats) = await loader.LoadDetailedAsync(tmpDir);

            Assert.Equal("filesystem", stats.DiscoveryMode);
            Assert.Equal(2, stats.DiscoveredFiles);
            Assert.Equal(0, stats.ExcludedFiles);
            Assert.Equal(2, docs.Count);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public async Task LoadDetailedAsync_ExcludedGeneratedFiles_CountedInExcluded()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "gentest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "A.cs"), "public class A { }");
            File.WriteAllText(Path.Combine(tmpDir, "B.g.cs"), "// <auto-generated/>\npublic class B { }");

            var options = new DetectionOptions { IncludeGenerated = false };
            var loader = new ProjectLoader(options);
            var (docs, stats) = await loader.LoadDetailedAsync(tmpDir);

            Assert.Equal("filesystem", stats.DiscoveryMode);
            Assert.Equal(2, stats.DiscoveredFiles);
            Assert.True(stats.ExcludedFiles >= 1, "At least the .g.cs file should be excluded");
            Assert.Single(docs);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void ReportSummary_HasDiscoveryFields()
    {
        var summary = new ReportSummary
        {
            DiscoveredFiles = 100,
            ExcludedFiles = 5,
            DiscoveryMode = "filesystem"
        };

        Assert.Equal(100, summary.DiscoveredFiles);
        Assert.Equal(5, summary.ExcludedFiles);
        Assert.Equal("filesystem", summary.DiscoveryMode);
    }

    [Fact]
    public void ReportSummary_DiscoveryFields_AppearsInYaml()
    {
        var report = new DetectionReport
        {
            Summary = new ReportSummary
            {
                TotalFiles = 100,
                DiscoveredFiles = 106,
                ExcludedFiles = 6,
                DiscoveryMode = "filesystem",
                ScoreLabel = "low"
            }
        };

        var yaml = new Reporter().Render(report, "yaml");
        Assert.Contains("discoveredFiles:", yaml, StringComparison.Ordinal);
        Assert.Contains("excludedFiles:", yaml, StringComparison.Ordinal);
        Assert.Contains("discoveryMode:", yaml, StringComparison.Ordinal);
    }

    // ──── GAP-A2: --exclude-project-pattern ───────────────────────────────────

    [Fact]
    public void ExcludeProjectPattern_AllInstancesInMatchingProject_ClusterRemoved()
    {
        var code = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"tests\Alpha.Architecture.Tests\ArchTest.cs", "Alpha.Architecture.Tests", 1, 10);
        var b2 = MakeBlock(code, @"tests\Beta.Architecture.Tests\ArchTest.cs", "Beta.Architecture.Tests", 1, 10);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1);

        // Simulate --exclude-project-pattern ".Architecture."
        var patterns = new List<string> { ".Architecture." };
        var filtered = clusters
            .Where(c => !c.Instances.All(inst =>
                patterns.Any(p => inst.ProjectName.Contains(p, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        Assert.Empty(filtered);
    }

    [Fact]
    public void ExcludeProjectPattern_MixedProjects_ClusterKept()
    {
        // One instance is in an architecture project, one in production — cluster should NOT be filtered
        var code = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"tests\Alpha.Architecture.Tests\ArchTest.cs", "Alpha.Architecture.Tests", 1, 10);
        var b2 = MakeBlock(code, @"src\Production\Host.cs", "Production", 1, 10);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1);

        var patterns = new List<string> { ".Architecture." };
        var filtered = clusters
            .Where(c => !c.Instances.All(inst =>
                patterns.Any(p => inst.ProjectName.Contains(p, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        Assert.Single(filtered);
    }

    [Fact]
    public void ExcludeProjectPattern_CaseInsensitive()
    {
        var code = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var b1 = MakeBlock(code, @"tests\Alpha.Architecture.Tests\A.cs", "Alpha.Architecture.Tests", 1, 10);
        var b2 = MakeBlock(code, @"tests\Beta.Architecture.Tests\B.cs", "Beta.Architecture.Tests", 1, 10);

        var clusters = _detector.Detect(new List<CodeBlock> { b1, b2 }, 0.99,
            minClusterSpread: 1, minProjectSpread: 1);

        // Lowercase pattern
        var patterns = new List<string> { ".architecture." };
        var filtered = clusters
            .Where(c => !c.Instances.All(inst =>
                patterns.Any(p => inst.ProjectName.Contains(p, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        Assert.Empty(filtered);
    }

    [Fact]
    public void ExcludeProjectPattern_MultiplePatterns_AnyMatchExcludes()
    {
        var code1 = """
            void BuildHost() {
                var svc = new ServiceCollection();
                svc.AddLogging();
                svc.AddSingleton<IApp, App>();
                return svc.BuildServiceProvider();
            }
            """;
        var code2 = """
            void SetupTest() {
                var factory = new ItemFactory();
                factory.Configure(ItemType.Default);
                factory.Build();
                return factory.Result;
            }
            """;

        var archBlocks = new List<CodeBlock>
        {
            MakeBlock(code1, @"tests\A.Architecture.Tests\A.cs", "A.Architecture.Tests", 1, 10),
            MakeBlock(code1, @"tests\B.Architecture.Tests\B.cs", "B.Architecture.Tests", 1, 10)
        };
        var testBlocks = new List<CodeBlock>
        {
            MakeBlock(code2, @"tests\X.Integration.Tests\X.cs", "X.Integration.Tests", 1, 10),
            MakeBlock(code2, @"tests\Y.Integration.Tests\Y.cs", "Y.Integration.Tests", 1, 10)
        };

        var clusters = _detector.Detect(archBlocks.Concat(testBlocks).ToList(), 0.99,
            minClusterSpread: 1, minProjectSpread: 1);

        var patterns = new List<string> { ".Architecture.", ".Integration." };
        var filtered = clusters
            .Where(c => !c.Instances.All(inst =>
                patterns.Any(p => inst.ProjectName.Contains(p, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        Assert.Empty(filtered);
    }
}
