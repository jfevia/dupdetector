using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for <see cref="ProjectLoader.FindNearestProjectName"/> and
/// the project grouping logic introduced to address GAP-5.
/// </summary>
public class ProjectGroupingTests
{
    // ──── FindNearestProjectName unit tests ───────────────────────────────────

    [Fact]
    public void FindNearestProjectName_InSameDirAsCsproj_ReturnsProjectName()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "MyProject.csproj"), "<Project/>");
            var file = Path.Combine(tmpDir, "Foo.cs");
            File.WriteAllText(file, "");

            var name = ProjectLoader.FindNearestProjectName(file);
            Assert.Equal("MyProject", name);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void FindNearestProjectName_InSubdirectory_WalksUp()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Core.csproj"), "<Project/>");
            var subDir = Path.Combine(tmpDir, "Services", "Auth");
            Directory.CreateDirectory(subDir);
            var file = Path.Combine(subDir, "AuthService.cs");
            File.WriteAllText(file, "");

            var name = ProjectLoader.FindNearestProjectName(file);
            Assert.Equal("Core", name);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void FindNearestProjectName_NoCsproj_ReturnsFallbackDirectory()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var subDir = Path.Combine(tmpDir, "orphan");
            Directory.CreateDirectory(subDir);
            var file = Path.Combine(subDir, "Foo.cs");
            File.WriteAllText(file, "");

            // No .csproj anywhere — fallback: immediate parent dir name
            var name = ProjectLoader.FindNearestProjectName(file);
            Assert.Equal("orphan", name);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void FindNearestProjectName_NearestCsprojWins()
    {
        // Two .csproj files in the hierarchy — nearest one should win
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Solution.csproj"), "<Project/>");
            var subDir = Path.Combine(tmpDir, "SubProject");
            Directory.CreateDirectory(subDir);
            File.WriteAllText(Path.Combine(subDir, "SubProject.csproj"), "<Project/>");
            var file = Path.Combine(subDir, "MyClass.cs");
            File.WriteAllText(file, "");

            var name = ProjectLoader.FindNearestProjectName(file);
            Assert.Equal("SubProject", name);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void FindNearestProjectName_MultipleFilesInSameProject_ReturnSameName()
    {
        var tmpDir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(tmpDir, "Shared.csproj"), "<Project/>");
            var files = new[]
            {
                Path.Combine(tmpDir, "A.cs"),
                Path.Combine(tmpDir, "B.cs"),
                Path.Combine(tmpDir, "Sub", "C.cs"),
            };
            Directory.CreateDirectory(Path.Combine(tmpDir, "Sub"));
            foreach (var f in files) File.WriteAllText(f, "");

            var names = files.Select(ProjectLoader.FindNearestProjectName).Distinct().ToList();
            Assert.Single(names);
            Assert.Equal("Shared", names[0]);
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    [Fact]
    public void FindNearestProjectName_FilesInDifferentProjects_ReturnDifferentNames()
    {
        var tmpDir = CreateTempDir();
        try
        {
            var projA = Path.Combine(tmpDir, "ProjectA");
            var projB = Path.Combine(tmpDir, "ProjectB");
            Directory.CreateDirectory(projA);
            Directory.CreateDirectory(projB);
            File.WriteAllText(Path.Combine(projA, "ProjectA.csproj"), "<Project/>");
            File.WriteAllText(Path.Combine(projB, "ProjectB.csproj"), "<Project/>");
            var fileA = Path.Combine(projA, "ClassA.cs");
            var fileB = Path.Combine(projB, "ClassB.cs");
            File.WriteAllText(fileA, "");
            File.WriteAllText(fileB, "");

            Assert.Equal("ProjectA", ProjectLoader.FindNearestProjectName(fileA));
            Assert.Equal("ProjectB", ProjectLoader.FindNearestProjectName(fileB));
        }
        finally { Directory.Delete(tmpDir, recursive: true); }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "proj-group-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
