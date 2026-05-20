using System.Xml.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace DupDetector;

/// <summary>
/// Loads C# source documents from .sln, .slnx, .csproj, or directory paths.
/// Falls back to text-based parsing when MSBuildWorkspace is unavailable.
/// </summary>
public class ProjectLoader
{
    private readonly DetectionOptions _options;

    public ProjectLoader(DetectionOptions options)
    {
        _options = options;
    }

    public async Task<List<SourceDocument>> LoadAsync(string path)
    {
        if (File.Exists(path))
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".sln" || ext == ".slnx" || ext == ".csproj")
            {
                try
                {
                    return await LoadFromWorkspaceAsync(path);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[warn] MSBuildWorkspace failed ({ex.Message}), falling back to directory scan.");
                    var dir = Path.GetDirectoryName(path) ?? ".";
                    return LoadFromDirectory(dir);
                }
            }

            if (ext == ".cs")
            {
                return LoadFiles(new[] { path });
            }
        }

        if (Directory.Exists(path))
        {
            return LoadFromDirectory(path);
        }

        throw new ArgumentException($"Path does not exist: {path}");
    }

    private async Task<List<SourceDocument>> LoadFromWorkspaceAsync(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (ext == ".slnx")
        {
            return await LoadFromSlnxAsync(path);
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
            Console.Error.WriteLine($"[workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");

        IEnumerable<Document> documents;
        string? singleProjectName = null;

        if (ext == ".sln")
        {
            var solution = await workspace.OpenSolutionAsync(path);
            documents = solution.Projects.SelectMany(p => p.Documents);
        }
        else
        {
            var project = await workspace.OpenProjectAsync(path);
            singleProjectName = project.Name;
            documents = project.Documents;
        }

        var results = new List<SourceDocument>();
        foreach (var doc in documents)
        {
            if (doc.FilePath == null) continue;
            if (ShouldExclude(doc.FilePath)) continue;

            var sourceText = await doc.GetTextAsync();
            var syntaxTree = await doc.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var text = sourceText.ToString();
            if (!_options.IncludeGenerated && IsGeneratedFile(doc.FilePath, text)) continue;

            var projectName = singleProjectName ?? doc.Project.Name;
            results.Add(new SourceDocument(doc.FilePath, syntaxTree, text, projectName));
        }
        return results;
    }

    /// <summary>
    /// Parses a .slnx solution filter file, extracts the referenced project paths,
    /// and loads each project via MSBuildWorkspace.
    /// </summary>
    private async Task<List<SourceDocument>> LoadFromSlnxAsync(string slnxPath)
    {
        var slnxDir = Path.GetDirectoryName(Path.GetFullPath(slnxPath)) ?? ".";
        var xml = XDocument.Load(slnxPath);

        var projectPaths = xml.Descendants("Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Path.GetFullPath(Path.Combine(slnxDir, p!)))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (projectPaths.Count == 0)
        {
            Console.Error.WriteLine("[warn] No projects found in .slnx file.");
            return new List<SourceDocument>();
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
            Console.Error.WriteLine($"[workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");

        var results = new List<SourceDocument>();
        foreach (var projectPath in projectPaths)
        {
            try
            {
                var project = await workspace.OpenProjectAsync(projectPath);
                foreach (var doc in project.Documents)
                {
                    if (doc.FilePath == null) continue;
                    if (ShouldExclude(doc.FilePath)) continue;

                    var sourceText = await doc.GetTextAsync();
                    var syntaxTree = await doc.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var text = sourceText.ToString();
                    if (!_options.IncludeGenerated && IsGeneratedFile(doc.FilePath, text)) continue;

                    results.Add(new SourceDocument(doc.FilePath, syntaxTree, text, project.Name));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[warn] Failed to load project '{projectPath}': {ex.Message}");
            }
        }

        return results;
    }

    private List<SourceDocument> LoadFromDirectory(string dir)
    {
        var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f))
            .ToList();
        return LoadFiles(files);
    }

    private List<SourceDocument> LoadFiles(IEnumerable<string> files)
    {
        var results = new List<SourceDocument>();
        foreach (var file in files)
        {
            try
            {
                var text = File.ReadAllText(file);
                if (!_options.IncludeGenerated && IsGeneratedFile(file, text)) continue;

                var syntaxTree = CSharpSyntaxTree.ParseText(text, path: file);
                var projectName = FindNearestProjectName(file);
                results.Add(new SourceDocument(file, syntaxTree, text, projectName));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[warn] Failed to read {file}: {ex.Message}");
            }
        }
        return results;
    }

    /// <summary>
    /// Walks up the directory tree from <paramref name="filePath"/> to find the nearest
    /// .csproj file. Returns the project name (filename without extension), or the
    /// immediate parent directory name if no .csproj is found.
    /// </summary>
    internal static string FindNearestProjectName(string filePath)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        while (dir != null)
        {
            var csproj = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
            if (csproj != null)
                return Path.GetFileNameWithoutExtension(csproj);
            dir = Path.GetDirectoryName(dir);
        }
        var parent = Path.GetDirectoryName(Path.GetFullPath(filePath));
        return parent != null ? Path.GetFileName(parent) : ".";
    }

    private bool ShouldExclude(string filePath)
    {
        foreach (var pattern in _options.Exclude)
        {
            if (GlobMatch(pattern, filePath)) return true;
        }
        return false;
    }

    private static bool IsGeneratedFile(string filePath, string content)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.Contains(".Designer.", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)) return true;
        if (content.Contains("<auto-generated>", StringComparison.OrdinalIgnoreCase)) return true;
        if (content.Contains("[GeneratedCode", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Simple glob matching supporting * and ? wildcards.</summary>
    private static bool GlobMatch(string pattern, string filePath)
    {
        filePath = filePath.Replace('\\', '/');
        pattern = pattern.Replace('\\', '/');
        return GlobMatchCore(pattern, filePath);
    }

    private static bool GlobMatchCore(string pattern, string input)
    {
        int p = 0, i = 0;
        int starP = -1, starI = 0;

        while (i < input.Length)
        {
            if (p < pattern.Length && (pattern[p] == '?' || pattern[p] == input[i]))
            {
                p++; i++;
            }
            else if (p < pattern.Length && pattern[p] == '*')
            {
                starP = p++;
                starI = i;
            }
            else if (starP != -1)
            {
                p = starP + 1;
                i = ++starI;
            }
            else return false;
        }

        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }
}
