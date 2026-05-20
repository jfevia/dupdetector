using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for the <c>--min-project-spread</c> filter (GAP-A, Run 3).
/// Verifies that <see cref="ClusterMetrics.ProjectSpread"/> is correctly computed
/// and that clusters can be filtered by project spread in both exact-match and near-dup phases.
/// </summary>
public class ProjectSpreadFilterTests
{
    private readonly DuplicateDetector _detector = new();
    private readonly CodeNormalizer _normalizer = new();

    private CodeBlock MakeBlock(string code, string file, string project, int start = 1, int end = 10)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        return new CodeBlock(file, start, end, "M", hash, normalized, code, end - start + 1)
        {
            ProjectName = project
        };
    }

    private static string MakeCode(string tag) => $$"""
        void Method_{{tag}}(int a, int b) {
            var r = a + b;
            Console.WriteLine(r);
            var x = r * 2;
            return;
        }
        """;

    // ──── ProjectSpread computation ───────────────────────────────────────────

    [Fact]
    public void ProjectSpread_IsOne_WhenAllBlocksSameProject()
    {
        var code = MakeCode("A");
        var blocks = new[]
        {
            MakeBlock(code, "file1.cs", "ProjectA", 1, 6),
            MakeBlock(code, "file2.cs", "ProjectA", 10, 15),
            MakeBlock(code, "file3.cs", "ProjectA", 20, 25),
        };

        var clusters = _detector.Detect(blocks.ToList(), 0.99, minClusterSpread: 1);

        Assert.Single(clusters);
        Assert.Equal(1, clusters[0].Metrics.ProjectSpread);
    }

    [Fact]
    public void ProjectSpread_IsTwo_WhenBlocksSpanTwoProjects()
    {
        var code = MakeCode("B");
        var blocks = new[]
        {
            MakeBlock(code, "file1.cs", "ProjectA", 1, 6),
            MakeBlock(code, "file2.cs", "ProjectA", 10, 15),
            MakeBlock(code, "file3.cs", "ProjectB", 20, 25),
        };

        var clusters = _detector.Detect(blocks.ToList(), 0.99, minClusterSpread: 1);

        Assert.Single(clusters);
        Assert.Equal(2, clusters[0].Metrics.ProjectSpread);
    }

    [Fact]
    public void ProjectSpread_CountsDistinctProjects()
    {
        var code = MakeCode("C");
        var blocks = Enumerable.Range(0, 9)
            .Select(i => MakeBlock(code, $"file{i}.cs", $"Project{i % 3}", 1 + i * 10, 6 + i * 10))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.99, minClusterSpread: 1);

        Assert.Single(clusters);
        Assert.Equal(3, clusters[0].Metrics.ProjectSpread);
    }

    [Fact]
    public void ProjectSpread_FallsBackToFileSpread_WhenProjectNamesEmpty()
    {
        var code = MakeCode("D");
        // Blocks with no ProjectName set — should fall back to file spread
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        var blocks = Enumerable.Range(0, 4)
            .Select(i => new CodeBlock($"file{i}.cs", 1, 6, "M", hash, normalized, code, 6))
            .ToList();

        var clusters = _detector.Detect(blocks, 0.99, minClusterSpread: 1);

        Assert.Single(clusters);
        // No ProjectName set → falls back to file spread (4 distinct files)
        Assert.Equal(4, clusters[0].Metrics.ProjectSpread);
    }

    // ──── Filtering by MinProjectSpread ──────────────────────────────────────

    [Fact]
    public void MinProjectSpread_1_KeepsSingleProjectCluster()
    {
        var code = MakeCode("E");
        var blocks = new[]
        {
            MakeBlock(code, "file1.cs", "ProjectA", 1, 6),
            MakeBlock(code, "file2.cs", "ProjectA", 10, 15),
        };

        var clusters = _detector.Detect(blocks.ToList(), 0.99, minClusterSpread: 1, minProjectSpread: 1);

        Assert.NotEmpty(clusters);
    }

    [Fact]
    public void MinProjectSpread_2_FiltersSingleProjectCluster()
    {
        var code = MakeCode("F");
        var blocks = new[]
        {
            MakeBlock(code, "file1.cs", "ProjectA", 1, 6),
            MakeBlock(code, "file2.cs", "ProjectA", 10, 15),
            MakeBlock(code, "file3.cs", "ProjectA", 20, 25),
        };

        var clusters = _detector.Detect(blocks.ToList(), 0.99, minClusterSpread: 1, minProjectSpread: 2);

        Assert.Empty(clusters);
    }

    [Fact]
    public void MinProjectSpread_2_KeepsCrossProjectCluster()
    {
        var code = MakeCode("G");
        var blocks = new[]
        {
            MakeBlock(code, "file1.cs", "ProjectA", 1, 6),
            MakeBlock(code, "file2.cs", "ProjectB", 10, 15),
        };

        var clusters = _detector.Detect(blocks.ToList(), 0.99, minClusterSpread: 1, minProjectSpread: 2);

        Assert.NotEmpty(clusters);
        Assert.Equal(2, clusters[0].Metrics.ProjectSpread);
    }

    [Fact]
    public void MinProjectSpread_AlsoFiltersNearDupClusters()
    {
        // Two near-dup blocks that differ slightly, both in the same project
        var code1 = """
            void NearA(int x, int y) {
                var r = x + y;
                Console.WriteLine(r);
                return;
            }
            """;
        var code2 = """
            void NearB(int a, int b) {
                var r = a + b;
                Console.WriteLine(r);
                return;
            }
            """;
        var block1 = MakeBlock(code1, "file1.cs", "SameProject", 1, 5);
        var block2 = MakeBlock(code2, "file2.cs", "SameProject", 10, 14);

        // minClusterSpread=1 to allow same-file; only minProjectSpread=2 should filter
        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.70,
            minClusterSpread: 1, minProjectSpread: 2);

        Assert.Empty(clusters);
    }

    [Fact]
    public void MinProjectSpread_NearDup_CrossProjectKept()
    {
        var code1 = """
            void NearA(int x, int y) {
                var r = x + y;
                Console.WriteLine(r);
                return;
            }
            """;
        var code2 = """
            void NearB(int a, int b) {
                var r = a + b;
                Console.WriteLine(r);
                return;
            }
            """;
        var block1 = MakeBlock(code1, "file1.cs", "ProjectA", 1, 5);
        var block2 = MakeBlock(code2, "file2.cs", "ProjectB", 10, 14);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.70,
            minClusterSpread: 1, minProjectSpread: 2);

        Assert.NotEmpty(clusters);
    }

    // ──── CLI parsing ─────────────────────────────────────────────────────────

    [Fact]
    public void CliParser_MinProjectSpread_IsParsed()
    {
        var opts = CliArgParser.Parse(["my.sln", "--min-project-spread", "3"]);
        Assert.Equal(3, opts.MinProjectSpread);
    }

    [Fact]
    public void CliParser_MinProjectSpread_Default_IsTwo()
    {
        var opts = CliArgParser.Parse(["my.sln"]);
        Assert.Equal(2, opts.MinProjectSpread);
    }

    [Fact]
    public void CliParser_MinProjectSpread_ClampsToMinimum1()
    {
        var opts = CliArgParser.Parse(["my.sln", "--min-project-spread", "0"]);
        Assert.Equal(1, opts.MinProjectSpread);
    }

    // ──── ProjectName on CodeBlock ────────────────────────────────────────────

    [Fact]
    public void CodeBlock_ProjectName_DefaultsToEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText("void M() {}");
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        var block = new CodeBlock("file.cs", 1, 1, "M", hash, normalized, "void M() {}", 1);

        Assert.Equal("", block.ProjectName);
    }

    [Fact]
    public void CodeBlock_ProjectName_CanBeSet()
    {
        var tree = CSharpSyntaxTree.ParseText("void M() {}");
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        var block = new CodeBlock("file.cs", 1, 1, "M", hash, normalized, "void M() {}", 1)
        {
            ProjectName = "MyProject"
        };

        Assert.Equal("MyProject", block.ProjectName);
    }

    // ──── FeatureExtractor stamps project name ─────────────────────────────────

    [Fact]
    public void FeatureExtractor_StampsProjectName_OnExtractedBlocks()
    {
        var extractor = new FeatureExtractor();
        var code = """
            public class C {
                public void MyMethod(int a, int b, int c) {
                    var x = a + b;
                    var y = x * c;
                    Console.WriteLine(y);
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var blocks = extractor.Extract("file.cs", tree, code, 3, projectName: "MyProject");

        Assert.True(blocks.Count > 0);
        Assert.All(blocks, b => Assert.Equal("MyProject", b.ProjectName));
    }

    [Fact]
    public void FeatureExtractor_DefaultProjectName_IsEmpty()
    {
        var extractor = new FeatureExtractor();
        var code = """
            public class C {
                public void Method(int a, int b) {
                    var x = a + b;
                    Console.WriteLine(x);
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var blocks = extractor.Extract("file.cs", tree, code, 3);

        Assert.True(blocks.Count > 0);
        Assert.All(blocks, b => Assert.Equal("", b.ProjectName));
    }
}
