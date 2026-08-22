using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Core.Tests.Matching;

public class GlobPatternTests
{
    [Theory]
    // The pattern that silently matched nothing in the previous implementation.
    [InlineData("src/**", "C:/repo/src/Foo.cs", true)]
    [InlineData("src/**", "C:/repo/src/deep/Foo.cs", true)]
    [InlineData("src/**", "C:/repo/lib/Foo.cs", false)]
    [InlineData("**", "C:/repo/src/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/obj/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/src/obj/a/Foo.cs", true)]
    [InlineData("**/obj/**", "C:/repo/src/Foo.cs", false)]
    [InlineData("**/Arch/*.cs", "C:/repo/src/Arch/Foo.cs", true)]
    [InlineData("**/Arch/*.cs", "C:/repo/src/Arch/deep/Foo.cs", false)]
    // A bare name matches at any depth, which is what users expect.
    [InlineData("Gen", "C:/a/Gen/F.cs", true)]
    [InlineData("Gen", "C:/a/Generated/F.cs", false)]
    [InlineData("*.cs", "C:/a/F.cs", true)]
    [InlineData("*.cs", "C:/a/F.txt", false)]
    // A single star never crosses a separator.
    [InlineData("a/*/c.cs", "x/a/b/c.cs", true)]
    [InlineData("a/*/c.cs", "x/a/b/d/c.cs", false)]
    [InlineData("F?.cs", "C:/a/F1.cs", true)]
    [InlineData("F?.cs", "C:/a/F12.cs", false)]
    [InlineData("a**b", "x/aQQb", true)]
    [InlineData("/src/**", "C:/repo/src/Foo.cs", true)]
    public void IsMatch_FollowsGitignoreSemantics(string pattern, string path, bool expected) =>
        Assert.Equal(expected, GlobPattern.Parse(pattern).IsMatch(path));

    [Fact]
    public void IsMatch_IsCaseInsensitiveAndSeparatorAgnostic()
    {
        var pattern = GlobPattern.Parse("**/OBJ/**");
        Assert.True(pattern.IsMatch(@"C:\repo\obj\Foo.cs"));
    }

    [Fact]
    public void Parse_RejectsBlankPatterns() =>
        Assert.Throws<ArgumentException>(() => GlobPattern.Parse("  "));

    [Fact]
    public void Normalize_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => GlobPattern.Normalize(null!));

    [Fact]
    public void PatternAndToString_ExposeTheOriginalText()
    {
        var pattern = GlobPattern.Parse("**/x.cs");
        Assert.Equal("**/x.cs", pattern.Pattern);
        Assert.Equal("**/x.cs", pattern.ToString());
    }
}

public class GlobSetTests
{
    [Fact]
    public void Empty_MatchesNothing()
    {
        Assert.Equal(0, GlobSet.Empty.Count);
        Assert.False(GlobSet.Empty.IsMatch("C:/a/F.cs"));
    }

    [Fact]
    public void Parse_CombinesPatternsAsOr()
    {
        var set = GlobSet.Parse(["**/obj/**", "**/*.g.cs"]);
        Assert.Equal(2, set.Count);
        Assert.True(set.IsMatch("C:/a/obj/F.cs"));
        Assert.True(set.IsMatch("C:/a/F.g.cs"));
        Assert.False(set.IsMatch("C:/a/F.cs"));
    }

    [Fact]
    public void Parse_RejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => GlobSet.Parse(null!));
}

public class TestFileClassifierTests
{
    [Theory]
    // Word-boundary matching keeps these production files out of the test bucket.
    [InlineData("src/Models/Latest.cs", false)]
    [InlineData("src/Voting/Contest.cs", false)]
    [InlineData("src/Api/Greatest.cs", false)]
    [InlineData("src/Api/Manifest.cs", false)]
    [InlineData("src/Api/OrderService.cs", false)]
    // Genuine test files still classify.
    [InlineData("src/OrderServiceTests.cs", true)]
    [InlineData("src/OrderServiceTest.cs", true)]
    [InlineData("src/OrderSpec.cs", true)]
    [InlineData("src/order_service_test.cs", true)]
    [InlineData("tests/Helpers.cs", true)]
    [InlineData("MyProject.Tests/Helpers.cs", true)]
    [InlineData("spec/Helpers.cs", true)]
    [InlineData("src/MyHTTPTest.cs", true)]
    public void IsTestFile_MatchesWholeWordsOnly(string relativePath, bool expected) =>
        Assert.Equal(expected, TestFileClassifier.IsTestFile(relativePath, ProjectIdentity.Unknown));

    [Fact]
    public void IsTestFile_UsesTheProjectNameWhenKnown() =>
        Assert.True(TestFileClassifier.IsTestFile("src/Helpers.cs", ProjectIdentity.Named("Acme.Tests")));

    [Fact]
    public void IsTestFile_IgnoresAbsolutePathAncestry()
    {
        // The scan root is C:\test\myapp, so the relative path is all the classifier sees.
        Assert.False(TestFileClassifier.IsTestFile("src/Service.cs", ProjectIdentity.Named("MyApp")));
    }

    [Fact]
    public void IsTestFile_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => TestFileClassifier.IsTestFile(null!, ProjectIdentity.Unknown));
        Assert.Throws<ArgumentNullException>(() => TestFileClassifier.IsTestFile("a.cs", null!));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("_", false)]
    [InlineData("Tests", true)]
    [InlineData("tests", true)]
    [InlineData("Specs", true)]
    [InlineData("Service", false)]
    public void LastWordIsTestWord_ExaminesTheFinalWord(string name, bool expected) =>
        Assert.Equal(expected, TestFileClassifier.LastWordIsTestWord(name));
}
