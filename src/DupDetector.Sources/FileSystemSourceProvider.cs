using DupDetector.Core.Matching;
using DupDetector.Core.Model;

namespace DupDetector.Sources;

/// <summary>
/// Loads C# files from a directory or a single file.
/// </summary>
/// <remarks>
/// Enumeration skips unreadable directories and reparse points, so neither a permission-denied
/// folder nor a directory junction can abort a scan or send it into runaway recursion.
/// </remarks>
public sealed class FileSystemSourceProvider : ISourceProvider
{
    private static readonly EnumerationOptions Enumeration = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        MatchCasing = MatchCasing.CaseInsensitive,
    };

    private static readonly string[] ArtifactDirectories = ["obj", "bin"];

    public SourceLoadResult Load(string path, DetectionSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(settings);

        var full = Path.GetFullPath(path);

        if (File.Exists(full))
        {
            // A path that exists as a file always has a parent directory.
            var root = Path.GetDirectoryName(full)!;
            return Read([full], root, settings, cancellationToken);
        }

        if (!Directory.Exists(full))
        {
            return SourceLoadResult.Empty with
            {
                Diagnostics = [SourceDiagnostic.Error($"Path does not exist: {full}", full)],
            };
        }

        return Read(Directory.EnumerateFiles(full, "*.cs", Enumeration), full, settings, cancellationToken);
    }

    private static SourceLoadResult Read(
        IEnumerable<string> files,
        string root,
        DetectionSettings settings,
        CancellationToken cancellationToken)
    {
        var excludes = GlobSet.Parse(settings.ExcludeFileGlobs);
        var resolver = new ProjectNameResolver();
        var units = new List<SourceUnit>();
        var diagnostics = new List<SourceDiagnostic>();
        var discovered = 0;
        var excluded = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            discovered++;

            var relative = Relative(root, file);
            if (IsArtifact(relative) || excludes.IsMatch(file))
            {
                excluded++;
                continue;
            }

            var project = resolver.Resolve(file);
            var isTestFile = TestFileClassifier.IsTestFile(relative, project);
            if (settings.ExcludeTestFiles && isTestFile)
            {
                // Excluded from the whole pipeline, not merely hidden from the listings, so every
                // total downstream describes production code only.
                excluded++;
                continue;
            }

            string text;
            try
            {
                text = SourceDecoder.Decode(File.ReadAllBytes(file));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add(SourceDiagnostic.Warning($"Could not read file: {exception.Message}", file));
                excluded++;
                continue;
            }

            if (GeneratedFileDetector.IsGenerated(file, text))
            {
                excluded++;
                continue;
            }

            var tree = SourceParser.Parse(text, file);
            if (SourceParser.DescribeParseFailures(tree, file) is { } failure)
            {
                diagnostics.Add(failure);
            }

            units.Add(new SourceUnit(file, relative, text, tree, project, isTestFile));
        }

        return new SourceLoadResult(
            units,
            new DiscoveryStats(discovered, excluded, DiscoveryMode.FileSystem),
            diagnostics);
    }

    internal static string Relative(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        return GlobPattern.Normalize(relative);
    }

    internal static bool IsArtifact(string relativePath) =>
        relativePath.Split('/').Any(segment => ArtifactDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase));
}
