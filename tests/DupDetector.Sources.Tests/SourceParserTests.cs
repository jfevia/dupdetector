using DupDetector.Core.Model.Reporting;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

namespace DupDetector.Sources.Tests;

/// <summary>
///     
/// </summary>
public class SourceParserTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DescribeParseFailures_ReportsBrokenSource()
    {
        var diagnostic = SourceParser.DescribeParseFailures(SourceParser.Parse("class C { void M( }", "x.cs"), "x.cs");

        Assert.NotNull(diagnostic);
        Assert.Equal(SourceDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("x.cs", diagnostic.Path);
        Assert.Contains("parse error", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Diagnostics_CarryTheirSeverity()
    {
        Assert.Equal(SourceDiagnosticSeverity.Error, SourceDiagnostics.Error("m", null).Severity);
        Assert.Equal(SourceDiagnosticSeverity.Warning, SourceDiagnostics.Warning("m", null).Severity);
        Assert.Empty(SourceLoadResult.Empty.Units);
        Assert.Empty(SourceLoadResult.Empty.Diagnostics);
        Assert.Equal(DiscoveryMode.None, SourceLoadResult.Empty.Stats.Mode);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Options_PinAnExplicitLanguageVersion()
    {
        Assert.Equal(LanguageVersion.Preview, SourceParser.Options.LanguageVersion);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="source"></param>
    [Theory]
    [InlineData("class C { void M() { var s = \"\\e[0m\"; } }")]
    [InlineData("static class E { extension(string s) { public bool IsLong => s.Length > 10; } }")]
    [InlineData("partial class C { public partial int P { get; set; } }")]
    [InlineData("class C { int[] A = [1, 2, 3]; }")]
    public void Parse_AcceptsModernSyntax(string source)
    {
        Assert.Null(SourceParser.DescribeParseFailures(SourceParser.Parse(source, "x.cs"), "x.cs"));
    }
}
