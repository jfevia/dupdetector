using DupDetector.Core.Model;
using System.Security.Cryptography;
using System.Text;

namespace DupDetector.Core.Detection;

/// <summary>
///     Groups code blocks into duplicate clusters.
/// </summary>
public static class DuplicateDetector
{
    /// <summary>
    ///     Builds a cluster from blocks already known to belong together.
    /// </summary>
    /// <param name="members">The blocks forming the cluster; at least one is required.</param>
    /// <param name="settings">The thresholds that decide whether the cluster is production debt.</param>
    /// <param name="cohesive">Whether every member is similar to every other member.</param>
    /// <returns>The cluster, with its instances ordered by file and then by start line.</returns>
    public static DuplicateCluster Build(IReadOnlyList<CodeBlock> members, DetectionSettings settings, bool cohesive)
    {
        var ordered = new List<CodeBlock>(members);
        ordered.Sort(CompareByLocation);

        var gathered = Gather(ordered);
        var lines = (int)Math.Round((double)gathered.TotalLines / ordered.Count, MidpointRounding.AwayFromZero);
        var spread = new ClusterSpread(
            gathered.Files.Count,
            gathered.Projects.Count,
            gathered.IsProjectSpreadKnown);

        var metrics = new ClusterMetrics(lines, gathered.Instances.Length, spread);

        var isProductionDuplicate =
            gathered.Hashes.Count == 1 &&
            gathered.Projects.Count >= 2 &&
            lines >= settings.MinProductionDuplicateLines &&
            gathered.HasProductionInstance;

        var cluster = new DuplicateCluster
        {
            Id = BuildId(gathered.Instances),
            Instances = gathered.Instances,
            Metrics = metrics,
            NormalizedSnippet = ordered[0].NormalizedText,
            RawSnippets = gathered.RawSnippets,
            IsCohesive = cohesive,
            IsProductionDuplicate = isProductionDuplicate,
        };

        return cluster;
    }

    /// <summary>
    ///     Detects duplicate clusters using the default grouping budget.
    /// </summary>
    /// <param name="blocks">The blocks to search for duplication.</param>
    /// <param name="settings">The thresholds that decide which clusters are reported.</param>
    /// <returns>The reported clusters, most severe first.</returns>
    public static IReadOnlyList<DuplicateCluster> Detect(IReadOnlyList<CodeBlock> blocks, DetectionSettings settings)
    {
        return Detect(blocks, settings, CliqueBudget.Default);
    }

    /// <summary>
    ///     Detects duplicate clusters under an explicit grouping budget.
    /// </summary>
    /// <param name="blocks">The blocks to search for duplication.</param>
    /// <param name="settings">The thresholds that decide which clusters are reported.</param>
    /// <param name="budget">The ceiling on clique enumeration work.</param>
    /// <returns>The reported clusters, most severe first.</returns>
    public static IReadOnlyList<DuplicateCluster> Detect(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget)
    {
        return DetectDetailed(blocks, settings, budget).Clusters;
    }

    /// <summary>
    ///     Detects duplicates and reports how many candidates each threshold discarded.
    /// </summary>
    /// <param name="blocks">The blocks to search for duplication.</param>
    /// <param name="settings">The thresholds that decide which clusters are reported.</param>
    /// <param name="budget">The ceiling on clique enumeration work.</param>
    /// <returns>The reported clusters together with the per-threshold discard counts.</returns>
    public static DetectionOutcome DetectDetailed(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget)
    {
        var accumulator = new Accumulator();

        AddExactClusters(blocks, settings, accumulator);

        if (settings.Similarity < 1.0)
        {
            AddNearDuplicateClusters(blocks, settings, budget, accumulator);
        }

        accumulator.Clusters.Sort(CompareBySeverity);
        var outcome = new DetectionOutcome(accumulator.Clusters, accumulator.Tally.ToCounts());
        return outcome;
    }

    private static void AddExactClusters(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        Accumulator accumulator)
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

        foreach (var members in byHash.Values)
        {
            if (members.Count < 2)
            {
                continue;
            }

            var group = new List<CodeBlock>(members.Count);
            foreach (var index in members)
            {
                group.Add(blocks[index]);
            }

            var cluster = Build(group, settings, cohesive: true);
            if (!CanMeetMinimums(cluster, settings))
            {
                Record(cluster, settings, accumulator.Tally);
                continue;
            }

            accumulator.Clusters.Add(cluster);
            accumulator.Claimed.UnionWith(members);
        }
    }

    private static void AddNearDuplicateClusters(
        IReadOnlyList<CodeBlock> blocks,
        DetectionSettings settings,
        CliqueBudget budget,
        Accumulator accumulator)
    {
        var remaining = new List<int>();
        for (var index = 0; index < blocks.Count; index++)
        {
            if (!accumulator.Claimed.Contains(index))
            {
                remaining.Add(index);
            }
        }

        if (remaining.Count < 2)
        {
            return;
        }

        var interner = new TokenInterner();
        var multisets = new TokenMultiset[remaining.Count];
        for (var index = 0; index < remaining.Count; index++)
        {
            multisets[index] = TokenMultisets.Create(blocks[remaining[index]].NormalizedText, interner);
        }

        var pairs = SimilarityJoin.FindPairs(multisets, settings.Similarity);

        foreach (var group in CliqueGrouper.Group(remaining.Count, pairs, budget))
        {
            var members = new List<CodeBlock>(group.Members.Count);
            foreach (var position in group.Members)
            {
                members.Add(blocks[remaining[position]]);
            }

            var cluster = Build(members, settings, group.IsCohesive);
            if (CanMeetMinimums(cluster, settings) && IsWithinMaximums(cluster, settings))
            {
                accumulator.Clusters.Add(cluster);
            }
            else
            {
                Record(cluster, settings, accumulator.Tally);
            }
        }
    }

    /// <summary>
    ///     Derives the cluster id from the sorted member hashes and sizes, so renaming or adding a file
    ///     cannot change the identity of unchanged duplicated code.
    /// </summary>
    /// <param name="instances">The cluster members.</param>
    /// <returns>A stable identifier of the form <c>dup-</c> followed by twelve hex characters.</returns>
    private static string BuildId(CodeInstance[] instances)
    {
        var parts = new List<string>(instances.Length);
        foreach (var instance in instances)
        {
            parts.Add($"{instance.Hash}:{instance.Lines.Count}");
        }

        parts.Sort(StringComparer.Ordinal);
        var material = string.Join('\n', parts);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return $"dup-{Convert.ToHexString(digest)[..12].ToLowerInvariant()}";
    }

    private static bool CanMeetMinimums(DuplicateCluster cluster, DetectionSettings settings)
    {
        return cluster.Metrics.FileSpread >= settings.MinFileSpread &&
            (!cluster.Metrics.IsProjectSpreadKnown || cluster.Metrics.ProjectSpread >= settings.MinProjectSpread);
    }

    private static int CompareByLocation(CodeBlock left, CodeBlock right)
    {
        var byPath = string.Compare(left.FilePath, right.FilePath, StringComparison.OrdinalIgnoreCase);
        if (byPath != 0)
        {
            return byPath;
        }

        var byStart = left.Lines.Start.CompareTo(right.Lines.Start);
        return byStart != 0 ? byStart : string.CompareOrdinal(left.Hash, right.Hash);
    }

    private static int CompareBySeverity(DuplicateCluster left, DuplicateCluster right)
    {
        var byRemovable = right.Metrics.RemovableLines.CompareTo(left.Metrics.RemovableLines);
        if (byRemovable != 0)
        {
            return byRemovable;
        }

        var byOccurrences = right.Metrics.Occurrences.CompareTo(left.Metrics.Occurrences);
        return byOccurrences != 0 ? byOccurrences : string.CompareOrdinal(left.Id, right.Id);
    }

    private static Gathered Gather(List<CodeBlock> ordered)
    {
        var gathered = new Gathered(ordered.Count);
        for (var index = 0; index < ordered.Count; index++)
        {
            gathered.Add(index, ordered[index]);
        }

        return gathered;
    }

    /// <summary>
    ///     Applied to near-duplicate clusters only.
    /// </summary>
    /// <param name="cluster">The candidate cluster.</param>
    /// <param name="settings">The thresholds to test against.</param>
    /// <returns><c>true</c> when the cluster is within both size ceilings.</returns>
    private static bool IsWithinMaximums(DuplicateCluster cluster, DetectionSettings settings)
    {
        return (settings.MaxFileSpread == 0 || cluster.Metrics.FileSpread <= settings.MaxFileSpread) &&
            (settings.MaxOccurrences == 0 || cluster.Metrics.Occurrences <= settings.MaxOccurrences);
    }

    /// <summary>
    ///     Attributes a rejected cluster to the threshold that rejected it.
    /// </summary>
    /// <param name="cluster">The rejected cluster.</param>
    /// <param name="settings">The thresholds that rejected it.</param>
    /// <param name="tally">The running totals to update.</param>
    private static void Record(DuplicateCluster cluster, DetectionSettings settings, Tally tally)
    {
        if (cluster.Metrics.FileSpread < settings.MinFileSpread)
        {
            tally.Add(nameof(SuppressionCounts.BelowFileSpread), cluster.Id);
        }
        else if (cluster.Metrics.IsProjectSpreadKnown && cluster.Metrics.ProjectSpread < settings.MinProjectSpread)
        {
            tally.Add(nameof(SuppressionCounts.BelowProjectSpread), cluster.Id);
        }
        else if (settings.MaxFileSpread > 0 && cluster.Metrics.FileSpread > settings.MaxFileSpread)
        {
            tally.Add(nameof(SuppressionCounts.AboveFileSpread), cluster.Id);
        }
        else
        {
            tally.Add(nameof(SuppressionCounts.AboveOccurrences), cluster.Id);
        }
    }

    /// <summary>
    ///     Carries the state the two detection passes share.
    /// </summary>
    private sealed class Accumulator
    {

        /// <summary>
        ///     Gets the block indexes already claimed by an exact cluster.
        /// </summary>
        public HashSet<int> Claimed { get; }

        /// <summary>
        ///     Gets the clusters accepted so far.
        /// </summary>
        public List<DuplicateCluster> Clusters { get; }

        /// <summary>
        ///     Gets the running totals of what each threshold rejected.
        /// </summary>
        public Tally Tally { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Accumulator"/> class.
        /// </summary>
        public Accumulator()
        {
            var clusters = new List<DuplicateCluster>();
            var claimed = new HashSet<int>();
            var tally = new Tally();
            Clusters = clusters;
            Claimed = claimed;
            Tally = tally;
        }
    }

    /// <summary>
    ///     The per-block facts a cluster is derived from.
    /// </summary>
    private sealed class Gathered
    {

        /// <summary>
        ///     Gets the distinct files the blocks occupy.
        /// </summary>
        public HashSet<string> Files { get; }

        /// <summary>
        ///     Gets the distinct structural hashes seen.
        /// </summary>
        public HashSet<string> Hashes { get; }

        /// <summary>
        ///     Gets a value indicating whether any block is production code.
        /// </summary>
        public bool HasProductionInstance { get; private set; }

        /// <summary>
        ///     Gets the instances in the order the blocks were added.
        /// </summary>
        public CodeInstance[] Instances { get; }

        /// <summary>
        ///     Gets a value indicating whether every block knows its project.
        /// </summary>
        public bool IsProjectSpreadKnown { get; private set; }

        /// <summary>
        ///     Gets the distinct known projects the blocks belong to.
        /// </summary>
        public HashSet<ProjectIdentity> Projects { get; }

        /// <summary>
        ///     Gets the verbatim source of each block.
        /// </summary>
        public string[] RawSnippets { get; }

        /// <summary>
        ///     Gets the summed line count of every block added.
        /// </summary>
        public int TotalLines { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Gathered"/> class.
        /// </summary>
        /// <param name="count">The number of blocks that will be added.</param>
        public Gathered(int count)
        {
            var instances = new CodeInstance[count];
            var rawSnippets = new string[count];
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var projects = new HashSet<ProjectIdentity>();
            var hashes = new HashSet<string>(StringComparer.Ordinal);
            Instances = instances;
            RawSnippets = rawSnippets;
            Files = files;
            Projects = projects;
            Hashes = hashes;
            IsProjectSpreadKnown = true;
        }

        /// <summary>
        ///     Records one block at its ordered position.
        /// </summary>
        /// <param name="index">The position of the block within the cluster.</param>
        /// <param name="block">The block to record.</param>
        public void Add(int index, CodeBlock block)
        {
            var instance = block.ToInstance();
            Instances[index] = instance;
            RawSnippets[index] = block.RawText;
            TotalLines += block.Lines.Count;
            Files.Add(instance.FilePath);
            Hashes.Add(block.Hash);

            if (instance.Project.IsKnown)
            {
                Projects.Add(instance.Project);
            }
            else
            {
                IsProjectSpreadKnown = false;
            }

            if (!instance.IsTestFile)
            {
                HasProductionInstance = true;
            }
        }
    }

    /// <summary>
    ///     Running totals of what each threshold rejected.
    /// </summary>
    private sealed class Tally
    {
        private readonly Dictionary<string, HashSet<string>> _byReason;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Tally"/> class.
        /// </summary>
        public Tally()
        {
            var byReason = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            _byReason = byReason;
        }

        /// <summary>
        ///     Attributes one rejected cluster to a reason, ignoring repeats of the same cluster.
        /// </summary>
        /// <param name="reason">The threshold that rejected the cluster.</param>
        /// <param name="clusterId">The identifier of the rejected cluster.</param>
        public void Add(string reason, string clusterId)
        {
            if (!_byReason.TryGetValue(reason, out var ids))
            {
                ids = new HashSet<string>(StringComparer.Ordinal);
                _byReason[reason] = ids;
            }

            ids.Add(clusterId);
        }

        /// <summary>
        ///     Projects the running totals into the reported shape.
        /// </summary>
        /// <returns>The per-reason discard counts.</returns>
        public SuppressionCounts ToCounts()
        {
            var counts = new SuppressionCounts
            {
                BelowFileSpread = Count(nameof(SuppressionCounts.BelowFileSpread)),
                BelowProjectSpread = Count(nameof(SuppressionCounts.BelowProjectSpread)),
                AboveFileSpread = Count(nameof(SuppressionCounts.AboveFileSpread)),
                AboveOccurrences = Count(nameof(SuppressionCounts.AboveOccurrences)),
            };

            return counts;
        }

        private int Count(string reason)
        {
            return _byReason.TryGetValue(reason, out var ids) ? ids.Count : 0;
        }
    }
}
