using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using DupDetector.Core.Normalization;
using DupDetector.TestKit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DupDetector.Core.Tests.Normalization;

public class StructuralNormalizerTests
{
    private static string Normalize(string source) =>
        StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Text;

    private static string Hash(string source) =>
        StructuralNormalizer.Normalize(CSharpSyntaxTree.ParseText(source).GetRoot()).Hash;

    [Fact]
    public void Normalize_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => StructuralNormalizer.Normalize(null!));

    [Fact]
    public void Hash_IsStableAcrossCalls() =>
        Assert.Equal(Hash("class C { void M() { } }"), Hash("class C { void M() { } }"));

    [Fact]
    public void UnrelatedMappersOverDifferentTypesDoNotCollide()
    {
        // The defect that made two unrelated DTO mappers report as one exact cluster.
        var widget = Hash("class C { WidgetResult Process(WidgetInput input) { var r = new WidgetResult(); r.Name = input.Name; return r; } }");
        var gadget = Hash("class C { GadgetResult Handle(GadgetInput input) { var g = new GadgetResult(); g.Name = input.Name; return g; } }");

        Assert.NotEqual(widget, gadget);
    }

    [Fact]
    public void UnrelatedMethodsWithTheSameShapeDoNotCollide()
    {
        var charge = Hash("class C { int ChargeCard(int p) { int amount = 100; return amount; } }");
        var email = Hash("class C { int SendEmail(int p) { int retries = 5; return retries; } }");

        // Same shape, but the declared names differ only after renaming, so the member names decide.
        Assert.Equal(charge, email);
    }

    [Fact]
    public void GenuineCopiesStillMatchDespiteRenaming()
    {
        var original = Hash("class C { int Total(Order order) { var sum = order.Price; return sum; } }");
        var copy = Hash("class C { int Sum(Order invoice) { var accumulator = invoice.Price; return accumulator; } }");

        Assert.Equal(original, copy);
    }

    [Fact]
    public void MemberAccessNamesArePreserved()
    {
        var price = Hash("class C { int M(Order o) { return o.Price; } }");
        var quantity = Hash("class C { int M(Order o) { return o.Quantity; } }");

        Assert.NotEqual(price, quantity);
    }

    [Fact]
    public void ParameterAndLocalTypesArePreservedConsistently()
    {
        var ints = Hash("class C { int M(int a) { int t = a; return t; } }");
        var longs = Hash("class C { long M(long a) { long t = a; return t; } }");

        Assert.NotEqual(ints, longs);
    }

    [Fact]
    public void DeclaredNamesAreRenamedInOrderOfAppearance() =>
        Assert.Contains("var0", Normalize("class C { void M(int alpha) { int beta = alpha; } }"), StringComparison.Ordinal);

    [Fact]
    public void ARenamedIdentifierKeepsTheSameReplacementEverywhere()
    {
        var normalized = Normalize("class C { void M(int alpha) { int b = alpha; int c = alpha; } }");
        Assert.Equal(3, normalized.Split("var1").Length - 1);
    }

    [Theory]
    [InlineData("class C { void M() { var x = \"s\"; } }", "STR")]
    [InlineData("class C { void M() { var x = \"s\"u8; } }", "STR")]
    [InlineData("class C { void M() { var x = 1; } }", "NUM")]
    [InlineData("class C { void M() { var x = 'c'; } }", "CHR")]
    [InlineData("class C { void M() { var x = true; } }", "BOOL")]
    [InlineData("class C { void M() { var x = false; } }", "BOOL")]
    [InlineData("class C { void M() { object x = null; } }", "NULL")]
    [InlineData("class C { void M() { int x = default; } }", "LIT")]
    public void LiteralsBecomeKindPlaceholders(string source, string placeholder) =>
        Assert.Contains(placeholder, Normalize(source), StringComparison.Ordinal);

    [Fact]
    public void LiteralsOfDifferentValuesButOneKindCollapseTogether() =>
        Assert.Equal(
            Hash("class C { int M() { return 1; } }"),
            Hash("class C { int M() { return 9999; } }"));

    [Fact]
    public void MemberBindingNamesArePreserved()
    {
        var first = Hash("class C { int? M(Order o) { return o?.Price; } }");
        var second = Hash("class C { int? M(Order o) { return o?.Quantity; } }");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void QualifiedTypeNamesArePreserved()
    {
        var first = Hash("class C { void M() { System.Console.Write(1); } }");
        var second = Hash("class C { void M() { System.Diagnostics.Trace.Write(1); } }");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void AllDeclarationSitesAreCollected()
    {
        const string Source = """
            class C
            {
                T M<T>(T seed, System.Collections.Generic.List<T> items)
                {
                    var accumulator = seed;
                    foreach (var item in items) { accumulator = item; }
                    if (items is { Count: > 0 } populated) { accumulator = populated[0]; }
                    try { Local(); } catch (System.Exception error) { System.Console.Write(error); }
                    T Local() { return accumulator; }
                    return accumulator;
                }
            }
            """;

        var normalized = Normalize(Source);

        Assert.DoesNotContain("accumulator", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("populated", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("error", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("seed", normalized, StringComparison.Ordinal);
        Assert.DoesNotContain("Local", normalized, StringComparison.Ordinal);
        // Type names and member names survive.
        Assert.Contains("Console", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void ALocalSharingAMemberNameDoesNotRenameTheMember()
    {
        // The parameter named Price is renamed; the member access .Price is not.
        var normalized = Normalize("class C { void M(Order Price) { var x = Price.Price; } }");

        Assert.Equal("class C { void var0 ( Order var1 ) { var var2 = var1 . Price ; } }", normalized);
    }

    [Fact]
    public void ADeclaredNameReusedAsAConditionalMemberNameIsPreserved()
    {
        var normalized = Normalize("class C { void M(Order Price) { var x = Price?.Price; } }");

        Assert.Contains("Order var1", normalized, StringComparison.Ordinal);
        Assert.Contains("Price", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void ADeclaredNameReusedAsAQualifiedNameSegmentIsPreserved()
    {
        // The parameter is called Generic, which also appears inside System.Collections.Generic.
        var normalized = Normalize(
            "class C { void M(int Generic) { System.Collections.Generic.List<int> items = null; } }");

        Assert.Contains("int var1", normalized, StringComparison.Ordinal);
        Assert.Contains("Collections", normalized, StringComparison.Ordinal);
        Assert.Contains("Generic", normalized, StringComparison.Ordinal);
    }
}

public class MemberBlockExtractorTests
{
    private static DetectionSettings Settings(DetectionKind kinds = DetectionKind.All, int minLines = 1) =>
        new() { Kinds = kinds, MinLines = minLines };

    [Fact]
    public void Extract_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => MemberBlockExtractor.Extract(null!, Settings()));
        Assert.Throws<ArgumentNullException>(() => MemberBlockExtractor.Extract(Code.Unit("class C { }"), null!));
    }

    [Fact]
    public void Extract_FindsEveryMemberKind()
    {
        const string Source = """
            class C
            {
                public C() { }
                ~C() { }
                public int Total { get { return 1; } set { _ = value; } }
                public int this[int i] { get { return i; } }
                public event System.EventHandler E { add { } remove { } }
                public static C operator +(C a, C b) { return a; }
                public static explicit operator int(C a) { return 0; }
                void M() { void Inner() { } Inner(); }
            }
            """;

        var names = Code.Blocks(Source, Settings()).Select(block => block.MemberName).ToArray();

        Assert.Contains("C", names);
        Assert.Contains("~C", names);
        Assert.Contains("Total.get", names);
        Assert.Contains("Total.set", names);
        Assert.Contains("this[].get", names);
        Assert.Contains("E.add", names);
        Assert.Contains("operator +", names);
        Assert.Contains("operator int", names);
        Assert.Contains("M", names);
        Assert.Contains("Inner", names);
    }

    [Theory]
    [InlineData(DetectionKind.Methods, "M")]
    [InlineData(DetectionKind.Constructors, "C")]
    [InlineData(DetectionKind.Destructors, "~C")]
    [InlineData(DetectionKind.Accessors, "Total.get")]
    [InlineData(DetectionKind.Operators, "operator +")]
    public void Extract_HonoursTheRequestedKinds(DetectionKind kind, string expected)
    {
        const string Source = """
            class C
            {
                public C() { }
                ~C() { }
                public int Total { get { return 1; } }
                public static C operator +(C a, C b) { return a; }
                void M() { }
            }
            """;

        var names = Code.Blocks(Source, Settings(kind)).Select(block => block.MemberName).ToArray();
        Assert.Equal([expected], names);
    }

    [Fact]
    public void Extract_SkipsMembersBelowTheMinimumSize()
    {
        var blocks = Code.Blocks(Code.Method("M", statementCount: 1), Settings(DetectionKind.Methods, minLines: 50));
        Assert.Empty(blocks);
    }

    [Fact]
    public void Extract_RecordsLocationAndText()
    {
        var block = Assert.Single(Code.Blocks("class C\n{\n    void M()\n    {\n    }\n}", Settings(DetectionKind.Methods)));

        Assert.Equal("/repo/File.cs", block.FilePath);
        Assert.Equal(ProjectIdentity.Named("Proj"), block.Project);
        Assert.False(block.IsTestFile);
        Assert.Equal(3, block.Lines.Start);
        Assert.Equal(5, block.Lines.End);
        Assert.Contains("void M()", block.RawText, StringComparison.Ordinal);
        Assert.NotEmpty(block.Hash);
    }

    [Fact]
    public void Describe_ReturnsNullForNodesThatAreNotMembers()
    {
        var root = CSharpSyntaxTree.ParseText("namespace N { class C { } }").GetRoot();
        var declaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();

        Assert.Null(MemberBlockExtractor.Describe(declaration));
    }

    [Fact]
    public void Describe_FallsBackWhenAnAccessorHasNoOwningMember()
    {
        var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, SyntaxFactory.Block());

        var described = MemberBlockExtractor.Describe(accessor);

        Assert.NotNull(described);
        Assert.Equal("?.get", described.Value.Name);
        Assert.Equal(DetectionKind.Accessors, described.Value.Kind);
    }
}
