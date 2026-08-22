using DupDetector.Core.Matching;
using Xunit;

namespace DupDetector.Core.Tests.Matching;

/// <summary>
///     
/// </summary>
public class GlobPatternTests
{
    /// <summary>
    ///     
    /// </summary>
    /// <param name="path"></param>
    /// <param name="pattern"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("src/**", "C:/repo/src/Foo.cs", true)]
    [InlineData("src/**", "C:/repo/src/deep/Foo.cs", true)]
    [InlineData("src/**", "C:/repo/lib/Foo.cs", false)]
    [InlineData("**", "C:/repo/src/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/obj/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/src/obj/a/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/src/Foo.cs", false)]
    [InlineData("**/Arch/*.cs", "C:/repo/src/Arch/Foo.cs", true)]
    [InlineData("**/Arch/*.cs", "C:/repo/src/Arch/deep/Foo.cs", false)]
    [InlineData("Gen", "C:/a/Gen/F.cs", true)]
    [InlineData("Gen", "C:/a/Generated/F.cs", false)]
    [InlineData("*.cs", "C:/a/F.cs", true)]
    [InlineData("*.cs", "C:/a/F.txt", false)]
    [InlineData("a/*/c.cs", "x/a/b/c.cs", true)]
    [InlineData("a/*/c.cs", "x/a/b/d/c.cs", false)]
    [InlineData("F?.cs", "C:/a/F1.cs", true)]
    [InlineData("F?.cs", "C:/a/F12.cs", false)]
    [InlineData("a**b", "x/aQQb", true)]
    [InlineData("/src/**", "C:/repo/src/Foo.cs", true)]
    public void IsMatch_FollowsGitignoreSemantics(string pattern, string path, bool expected)
    {
        Assert.Equal(expected, GlobPatterns.Parse(pattern).IsMatch(path));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsMatch_IsCaseInsensitiveAndSeparatorAgnostic()
    {
        var pattern = GlobPatterns.Parse("**/OBJ/**");
        Assert.True(pattern.IsMatch(@"C:\repo\obj\Foo.cs"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Parse_RejectsBlankPatterns()
    {
        Assert.Throws<ArgumentException>(() => GlobPatterns.Parse("  "));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void PatternAndToString_ExposeTheOriginalText()
    {
        var pattern = GlobPatterns.Parse("**/x.cs");
        Assert.Equal("**/x.cs", pattern.Pattern);
        Assert.Equal("**/x.cs", pattern.ToString());
    }
}
