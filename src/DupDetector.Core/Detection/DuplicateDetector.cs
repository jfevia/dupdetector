using System.Security.Cryptography;
using System.Text;
using DupDetector.Core.Model;

namespace DupDetector.Core.Detection;

/// <summary>
/// Groups code blocks into duplicate clusters.
/// </summary>
// Exact copies group by hash first; whatever remains goes to the similarity join when it is enabled.
public static class DuplicateDetector
{
    public static IReadOnlyList<DuplicateCluster> Detect(IReadOnlyList<CodeBlock> blocks, DetectionSettings settings) =>
        Detect(blocks, settings, CliqueBudget.Default);

    public static IReadOnlyList<DuplicateCluster> Detect(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget) => DetectDetailed(blocks, settings, budget).Clusters;

    /// <summary>
    /// Detects duplicates and reports how many candidates each threshold discarded.
    /// </summary>
    public static DetectionOutcome DetectDetailed(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget)
    {
        ArgumentNullException.ThrowIfNull(blocks);
        ArgumentNullException.ThrowIfNull(settings);

        var clusters = new List<DuplicateCluster>();
        var claimed = new HashSet<int>();
        var tally = new Tally();

        AddExactClusters(blocks, settings, clusters, claimed, tally);

        if (settings.Similarity < 1.0)
        {
            AddNearDuplicateClusters(blocks, settings, budget, clusters, claimed, tally);
        }

        return new DetectionOutcome(
            [.. clusters
                .OrderByDescending(cluster => cluster.Metrics.RemovableLines)
                .ThenByDescending(cluster => cluster.Metrics.Occurrences)
                .ThenBy(cluster => cluster.Id, StringComparer.Ordinal)],
            tally.ToCounts());
    }

    /// <summary>Running totals of what each threshold rejected.</summary>
    // Keyed on cluster id: a group rejected by the exact pass re-forms in the near pass and would count twice.
    private sealed class Tally
    {
        private readonly Dictionary<string, HashSet<string>> _byReason = [];

        internal void Add(string reason, string clusterId)
        {
            if (!_byReason.TryGetValue(reason, out var ids))
            {
                ids = new HashSet<string>(StringComparer.Ordinal);
                _byReason[reason] = ids;
            }

            ids.Add(clusterId);
        }

        internal SuppressionCounts ToCounts() => new()
        {
            BelowFileSpread = Count(nameof(SuppressionCounts.BelowFileSpread)),
            BelowProjectSpread = Count(nameof(SuppressionCounts.BelowProjectSpread)),
            AboveFileSpread = Count(nameof(SuppressionCounts.AboveFileSpread)),
            AboveOccurrences = Count(nameof(SuppressionCounts.AboveOccurrences)),
        };

        private int Count(string reason) => _byReason.TryGetValue(reason, out var ids) ? ids.Count : 0;
    }

    private static void AddExactClusters(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        List<DuplicateCluster> clusters,
        HashSet<int> claimed,
        Tally tally)
    {
        var byHash = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (var index = 0; index < blocks.Count; index++)
        {
            if (!byHash.TryGetValue(blocks[index].Hash, out var members))
            {
                members = [];
                byHash[blocks[index].Hash] = members;
            }

            members.Add(index);
        }

        foreach (var members in byHash.Values.Where(group => group.Count >= 2))
        {
            var cluster = Build([.. members.Select(index => blocks[index])], settings, cohesive: true);
            if (!MeetsMinimums(cluster, settings))
            {
                Record(cluster, settings, tally);

                // Left unclaimed on purpose: these blocks may still form a wider cluster below.
                continue;
            }

            clusters.Add(cluster);
            claimed.UnionWith(members);
        }
    }

    private static void AddNearDuplicateClusters(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget,
        List<DuplicateCluster> clusters,
        HashSet<int> claimed,
        Tally tally)
    {
        var remaining = Enumerable.Range(0, blocks.Count).Where(index => !claimed.Contains(index)).ToArray();
        if (remaining.Length < 2)
        {
            return;
        }

        var interner = new TokenInterner();
        var multisets = remaining.Select(index => TokenMultiset.Create(blocks[index].NormalizedText, interner)).ToArray();
        var pairs = SimilarityJoin.FindPairs(multisets, settings.Similarity);

        foreach (var group in CliqueGrouper.Group(remaining.Length, pairs, budget))
        {
            var members = group.Members.Select(position => blocks[remaining[position]]).ToArray();
            var cluster = Build(members, settings, group.IsCohesive);
            if (MeetsMinimums(cluster, settings) && WithinMaximums(cluster, settings))
            {
                clusters.Add(cluster);
            }
            else
            {
                Record(cluster, settings, tally);
            }
        }
    }

    /// <summary>Attributes a rejected cluster to the threshold that rejected it.</summary>
    private static void Record(DuplicateCluster cluster, DetectionSettings settings, Tally tally)
    {
        if (cluster.Metrics.FileSpread < settings.MinFileSpread)
        {
            tally.Add(nameof(SuppressionCounts.BelowFileSpread), cluster.Id);
        }
        else if (cluster.Metrics.ProjectSpreadKnown && cluster.Metrics.ProjectSpread < settings.MinProjectSpread)
        {
            tally.Add(nameof(SuppressionCounts.BelowProjectSpread), cluster.Id);
        }
        else if (settings.MaxFileSpread > 0 && cluster.Metrics.FileSpread > settings.MaxFileSpread)
        {
            tally.Add(nameof(SuppressionCounts.AboveFileSpread), cluster.Id);
        }
        else
        {
            // Reached only for a cluster that already failed a threshold, and the three checks above
            // are the exact negation of every other one, so the copy limit is what is left.
            tally.Add(nameof(SuppressionCounts.AboveOccurrences), cluster.Id);
        }
    }

    private static bool MeetsMinimums(DuplicateCluster cluster, DetectionSettings settings) =>
        cluster.Metrics.FileSpread >= settings.MinFileSpread &&
        // The project minimum is only enforceable when every instance knows its project. When it is
        // not, the constraint is skipped and the pipeline warns, rather than either fabricating a
        // spread from file counts or silently emptying the report.
        (!cluster.Metrics.ProjectSpreadKnown || cluster.Metrics.ProjectSpread >= settings.MinProjectSpread);

    /// <summary>
    /// Applied to near-duplicate clusters only.
    /// </summary>
    // A precision guard for the fuzzy join only: an exact cluster shares one hash and cannot be a false positive.
    // At the default limit of 20 files this would discard a class copied verbatim across 25.
    private static bool WithinMaximums(DuplicateCluster cluster, DetectionSettings settings) =>
        (settings.MaxFileSpread == 0 || cluster.Metrics.FileSpread <= settings.MaxFileSpread) &&
        (settings.MaxOccurrences == 0 || cluster.Metrics.Occurrences <= settings.MaxOccurrences);

    internal static DuplicateCluster Build(IReadOnlyList<CodeBlock> members, DetectionSettings settings, bool cohesive)
    {
        var ordered = members
            .OrderBy(block => block.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(block => block.Lines.Start)
            .ToArray();

        var instances = Array.ConvertAll(ordered, block => block.ToInstance());
        var lines = (int)Math.Round(ordered.Average(block => block.Lines.Count), MidpointRounding.AwayFromZero);
        var fileSpread = instances.DistinctBy(instance => instance.FilePath, StringComparer.OrdinalIgnoreCase).Count();
        var projectSpreadKnown = Array.TrueForAll(instances, instance => instance.Project.IsKnown);

        // No fallback to file spread: an unknown project must never satisfy a project-spread
        // requirement it was not actually measured against.
        var projectSpread = instances
            .Where(instance => instance.Project.IsKnown)
            .Select(instance => instance.Project)
            .Distinct()
            .Count();

        var isExact = ordered.DistinctBy(block => block.Hash, StringComparer.Ordinal).Count() == 1;

        return new DuplicateCluster
        {
            Id = BuildId(instances),
            Instances = instances,
            Metrics = new ClusterMetrics(lines, instances.Length, fileSpread, projectSpread, projectSpreadKnown),
            NormalizedSnippet = ordered[0].NormalizedText,
            RawSnippets = Array.ConvertAll(ordered, block => block.RawText),
            IsCohesive = cohesive,
            IsProductionDuplicate =
                isExact &&
                projectSpread >= 2 &&
                lines >= settings.MinProductionDuplicateLines &&
                Array.Exists(instances, instance => !instance.IsTestFile),
        };
    }

    /// <summary>
    /// Derives the cluster id from the sorted member hashes and sizes, so renaming or adding a file
    /// cannot change the identity of unchanged duplicated code, while two overlapping groups that
    /// share a member stay distinguishable.
    /// </summary>
    private static string BuildId(IReadOnlyList<CodeInstance> instances)
    {
        var material = string.Join(
            '\n',
            instances.Select(instance => $"{instance.Hash}:{instance.Lines.Count}").Order(StringComparer.Ordinal));

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return $"dup-{Convert.ToHexString(digest)[..12].ToLowerInvariant()}";
    }
}
