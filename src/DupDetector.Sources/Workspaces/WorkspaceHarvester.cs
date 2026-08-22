using DupDetector.Core.Matching;

using DupDetector.Core.Model;

using DupDetector.Core.Model.Reporting;

using DupDetector.Sources.Providers;

using Microsoft.CodeAnalysis;

namespace DupDetector.Sources.Workspaces;

/// <summary>
///     Turns loaded projects into source units.
/// </summary>
public static class WorkspaceHarvester
{
    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="projects"></param>
    /// <param name="root"></param>
    /// <param name="settings"></param>
    /// <param name="cancellationToken"></param>
    public static SourceLoadResult Collect(
        IReadOnlyList<Project> projects,
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

        var units = new List<SourceUnit>();
        var diagnostics = new List<SourceDiagnostic>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var discovered = 0;
        var excluded = 0;

        foreach (var project in projects)
        {
            var identity = ProjectIdentities.Named(project.Name);

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.FilePath is not { } documentPath || !seen.Add(documentPath))
                {
                    continue;
                }

                discovered++;
                var harvested = Harvest(document, identity, scope, cancellationToken);
                if (harvested.Unit is null)
                {
                    excluded++;
                }

                Record(harvested, units, diagnostics);
            }
        }

        var discoveryStats = new DiscoveryStats
        {
            Discovered = discovered,
            Excluded = excluded,
            Mode = DiscoveryMode.Workspace
        };

        var sourceLoadResult = new SourceLoadResult
        {
            Units = units,
            Stats = discoveryStats,
            Diagnostics = diagnostics,
        };
        return sourceLoadResult;
    }

    private static HarvestedSource Harvest(
        Document document,
        ProjectIdentity identity,
        HarvestScope scope,
        CancellationToken cancellationToken)
    {
        var documentPath = document.FilePath!;
        var relative = FileSystemSources.Relative(scope.Root, documentPath);
        if (FileSystemSources.IsArtifact(relative) || scope.Excludes.IsMatch(documentPath))
        {
            return HarvestedSource.Skipped;
        }

        var isTestFile = TestFileClassifier.IsTestFile(relative, identity);
        if (scope.Settings.IsExcludeTestFiles && isTestFile)
        {
            return HarvestedSource.Skipped;
        }

        var text = document.GetTextAsync(cancellationToken).GetAwaiter().GetResult().ToString();
        if (GeneratedFileDetector.IsGenerated(documentPath, text))
        {
            return HarvestedSource.Skipped;
        }

        var tree = SourceParser.Parse(text, documentPath);
        var origin = new SourceOrigin(relative, identity, isTestFile);
        var sourceUnit = new SourceUnit(documentPath, text, tree, origin);
        var harvested = new HarvestedSource
        {
            Unit = sourceUnit,
            Diagnostic = SourceParser.DescribeParseFailures(tree, documentPath),
        };

        return harvested;
    }

    private static void Record(
        HarvestedSource harvested,
        List<SourceUnit> units,
        List<SourceDiagnostic> diagnostics)
    {
        if (harvested.Diagnostic is { } failure)
        {
            diagnostics.Add(failure);
        }

        if (harvested.Unit is { } unit)
        {
            units.Add(unit);
        }
    }
}
