using System.Xml.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace DupDetector;

/// <summary>
/// Carries file discovery statistics from a single <see cref="ProjectLoader.LoadDetailedAsync"/> call.
/// </summary>
public record LoadStats(int DiscoveredFiles, int ExcludedFiles, string DiscoveryMode);

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
        var (docs, _) = await LoadDetailedAsync(path);
        return docs;
    }

    /// <summary>
    /// Loads source documents and returns discovery statistics alongside the documents.
    /// </summary>
    public async Task<(List<SourceDocument> Documents, LoadStats Stats)> LoadDetailedAsync(string path)
    {
        // Normalize to absolute path so all stored file paths are absolute regardless of CWD.
        path = Path.GetFullPath(path);

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

    private async Task<(List<SourceDocument>, LoadStats)> LoadFromWorkspaceAsync(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        if (ext == ".slnx")
        {
            return await LoadFromSlnxAsync(path);
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            // Suppress transitive-reference duplicate warnings — they are cosmetic and
            // do not affect analysis results (files are already deduplicated by path).
            if (e.Diagnostic.Message.Contains("already part of the workspace", StringComparison.OrdinalIgnoreCase))
                return;
            Console.Error.WriteLine($"[workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
        };

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
        int discovered = 0, excluded = 0;
        foreach (var doc in documents)
        {
            if (doc.FilePath == null) continue;
            discovered++;
            if (ShouldExclude(doc.FilePath)) { excluded++; continue; }

            var sourceText = await doc.GetTextAsync();
            var syntaxTree = await doc.GetSyntaxTreeAsync();
            if (syntaxTree == null) continue;

            var text = sourceText.ToString();
            if (!_options.IncludeGenerated && IsGeneratedFile(doc.FilePath, text)) { excluded++; continue; }

            var projectName = singleProjectName ?? doc.Project.Name;
            results.Add(new SourceDocument(doc.FilePath, syntaxTree, text, projectName));
        }
        return (results, new LoadStats(discovered, excluded, "workspace"));
    }

    /// <summary>
    /// Parses a .slnx solution filter file, extracts the referenced project paths,
    /// and loads each project via MSBuildWorkspace.
    /// </summary>
    private async Task<(List<SourceDocument>, LoadStats)> LoadFromSlnxAsync(string slnxPath)
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
            return (new List<SourceDocument>(), new LoadStats(0, 0, "workspace"));
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            if (e.Diagnostic.Message.Contains("already part of the workspace", StringComparison.OrdinalIgnoreCase))
                return;
            Console.Error.WriteLine($"[workspace] {e.Diagnostic.Kind}: {e.Diagnostic.Message}");
        };

        var results = new List<SourceDocument>();
        int discovered = 0, excluded = 0;
        foreach (var projectPath in projectPaths)
        {
            // Proactively skip projects already loaded as transitive dependencies.
            if (workspace.CurrentSolution.Projects.Any(p =>
                string.Equals(p.FilePath, projectPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            try
            {
                var project = await workspace.OpenProjectAsync(projectPath);
                foreach (var doc in project.Documents)
                {
                    if (doc.FilePath == null) continue;
                    discovered++;
                    if (ShouldExclude(doc.FilePath)) { excluded++; continue; }

                    var sourceText = await doc.GetTextAsync();
                    var syntaxTree = await doc.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var text = sourceText.ToString();
                    if (!_options.IncludeGenerated && IsGeneratedFile(doc.FilePath, text)) { excluded++; continue; }

                    results.Add(new SourceDocument(doc.FilePath, syntaxTree, text, project.Name));
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already part of the workspace", StringComparison.OrdinalIgnoreCase))
                    continue;
                Console.Error.WriteLine($"[warn] Failed to load project '{projectPath}': {ex.Message}");
            }
        }

        return (results, new LoadStats(discovered, excluded, "workspace"));
    }

    private (List<SourceDocument>, LoadStats) LoadFromDirectory(string dir)
    {
        var allFiles = Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories).ToList();
        int discovered = allFiles.Count;
        int excluded = allFiles.Count(f => ShouldExclude(f));
        var kept = allFiles.Where(f => !ShouldExclude(f)).ToList();
        var (docs, _) = LoadFiles(kept);
        // Subtract any additionally excluded by generated-file filter
        int genExcluded = kept.Count - docs.Count;
        return (docs, new LoadStats(discovered, excluded + genExcluded, "filesystem"));
    }

    private (List<SourceDocument>, LoadStats) LoadFiles(IEnumerable<string> files)
    {
        var results = new List<SourceDocument>();
        int discovered = 0, excluded = 0;
        foreach (var file in files)
        {
            discovered++;
            try
            {
                var absoluteFile = Path.GetFullPath(file);
                var text = File.ReadAllText(absoluteFile);
                if (!_options.IncludeGenerated && IsGeneratedFile(absoluteFile, text)) { excluded++; continue; }

                var syntaxTree = CSharpSyntaxTree.ParseText(text, path: absoluteFile);
                var projectName = FindNearestProjectName(absoluteFile);
                results.Add(new SourceDocument(absoluteFile, syntaxTree, text, projectName));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[warn] Failed to read {file}: {ex.Message}");
                excluded++;
            }
        }
        return (results, new LoadStats(discovered, excluded, "filesystem"));
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
        if (IsArtifactPath(filePath)) return true;
        foreach (var pattern in _options.Exclude)
        {
            if (GlobMatch(pattern, filePath)) return true;
        }
        return false;
    }

    /// <summary>Internal helper exposed for unit testing.</summary>
    internal bool IsExcluded(string filePath) => ShouldExclude(filePath);

    /// <summary>Internal helper exposed for unit testing.</summary>
    internal List<SourceDocument> LoadFromDirectoryInternal(string dir)
    {
        var (docs, _) = LoadFromDirectory(dir);
        return docs;
    }

    /// <summary>
    /// Returns <c>true</c> when the file resides inside a build-artifact directory
    /// (<c>obj</c> or <c>bin</c>). These directories contain auto-generated files
    /// that should never appear in duplication analysis output.
    /// </summary>
    private static bool IsArtifactPath(string filePath)
    {
        var normalized = filePath.Replace('\\', '/');
        foreach (var segment in normalized.Split('/'))
        {
            if (segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase))
                return true;
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
