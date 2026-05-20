using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for <see cref="TestFileHelper.IsTestFile"/> path heuristics.
/// Addresses GAP-6 and GAP-7: annotating test files to separate them from
/// production source hotspots.
/// </summary>
public class TestFileDetectionTests
{
    // ──── Files that should be identified as test files ───────────────────────

    [Theory]
    [InlineData("tests/Client.Core.Tests/FooTests.cs")]
    [InlineData("tests/IO.Binary.Tests/OpenTibiaBinaryWriterTests.cs")]
    [InlineData(@"C:\Repos\MyApp\tests\MyApp.Tests\FooTests.cs")]
    [InlineData("src/MyProject.Tests/Foo.cs")]
    [InlineData("test/UnitTest/Bar.cs")]
    [InlineData("specs/BehaviourSpec/Baz.cs")]
    [InlineData("spec/FeatureSpec/Qux.cs")]
    public void TestFilePaths_AreRecognized(string path)
    {
        Assert.True(TestFileHelper.IsTestFile(path), $"Expected IsTestFile=true for: {path}");
    }

    [Theory]
    [InlineData("src/Foo/BarTests.cs")]
    [InlineData("src/Foo/BazTest.cs")]
    [InlineData("src/Foo/QuxSpecs.cs")]
    [InlineData("src/Foo/QuxSpec.cs")]
    public void TestFileSuffixes_AreRecognized(string path)
    {
        Assert.True(TestFileHelper.IsTestFile(path), $"Expected IsTestFile=true for: {path}");
    }

    // ──── Files that should NOT be identified as test files ───────────────────

    [Theory]
    [InlineData("src/Client.Core/Services/FooService.cs")]
    [InlineData("src/IO.Objects/ObjectDataReader.cs")]
    [InlineData("src/Analyzers/UI/ViewModelAnalyzer.cs")]
    [InlineData("src/Map.Viewer.Desktop/ViewModels/ShellViewModel.cs")]
    [InlineData(@"C:\Repos\MyApp\src\Core\Domain\Entity.cs")]
    public void ProductionFilePaths_AreNotRecognized(string path)
    {
        Assert.False(TestFileHelper.IsTestFile(path), $"Expected IsTestFile=false for: {path}");
    }

    // ──── Edge cases ──────────────────────────────────────────────────────────

    [Fact]
    public void FileNamedTestableUtility_IsNotATestFile()
    {
        // A file called "Testable.cs" or "ContestEntry.cs" should not be flagged
        Assert.False(TestFileHelper.IsTestFile("src/Core/TestableBase.cs"));
        Assert.False(TestFileHelper.IsTestFile("src/Core/ContestEntry.cs"));
    }

    [Fact]
    public void PathWithTestInProjectName_TreatsSegmentExactly()
    {
        // "tests" directory segment → test file
        Assert.True(TestFileHelper.IsTestFile("tests/Foo/Bar.cs"));
        // But "contested" is not an exact match for a test segment
        Assert.False(TestFileHelper.IsTestFile("contested/Foo/Bar.cs"));
    }

    [Fact]
    public void CaseInsensitiveMatchingWorks()
    {
        Assert.True(TestFileHelper.IsTestFile("TESTS/Foo/Bar.cs"));
        Assert.True(TestFileHelper.IsTestFile("src/TESTS/Foo.cs"));
        Assert.True(TestFileHelper.IsTestFile("src/Foo/BarTESTS.cs"));
    }

    [Fact]
    public void BackslashPaths_AreNormalizedCorrectly()
    {
        Assert.True(TestFileHelper.IsTestFile(@"tests\Client.Core.Tests\FooTests.cs"));
        Assert.False(TestFileHelper.IsTestFile(@"src\Core\Services\FooService.cs"));
    }

    [Fact]
    public void EmptyPath_DoesNotThrow()
    {
        Assert.False(TestFileHelper.IsTestFile(""));
    }

    [Fact]
    public void FileScoreIsTestFile_ReflectsHeuristic()
    {
        var testScore = new FileScore
        {
            File = "tests/MyTests/FooTests.cs",
            IsTestFile = TestFileHelper.IsTestFile("tests/MyTests/FooTests.cs")
        };
        var prodScore = new FileScore
        {
            File = "src/MyProject/Foo.cs",
            IsTestFile = TestFileHelper.IsTestFile("src/MyProject/Foo.cs")
        };

        Assert.True(testScore.IsTestFile);
        Assert.False(prodScore.IsTestFile);
    }
}
