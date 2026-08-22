using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using DupDetector.TestKit;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Covers whole-type extraction and the suppression accounting added after the report audit.
/// </summary>
public class TypeExtractionTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AllIncludesTypesWhileMembersDoesNot()
    {
        Assert.True(DetectionKind.All.HasFlag(DetectionKind.Types));
        Assert.False(DetectionKind.Members.HasFlag(DetectionKind.Types));
        Assert.Equal(DetectionKind.All, DetectionKind.Members | DetectionKind.Types);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AnExpressionBodiedIndexerIsExtracted()
    {
        const string source = """
            class C
            {
                public int this[int i] =>
                    i +
                    1;
            }
            """;

        var block = Assert.Single(TypeFixtures.Extract(source, DetectionKind.Accessors));

        Assert.Equal("this[]", block.MemberName);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void BlockBodiedIndexerIsCoveredByItsAccessorsRatherThanTwice()
    {
        const string source = """
            class C
            {
                public int this[int i]
                {
                    get
                    {
                        var a = i;
                        var b = a + 1;
                        return b;
                    }
                }
            }
            """;

        var names = Code.MemberNames(TypeFixtures.Extract(source, DetectionKind.Accessors));

        Assert.Equal(["this[].get"], names);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DelegateIsNotDetectedAsType()
    {
        Assert.Empty(TypeFixtures.Extract("delegate void D(int a, int b, int c, int d, int e);", DetectionKind.Types, 1));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void EveryDeclarationFormIsClassifiedBySingleSwitch()
    {
        const string source = """
            class Outer
            {
                ~Outer() { var a = 1; }

                public Outer() { var b = 2; }

                public static Outer operator +(Outer l, Outer r) { return l; }

                public static explicit operator int(Outer o) { return 0; }

                public int Arrow => 1;

                public int this[string k] => 2;

                public int Block { get { return 3; } }

                public int this[int i] { get { return 4; } }

                public event System.EventHandler E { add { } remove { } }

                void Method()
                {
                    void Local() { var c = 5; }
                    Local();
                }
            }

            record R(int A);

            record struct RS(int B);

            struct S { }

            interface I { }

            enum Kind { One }

            delegate void D();
            """;

        var described = new List<string>();
        foreach (var node in CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes())
        {
            if (MemberBlockExtractor.Describe(node) is { } info)
            {
                described.Add(info.Name);
            }
        }

        described.Sort(StringComparer.Ordinal);

        Assert.Equal(
            [
                "Arrow", "Block.get", "E.add", "E.remove", "Local", "Method", "Outer",
                "class Outer", "enum Kind", "interface I", "operator +", "operator int",
                "record R", "record RS", "struct S", "this[]", "this[].get", "~Outer",
            ],
            described);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="source"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("record R(int A)\n{\n    int M() => 1;\n}", "record R")]
    [InlineData("struct S\n{\n    int M() => 1;\n}", "struct S")]
    [InlineData("class C\n{\n    int M() => 1;\n}", "class C")]
    [InlineData("interface I\n{\n    int M();\n    int N();\n}", "interface I")]
    [InlineData("enum E\n{\n    One,\n    Two,\n}", "enum E")]
    public void EveryTypeDeclarationKindIsNamed(string source, string expected)
    {
        var block = Assert.Single(TypeFixtures.Extract(source, DetectionKind.Types));

        Assert.Equal(expected, block.MemberName);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void RecordIsLabelledFromItsOwnKeywordRatherThanItsNodeType()
    {
        Assert.Equal("record R", Assert.Single(TypeFixtures.Extract("record class R\n{\n    int M() => 1;\n}", DetectionKind.Types)).MemberName);
        Assert.Equal("record RS", Assert.Single(TypeFixtures.Extract("record struct RS\n{\n    int M() => 1;\n}", DetectionKind.Types)).MemberName);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void TypesAreHeldToTheirOwnMinimum()
    {
        const string source = """
            class C
            {
                int M() => 1;
            }
            """;

        Assert.Empty(TypeFixtures.Extract(source, DetectionKind.Types, 99));
        Assert.Single(TypeFixtures.Extract(source, DetectionKind.Types, 4));
    }
}
