using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

public class CodeNormalizerTests
{
    private readonly CodeNormalizer _normalizer = new();

    [Fact]
    public void DifferentVariableNames_NormalizeToSameOutput()
    {
        var src1 = "void M() { int alpha = 1; }";
        var src2 = "void M() { int beta = 1; }";

        var tree1 = CSharpSyntaxTree.ParseText(src1);
        var tree2 = CSharpSyntaxTree.ParseText(src2);

        var norm1 = _normalizer.Normalize(tree1.GetRoot());
        var norm2 = _normalizer.Normalize(tree2.GetRoot());

        Assert.Equal(norm1, norm2);
    }

    [Fact]
    public void StringLiterals_AreReplacedWithSTR_LIT()
    {
        var src = "void M() { var x = \"hello world\"; }";
        var tree = CSharpSyntaxTree.ParseText(src);
        var normalized = _normalizer.Normalize(tree.GetRoot());

        Assert.Contains("STR_LIT", normalized);
        Assert.DoesNotContain("hello world", normalized);
    }

    [Fact]
    public void NumericLiterals_AreReplacedWithNUM_LIT()
    {
        var src = "void M() { var x = 42; }";
        var tree = CSharpSyntaxTree.ParseText(src);
        var normalized = _normalizer.Normalize(tree.GetRoot());

        Assert.Contains("NUM_LIT", normalized);
        Assert.DoesNotContain("42", normalized);
    }

    [Fact]
    public void CharLiterals_AreReplacedWithCHAR_LIT()
    {
        var src = "void M() { var x = 'a'; }";
        var tree = CSharpSyntaxTree.ParseText(src);
        var normalized = _normalizer.Normalize(tree.GetRoot());

        Assert.Contains("CHAR_LIT", normalized);
        Assert.DoesNotContain("'a'", normalized);
    }

    [Fact]
    public void BoolLiterals_AreReplacedWithBOOL_LIT()
    {
        var src = "void M() { var x = true; var y = false; }";
        var tree = CSharpSyntaxTree.ParseText(src);
        var normalized = _normalizer.Normalize(tree.GetRoot());

        Assert.Contains("BOOL_LIT", normalized);
        Assert.DoesNotContain("true", normalized);
        Assert.DoesNotContain("false", normalized);
    }

    [Fact]
    public void SameStructure_ProducesSameHash()
    {
        var src1 = "void M() { int foo = 10; Console.WriteLine(foo); }";
        var src2 = "void M() { int bar = 99; Console.WriteLine(bar); }";

        var tree1 = CSharpSyntaxTree.ParseText(src1);
        var tree2 = CSharpSyntaxTree.ParseText(src2);

        var hash1 = _normalizer.GetStructuralHash(tree1.GetRoot());
        var hash2 = _normalizer.GetStructuralHash(tree2.GetRoot());

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void DifferentStructure_ProducesDifferentHash()
    {
        var src1 = "void M() { int x = 1; }";
        var src2 = "void M() { int x = 1; int y = 2; }";

        var tree1 = CSharpSyntaxTree.ParseText(src1);
        var tree2 = CSharpSyntaxTree.ParseText(src2);

        var hash1 = _normalizer.GetStructuralHash(tree1.GetRoot());
        var hash2 = _normalizer.GetStructuralHash(tree2.GetRoot());

        Assert.NotEqual(hash1, hash2);
    }
}
