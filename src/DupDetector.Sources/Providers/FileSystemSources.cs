using DupDetector.Core.Matching;

using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

namespace DupDetector.Sources.Providers;

/// <summary>
///     Helpers for the file-system source provider.
/// </summary>
public static class FileSystemSources
{
    private static readonly string[] ArtifactDirectories;

    /// <summary>
    ///     
    /// </summary>
    public static EnumerationOptions Enumeration { get; }

    static FileSystemSources()
    {
        ArtifactDirectories = ["obj", "bin"];

        Enumeration = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
            MatchCasing = MatchCasing.CaseInsensitive,
        };
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="relativePath"></param>
    public static bool IsArtifact(string relativePath)
    {
        foreach (var segment in relativePath.Split('/'))
        {
            foreach (var directory in ArtifactDirectories)
            {
                if (string.Equals(segment, directory, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Reads every candidate file, counting the ones the settings excluded.
    /// </summary>
    /// <returns></returns>
    /// <param name="files"></param>
    /// <param name="root"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    public static SourceLoadResult Read(
        IEnumerable<string> files,
        string root,
        DetectionSettings settings,
        CancellationToken cancellationToken)
    {
        var scope = new HarvestScope
        {
            Root = root,
            Settings = settings,
            Excludes = GlobSets.Parse(settings.ExcludeFileGlobs),
        };

        var resolver = new ProjectNameResolver();
        var units = new List<SourceUnit>();
        var diagnostics = new List<SourceDiagnostic>();
        var discovered = 0;
        var excluded = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            discovered++;

            var harvested = Harvest(file, resolver, scope);
            if (harvested.Diagnostic is { } failure)
            {
                diagnostics.Add(failure);
            }

            if (harvested.Unit is { } unit)
            {
                units.Add(unit);
            }
            else
            {
                excluded++;
            }
        }

        var discoveryStats = new DiscoveryStats
        {
            Discovered = discovered,
            Excluded = excluded,
            Mode = DiscoveryMode.FileSystem
        };

        var sourceLoadResult = new SourceLoadResult
        {
            Units = units,
            Stats = discoveryStats,
            Diagnostics = diagnostics,
        };
        return sourceLoadResult;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="root"></param>
    /// <param name="file"></param>
    public static string Relative(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        return GlobPatterns.Normalize(relative);
    }

    private static HarvestedSource Harvest(string file, ProjectNameResolver resolver, HarvestScope scope)
    {
        var relative = Relative(scope.Root, file);
        if (IsArtifact(relative) || scope.Excludes.IsMatch(file))
        {
            return HarvestedSource.Skipped;
        }

        var project = resolver.Resolve(file);
        var isTestFile = TestFileClassifier.IsTestFile(relative, project);
        if (scope.Settings.IsExcludeTestFiles && isTestFile)
        {
            return HarvestedSource.Skipped;
        }

        string text;
        try
        {
            text = SourceDecoder.Decode(File.ReadAllBytes(file));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var unreadable = new HarvestedSource
            {
                Diagnostic = SourceDiagnostics.Warning($"Could not read file: {exception.Message}", file),
            };

            return unreadable;
        }

        if (GeneratedFileDetector.IsGenerated(file, text))
        {
            return HarvestedSource.Skipped;
        }

        var tree = SourceParser.Parse(text, file);
        var origin = new SourceOrigin(relative, project, isTestFile);
        var sourceUnit = new SourceUnit(file, text, tree, origin);
        var harvested = new HarvestedSource
        {
            Unit = sourceUnit,
            Diagnostic = SourceParser.DescribeParseFailures(tree, file),
        };

        return harvested;
    }
}
