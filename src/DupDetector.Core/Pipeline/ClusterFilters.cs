using DupDetector.Core.Detection;
using DupDetector.Core.Matching;
using DupDetector.Core.Model;

namespace DupDetector.Core.Pipeline;

/// <summary>
///     Post-detection cluster suppression.
/// </summary>
public static class ClusterFilters
{
    /// <summary>
    ///     Applies every configured suppression rule.
    /// </summary>
    /// <param name="clusters">The detected clusters.</param>
    /// <param name="settings">The exclusion rules to apply.</param>
    /// <returns>The clusters that survive every rule.</returns>
    public static IReadOnlyList<DuplicateCluster> Apply(
        IReadOnlyList<DuplicateCluster> clusters,
        DetectionSettings settings)
    {
        var outcome = new DetectionOutcome(clusters, SuppressionCounts.Empty);
        return ApplyDetailed(outcome, settings).Clusters;
    }

    /// <summary>
    ///     Applies every configured suppression rule and accumulates what each one removed.
    /// </summary>
    /// <param name="outcome">The detected clusters and any counts accumulated so far.</param>
    /// <param name="settings">The exclusion rules to apply.</param>
    /// <returns>The surviving clusters and the updated discard counts.</returns>
    public static DetectionOutcome ApplyDetailed(DetectionOutcome outcome, DetectionSettings settings)
    {
        var globs = GlobSets.Parse(settings.ExcludeClusterFileGlobs);
        var bySnippet = 0;
        var byGlob = 0;
        var byProject = 0;
        var kept = new List<DuplicateCluster>(outcome.Clusters.Count);

        foreach (var cluster in outcome.Clusters)
        {
            if (CanMatchAnySnippetPattern(cluster, settings.ExcludeSnippetPatterns))
            {
                bySnippet++;
            }
            else if (CanAllInstancesMatchGlob(cluster, globs))
            {
                byGlob++;
            }
            else if (CanAllInstancesBeInMatchingProject(cluster, settings.ExcludeProjectPatterns))
            {
                byProject++;
            }
            else
            {
                kept.Add(cluster);
            }
        }

        var uncontained = SuppressContained(kept);
        var suppressed = outcome.Suppressed with
        {
            ExcludedBySnippetPattern = outcome.Suppressed.ExcludedBySnippetPattern + bySnippet,
            ExcludedByFileGlob = outcome.Suppressed.ExcludedByFileGlob + byGlob,
            ExcludedByProjectPattern = outcome.Suppressed.ExcludedByProjectPattern + byProject,
            ContainedInLargerCluster =
                outcome.Suppressed.ContainedInLargerCluster + (kept.Count - uncontained.Count),
        };

        var result = new DetectionOutcome(uncontained, suppressed);
        return result;
    }

    /// <summary>
    ///     True when every instance belongs to a project matching one of the patterns.
    /// </summary>
    /// <param name="cluster">The cluster to test.</param>
    /// <param name="patterns">The project name fragments to match.</param>
    /// <returns><c>true</c> when the cluster is confined to matching projects.</returns>
    public static bool CanAllInstancesBeInMatchingProject(DuplicateCluster cluster, IReadOnlyList<string> patterns)
    {
        if (patterns.Count == 0)
        {
            return false;
        }

        foreach (var instance in cluster.Instances)
        {
            if (instance.Project.Name is not { } name || !CanMatchAnyPattern(name, patterns))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     True when every instance sits in a matching file, so a cluster straddling matching and
    ///     non-matching files is kept.
    /// </summary>
    /// <param name="cluster">The cluster to test.</param>
    /// <param name="globs">The globs to match file paths against.</param>
    /// <returns><c>true</c> when every instance matches.</returns>
    public static bool CanAllInstancesMatchGlob(DuplicateCluster cluster, GlobSet globs)
    {
        if (globs.Count == 0)
        {
            return false;
        }

        foreach (var instance in cluster.Instances)
        {
            if (!globs.IsMatch(instance.FilePath))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     True when any raw snippet contains one of the patterns, case-insensitively.
    /// </summary>
    /// <param name="cluster">The cluster to test.</param>
    /// <param name="patterns">The source fragments to look for.</param>
    /// <returns><c>true</c> when any snippet contains any pattern.</returns>
    public static bool CanMatchAnySnippetPattern(DuplicateCluster cluster, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            foreach (var snippet in cluster.RawSnippets)
            {
                if (snippet.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes clusters whose every instance sits inside an instance of a larger cluster.
    /// </summary>
    /// <param name="clusters">The clusters to filter.</param>
    /// <returns>The clusters that are not contained within another.</returns>
    public static IReadOnlyList<DuplicateCluster> SuppressContained(IReadOnlyList<DuplicateCluster> clusters)
    {
        var kept = new List<DuplicateCluster>(clusters.Count);
        foreach (var candidate in clusters)
        {
            if (!CanBeContained(candidate, clusters))
            {
                kept.Add(candidate);
            }
        }

        return kept;
    }

    private static bool CanBeContained(DuplicateCluster candidate, IReadOnlyList<DuplicateCluster> clusters)
    {
        foreach (var other in clusters)
        {
            if (CanContain(other, candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     True when <paramref name="outer"/> accounts for every instance of <paramref name="inner"/>.
    /// </summary>
    /// <param name="outer">The candidate enclosing cluster.</param>
    /// <param name="inner">The candidate enclosed cluster.</param>
    /// <returns><c>true</c> when every inner instance sits inside an outer instance.</returns>
    private static bool CanContain(DuplicateCluster outer, DuplicateCluster inner)
    {
        if (ReferenceEquals(outer, inner) || outer.Instances.Count < inner.Instances.Count)
        {
            return false;
        }

        if (outer.Instances.Count == inner.Instances.Count &&
            outer.Metrics.Lines <= inner.Metrics.Lines)
        {
            return false;
        }

        foreach (var instance in inner.Instances)
        {
            if (!CanHost(outer, instance))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanHost(DuplicateCluster outer, CodeInstance instance)
    {
        foreach (var host in outer.Instances)
        {
            if (string.Equals(host.FilePath, instance.FilePath, StringComparison.OrdinalIgnoreCase) &&
                host.Lines.Start <= instance.Lines.Start &&
                host.Lines.End >= instance.Lines.End)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanMatchAnyPattern(string name, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
