using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using Xunit;

namespace DupDetector.Core.Tests.Matching;

/// <summary>
///     
/// </summary>
public class TestFileClassifierTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsTestFile_IgnoresAbsolutePathAncestry()
    {
        Assert.False(TestFileClassifier.IsTestFile("src/Service.cs", ProjectIdentities.Named("MyApp")));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="relativePath"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("src/Models/Latest.cs", false)]
    [InlineData("src/Voting/Contest.cs", false)]
    [InlineData("src/Api/Greatest.cs", false)]
    [InlineData("src/Api/Manifest.cs", false)]
    [InlineData("src/Api/OrderService.cs", false)]
    [InlineData("src/OrderServiceTests.cs", true)]
    [InlineData("src/OrderServiceTest.cs", true)]
    [InlineData("src/OrderSpec.cs", true)]
    [InlineData("src/order_service_test.cs", true)]
    [InlineData("tests/Helpers.cs", true)]
    [InlineData("MyProject.Tests/Helpers.cs", true)]
    [InlineData("spec/Helpers.cs", true)]
    [InlineData("src/MyHTTPTest.cs", true)]
    public void IsTestFile_MatchesWholeWordsOnly(string relativePath, bool expected)
    {
        Assert.Equal(expected, TestFileClassifier.IsTestFile(relativePath, ProjectIdentity.Unknown));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void IsTestFile_UsesTheProjectNameWhenKnown()
    {
        Assert.True(TestFileClassifier.IsTestFile("src/Helpers.cs", ProjectIdentities.Named("Acme.Tests")));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="name"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("", false)]
    [InlineData("_", false)]
    [InlineData("Tests", true)]
    [InlineData("tests", true)]
    [InlineData("Specs", true)]
    [InlineData("Service", false)]
    public void LastWordIsTestWord_ExaminesTheFinalWord(string name, bool expected)
    {
        Assert.Equal(expected, TestFileClassifier.CanLastWordIsTestWord(name));
    }
}
