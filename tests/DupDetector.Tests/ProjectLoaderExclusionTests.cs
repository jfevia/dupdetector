using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests that verify <see cref="ProjectLoader"/> excludes build-artifact directories
/// (<c>obj/</c> and <c>bin/</c>) and applies user-defined glob patterns correctly.
/// Addresses GAP-1: auto-generated build artifact files were appearing in fileScores.
/// </summary>
public class ProjectLoaderExclusionTests
{
    // ──── obj/ directory exclusion ────────────────────────────────────────────

    [Theory]
    [InlineData(@"C:\Repos\MyProject\obj\Debug\net10.0\AssemblyInfo.cs")]
    [InlineData("src/MyProject/obj/Debug/net10.0/AssemblyInfo.cs")]
    [InlineData("tests/MyTests/obj/Release/net10.0/MyTests.AssemblyAttributes.cs")]
    [InlineData("obj/Foo.cs")]
    [InlineData("/home/user/project/obj/bar.cs")]
    public void ObjDirectoryFiles_AreExcluded(string filePath)
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.True(loader.IsExcluded(filePath),
            $"Expected {filePath} to be excluded (obj/)");
    }

    [Theory]
    [InlineData(@"C:\Repos\MyProject\bin\Debug\net10.0\MyProject.dll")]
    [InlineData("src/MyProject/bin/Release/net10.0/MyProject.exe")]
    [InlineData("bin/Foo.cs")]
    [InlineData("/home/user/project/bin/bar.cs")]
    public void BinDirectoryFiles_AreExcluded(string filePath)
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.True(loader.IsExcluded(filePath),
            $"Expected {filePath} to be excluded (bin/)");
    }

    // ──── Normal source files are NOT excluded ────────────────────────────────

    [Theory]
    [InlineData("src/MyProject/MyClass.cs")]
    [InlineData("tests/MyProject.Tests/MyTests.cs")]
    [InlineData(@"C:\Repos\MyProject\src\Core\Domain\Entity.cs")]
    [InlineData("src/Objects/BinaryReader.cs")]
    public void SourceFiles_AreNotExcluded(string filePath)
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.False(loader.IsExcluded(filePath),
            $"Expected {filePath} to NOT be excluded");
    }

    // ──── obj/bin as directory segment, not substring ─────────────────────────

    [Fact]
    public void ObjAsSubstring_NotExcluded()
    {
        var loader = new ProjectLoader(new DetectionOptions());

        Assert.False(loader.IsExcluded("src/objects/MyFile.cs"),
            "Directory 'objects' should not be excluded (not an exact 'obj' segment)");
        Assert.False(loader.IsExcluded("src/objviewer/MyFile.cs"),
            "Directory 'objviewer' should not be excluded");
    }

    [Fact]
    public void BinAsSubstring_NotExcluded()
    {
        var loader = new ProjectLoader(new DetectionOptions());

        Assert.False(loader.IsExcluded("src/binary/MyFile.cs"),
            "Directory 'binary' should not be excluded");
        Assert.False(loader.IsExcluded("src/bindings/MyFile.cs"),
            "Directory 'bindings' should not be excluded");
    }

    // ──── Nested obj/bin still excluded ──────────────────────────────────────

    [Fact]
    public void DeeplyNestedObjPath_IsExcluded()
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.True(loader.IsExcluded("a/b/c/obj/d/e/File.cs"));
    }

    [Fact]
    public void DeeplyNestedBinPath_IsExcluded()
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.True(loader.IsExcluded("a/b/c/bin/d/e/File.cs"));
    }

    // ──── Case-insensitive matching ───────────────────────────────────────────

    [Theory]
    [InlineData("src/OBJ/File.cs")]
    [InlineData("src/Obj/File.cs")]
    [InlineData("src/BIN/File.cs")]
    [InlineData("src/Bin/File.cs")]
    public void ArtifactDirectories_MatchCaseInsensitively(string filePath)
    {
        var loader = new ProjectLoader(new DetectionOptions());
        Assert.True(loader.IsExcluded(filePath));
    }

    // ──── User-specified exclude patterns still work ──────────────────────────

    [Fact]
    public void UserExcludePattern_IsApplied()
    {
        var options = new DetectionOptions();
        options.Exclude.Add("**/*.generated.cs");
        var loader = new ProjectLoader(options);

        Assert.True(loader.IsExcluded("src/Core/Foo.generated.cs"),
            "User pattern **/*.generated.cs should match");
        Assert.False(loader.IsExcluded("src/Core/Foo.cs"),
            "Normal file should not be excluded by *.generated.cs pattern");
    }

    [Fact]
    public void UserExcludePattern_WorksAlongsideObjExclusion()
    {
        var options = new DetectionOptions();
        options.Exclude.Add("**/Migrations/**");
        var loader = new ProjectLoader(options);

        Assert.True(loader.IsExcluded("src/App/obj/Debug/App.cs"));
        Assert.True(loader.IsExcluded("src/App/Migrations/001_Init.cs"));
        Assert.False(loader.IsExcluded("src/App/Services/FooService.cs"));
    }

    // ──── Directory scan integration ──────────────────────────────────────────

    [Fact]
    public void LoadFromDirectory_ExcludesObjAndBinFiles()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), "loader-excl-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var objDir = Path.Combine(tmpDir, "obj", "Debug", "net10.0");
            var binDir = Path.Combine(tmpDir, "bin", "Debug", "net10.0");
            Directory.CreateDirectory(objDir);
            Directory.CreateDirectory(binDir);

            var srcFile = Path.Combine(tmpDir, "MyClass.cs");
            File.WriteAllText(srcFile, "public class MyClass { }");

            // Artifact files (use IncludeGenerated=true so auto-generated filter doesn't interfere)
            File.WriteAllText(Path.Combine(objDir, "AssemblyInfo.cs"), "public class Artifact1 { }");
            File.WriteAllText(Path.Combine(binDir, "App.cs"), "public class Artifact2 { }");

            var options = new DetectionOptions { IncludeGenerated = true };
            var loader = new ProjectLoader(options);
            var docs = loader.LoadFromDirectoryInternal(tmpDir);

            var paths = docs.Select(d => d.FilePath).ToList();
            Assert.Contains(srcFile, paths);
            Assert.DoesNotContain(paths, p =>
                p.Replace('\\', '/').Split('/').Any(seg =>
                    seg.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                    seg.Equals("bin", StringComparison.OrdinalIgnoreCase)));
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }
}
