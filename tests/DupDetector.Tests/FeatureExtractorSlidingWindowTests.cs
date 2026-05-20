using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests that verify sliding-window block extraction is disabled by default
/// and enabled only when <see cref="DetectionKind.Windows"/> is explicitly set.
/// Addresses GAP-3 from the tool report.
/// </summary>
public class FeatureExtractorSlidingWindowTests
{
    private readonly FeatureExtractor _extractor = new();

    private const string SampleCode = """
        public class Service {
            public void Process(int a, int b, int c, int d, int e) {
                var x = a + b;
                var y = b * c;
                var z = c - d;
                var w = d + e;
                Console.WriteLine(x + y + z + w);
            }
        }
        """;

    [Fact]
    public void DefaultKinds_DoNotProduceWindowBlocks()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleCode);
        var blocks = _extractor.Extract("file.cs", tree, SampleCode, 3, DetectionKind.All);

        Assert.DoesNotContain(blocks, b => b.MethodName.StartsWith("<window@"));
    }

    [Fact]
    public void NoExplicitKind_DoesNotProduceWindowBlocks()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleCode);
        // DetectionKind.All does not include Windows
        var blocks = _extractor.Extract("file.cs", tree, SampleCode, 3);

        Assert.DoesNotContain(blocks, b => b.MethodName.StartsWith("<window@"));
    }

    [Fact]
    public void WindowsKind_ProducesWindowBlocks()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleCode);
        var blocks = _extractor.Extract("file.cs", tree, SampleCode, 3,
            DetectionKind.Methods | DetectionKind.Windows);

        Assert.Contains(blocks, b => b.MethodName.StartsWith("<window@"));
    }

    [Fact]
    public void WindowsKind_ProducesOverlappingWindowsFromSameMethod()
    {
        var code = """
            public class C {
                public void M(int a, int b, int c, int d, int e, int f) {
                    var p = a + b;
                    var q = b + c;
                    var r = c + d;
                    var s = d + e;
                    var t = e + f;
                    Console.WriteLine(p + q + r + s + t);
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var blocks = _extractor.Extract("file.cs", tree, code, 3,
            DetectionKind.Methods | DetectionKind.Windows);

        var windowBlocks = blocks.Where(b => b.MethodName.StartsWith("<window@")).ToList();
        // Multiple overlapping windows should be produced from the single method
        Assert.True(windowBlocks.Count > 1, $"Expected multiple window blocks, got {windowBlocks.Count}");
    }

    [Fact]
    public void DefaultAll_DoesNotIncludeWindows()
    {
        // Verify that DetectionKind.All does NOT have the Windows flag set
        Assert.False(DetectionKind.All.HasFlag(DetectionKind.Windows));
    }

    [Fact]
    public void WindowsWithoutMethods_ProducesNoBlocksFromMethodsOrWindows()
    {
        // Windows alone (without Methods flag) should produce no blocks since windows
        // are extracted from method bodies — and method bodies are only visited when
        // the method node itself is included.
        var tree = CSharpSyntaxTree.ParseText(SampleCode);
        var blocks = _extractor.Extract("file.cs", tree, SampleCode, 3,
            DetectionKind.Windows);

        // No method-level blocks and no window blocks
        Assert.Empty(blocks);
    }

    [Fact]
    public void ConstructorsOnly_NoWindowBlocks()
    {
        var code = """
            public class Service {
                public Service(int a, int b, int c, int d, int e) {
                    _a = a;
                    _b = b;
                    _c = c;
                    _d = d;
                    _e = e;
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);
        var blocks = _extractor.Extract("file.cs", tree, code, 3, DetectionKind.Constructors);

        Assert.DoesNotContain(blocks, b => b.MethodName.StartsWith("<window@"));
        Assert.Contains(blocks, b => b.MethodName == "Service");
    }
}
