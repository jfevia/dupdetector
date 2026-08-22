using DupDetector.Core.Detection;
using DupDetector.Core.Matching;
using DupDetector.Core.Model;

namespace DupDetector.Core.Pipeline;

/// <summary>
/// Post-detection cluster suppression.
/// </summary>
/// <remarks>
/// Each rule is a named, individually testable predicate rather than a lambda buried in the
/// pipeline, so the production expression is the one under test.
/// </remarks>
public static class ClusterFilters
{
    /// <summary>True when any raw snippet contains one of the patterns, case-insensitively.</summary>
    public static bool MatchesAnySnippetPattern(DuplicateCluster cluster, IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(patterns);

        return patterns.Any(pattern =>
            cluster.RawSnippets.Any(snippet => snippet.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// True when every instance sits in a matching file. A cluster straddling matching and
    /// non-matching files is kept, because it still represents real cross-boundary duplication.
    /// </summary>
    public static bool AllInstancesMatchGlob(DuplicateCluster cluster, GlobSet globs)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(globs);

        return globs.Count > 0 && cluster.Instances.All(instance => globs.IsMatch(instance.FilePath));
    }

    /// <summary>True when every instance belongs to a project matching one of the patterns.</summary>
    public static bool AllInstancesInMatchingProject(DuplicateCluster cluster, IReadOnlyList<string> patterns)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(patterns);

        return patterns.Count > 0 && cluster.Instances.All(instance =>
            instance.Project.Name is { } name &&
            patterns.Any(pattern => name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Removes clusters whose every instance sits inside an instance of a larger cluster.
    /// </summary>
    /// <remarks>
    /// When a whole type is duplicated, its members are duplicated with it. Reporting both describes
    /// the same code twice and splits one refactoring into several findings, so only the widest
    /// enclosing cluster is kept.
    /// </remarks>
    public static IReadOnlyList<DuplicateCluster> SuppressContained(IReadOnlyList<DuplicateCluster> clusters)
    {
        ArgumentNullException.ThrowIfNull(clusters);

        return [.. clusters.Where(candidate => !clusters.Any(other => Contains(other, candidate)))];
    }

    /// <summary>
    /// True when <paramref name="outer"/> accounts for every instance of <paramref name="inner"/>.
    /// </summary>
    private static bool Contains(DuplicateCluster outer, DuplicateCluster inner)
    {
        if (ReferenceEquals(outer, inner) || outer.Instances.Count < inner.Instances.Count)
        {
            return false;
        }

        // Equal-sized clusters would suppress each other; the wider one wins, ties are kept.
        if (outer.Instances.Count == inner.Instances.Count &&
            outer.Metrics.Lines <= inner.Metrics.Lines)
        {
            return false;
        }

        return inner.Instances.All(instance => outer.Instances.Any(host =>
            string.Equals(host.FilePath, instance.FilePath, StringComparison.OrdinalIgnoreCase) &&
            host.Lines.Start <= instance.Lines.Start &&
            host.Lines.End >= instance.Lines.End));
    }

    /// <summary>Applies every configured suppression rule.</summary>
    public static IReadOnlyList<DuplicateCluster> Apply(
        IReadOnlyList<DuplicateCluster> clusters,
        DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(clusters);

        return ApplyDetailed(new DetectionOutcome(clusters, SuppressionCounts.Empty), settings).Clusters;
    }

    /// <summary>Applies every configured suppression rule and accumulates what each one removed.</summary>
    public static DetectionOutcome ApplyDetailed(DetectionOutcome outcome, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(settings);

        var globs = GlobSet.Parse(settings.ExcludeClusterFileGlobs);
        var bySnippet = 0;
        var byGlob = 0;
        var byProject = 0;
        var kept = new List<DuplicateCluster>(outcome.Clusters.Count);

        foreach (var cluster in outcome.Clusters)
        {
            if (MatchesAnySnippetPattern(cluster, settings.ExcludeSnippetPatterns))
            {
                bySnippet++;
            }
            else if (AllInstancesMatchGlob(cluster, globs))
            {
                byGlob++;
            }
            else if (AllInstancesInMatchingProject(cluster, settings.ExcludeProjectPatterns))
            {
                byProject++;
            }
            else
            {
                kept.Add(cluster);
            }
        }

        var uncontained = SuppressContained(kept);

        return new DetectionOutcome(
            uncontained,
            outcome.Suppressed with
            {
                ExcludedBySnippetPattern = outcome.Suppressed.ExcludedBySnippetPattern + bySnippet,
                ExcludedByFileGlob = outcome.Suppressed.ExcludedByFileGlob + byGlob,
                ExcludedByProjectPattern = outcome.Suppressed.ExcludedByProjectPattern + byProject,
                ContainedInLargerCluster =
                    outcome.Suppressed.ContainedInLargerCluster + (kept.Count - uncontained.Count),
            });
    }
}
