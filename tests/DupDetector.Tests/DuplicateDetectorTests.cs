using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

public class DuplicateDetectorTests
{
    private readonly DuplicateDetector _detector = new();
    private readonly FeatureExtractor _extractor = new();
    private readonly CodeNormalizer _normalizer = new();

    private CodeBlock MakeBlock(string code, string file = "test.cs", int start = 1, int end = 10, string method = "M")
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var hash = _normalizer.GetStructuralHash(root);
        var normalized = _normalizer.Normalize(root);
        var lineCount = end - start + 1;
        return new CodeBlock(file, start, end, method, hash, normalized, code, lineCount);
    }

    [Fact]
    public void IdenticalMethods_AreDetectedAsDuplicates()
    {
        var code = """
            void DoWork() {
                var x = 1;
                var y = 2;
                var z = x + y;
                Console.WriteLine(z);
            }
            """;

        var block1 = MakeBlock(code, "file1.cs", 1, 7);
        var block2 = MakeBlock(code, "file2.cs", 1, 7);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.85);

        Assert.Single(clusters);
        Assert.Equal(2, clusters[0].Instances.Count);
    }

    [Fact]
    public void SameStructureDifferentNames_AreNearDuplicates()
    {
        var code1 = """
            void DoWork() {
                var alpha = 1;
                var beta = 2;
                var gamma = alpha + beta;
                Console.WriteLine(gamma);
            }
            """;

        var code2 = """
            void DoWork() {
                var foo = 1;
                var bar = 2;
                var baz = foo + bar;
                Console.WriteLine(baz);
            }
            """;

        var block1 = MakeBlock(code1, "file1.cs", 1, 7);
        var block2 = MakeBlock(code2, "file2.cs", 1, 7);

        // They should produce the same hash because variable names are normalized
        Assert.Equal(block1.NormalizedHash, block2.NormalizedHash);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.85);
        Assert.Single(clusters);
    }

    [Fact]
    public void BlocksBelowMinLines_AreFilteredByExtractor()
    {
        var shortCode = """
            using Microsoft.CodeAnalysis.CSharp;
            public class C {
                void Short() {
                    var x = 1;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(shortCode);
        // minLines = 10 means Short() with 1 line body should be excluded
        var blocks = _extractor.Extract("test.cs", tree, shortCode, 10);

        Assert.Empty(blocks);
    }

    [Fact]
    public void DifferentStructure_NotDetectedAsDuplicates()
    {
        var code1 = """
            void M1() {
                var x = 1;
                var y = 2;
                Console.WriteLine(x + y);
                return;
            }
            """;

        var code2 = """
            void M2() {
                var a = new List<int>();
                a.Add(1);
                a.Add(2);
                Console.WriteLine(a.Count);
            }
            """;

        var block1 = MakeBlock(code1, "file1.cs", 1, 7);
        var block2 = MakeBlock(code2, "file2.cs", 1, 7);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2 }, 0.95);

        Assert.Empty(clusters);
    }

    [Fact]
    public void Clusters_AreSortedByScoreDescending()
    {
        var code = """
            void DoWork() {
                var x = 1;
                var y = 2;
                var z = x + y;
                Console.WriteLine(z);
            }
            """;

        var bigCode = """
            void BigWork() {
                var a = 1; var b = 2; var c = a + b;
                var d = 3; var e = 4; var f = d + e;
                var g = 5; var h = 6; var k = g + h;
                Console.WriteLine(a + b + c + d + e + f + g + h + k);
            }
            """;

        var block1 = MakeBlock(code, "f1.cs", 1, 7);
        var block2 = MakeBlock(code, "f2.cs", 1, 7);
        var block3 = MakeBlock(bigCode, "f3.cs", 1, 7);
        var block4 = MakeBlock(bigCode, "f4.cs", 1, 7);

        var clusters = _detector.Detect(new List<CodeBlock> { block1, block2, block3, block4 }, 0.5);

        Assert.True(clusters.Count >= 1);
        // Verify descending score order
        for (int i = 1; i < clusters.Count; i++)
        {
            Assert.True(clusters[i - 1].Metrics.Score >= clusters[i].Metrics.Score);
        }
    }
}
