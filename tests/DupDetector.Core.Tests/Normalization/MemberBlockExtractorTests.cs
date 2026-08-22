using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using DupDetector.TestKit;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace DupDetector.Core.Tests.Normalization;

/// <summary>
///     
/// </summary>
public class MemberBlockExtractorTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Describe_FallsBackWhenAnAccessorHasNoOwningMember()
    {
        var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, SyntaxFactory.Block());

        var described = MemberBlockExtractor.Describe(accessor);

        Assert.NotNull(described);
        Assert.Equal("?.get", described.Name);
        Assert.Equal(DetectionKind.Accessors, described.Kind);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Describe_ReturnsNullForNodesThatAreNotMembers()
    {
        var root = CSharpSyntaxTree.ParseText("namespace N { class C { } }").GetRoot();
        NamespaceDeclarationSyntax? declaration = null;
        foreach (var node in root.DescendantNodes())
        {
            if (node is NamespaceDeclarationSyntax found)
            {
                declaration = found;
                break;
            }
        }

        Assert.NotNull(declaration);

        Assert.Null(MemberBlockExtractor.Describe(declaration));
    }

    /// <summary>
    ///     
    /// </summary>
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

        var names = Code.MemberNames(Code.Blocks(Source, ExtractorFixtures.Settings()));

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

    /// <summary>
    ///     
    /// </summary>
    /// <param name="kind"></param>
    /// <param name="expected"></param>
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

        var names = Code.MemberNames(Code.Blocks(Source, ExtractorFixtures.Settings(kind)));
        Assert.Equal([expected], names);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Extract_RecordsLocationAndText()
    {
        var block = Assert.Single(Code.Blocks("class C\n{\n    void M()\n    {\n    }\n}", ExtractorFixtures.Settings(DetectionKind.Methods)));

        Assert.Equal("/repo/File.cs", block.FilePath);
        Assert.Equal(ProjectIdentities.Named("Proj"), block.Project);
        Assert.False(block.IsTestFile);
        Assert.Equal(3, block.Lines.Start);
        Assert.Equal(5, block.Lines.End);
        Assert.Contains("void M()", block.RawText, StringComparison.Ordinal);
        Assert.NotEmpty(block.Hash);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Extract_SkipsMembersBelowTheMinimumSize()
    {
        var blocks = Code.Blocks(Code.Method("M", statementCount: 1), ExtractorFixtures.Settings(DetectionKind.Methods, 50));
        Assert.Empty(blocks);
    }
}
