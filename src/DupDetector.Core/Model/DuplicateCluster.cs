namespace DupDetector.Core.Model;

/// <summary>
/// A group of code blocks that duplicate one another.
/// </summary>
public sealed record DuplicateCluster
{
    public required string Id { get; init; }

    public required IReadOnlyList<CodeInstance> Instances { get; init; }

    public required ClusterMetrics Metrics { get; init; }

    public required string NormalizedSnippet { get; init; }

    public required IReadOnlyList<string> RawSnippets { get; init; }

    /// <summary>
    /// True when every instance shares one structural hash. Derived from the instances themselves,
    /// so a verbatim copy can never be relabelled as a near-duplicate by an upstream filter.
    /// </summary>
    public bool IsExact => Instances.DistinctBy(instance => instance.Hash, StringComparer.Ordinal).Count() == 1;

    /// <summary>
    /// Identity that survives copies being added or removed, unlike <see cref="Id"/>, which is
    /// derived from the full membership. This is what a baseline comparison must key on.
    /// </summary>
    public string ContentKey => Instances.Select(instance => instance.Hash).Order(StringComparer.Ordinal).First();

    /// <summary>
    /// True when every member is similar to every other member. False only when the clique budget
    /// was exhausted and the group fell back to connectivity, in which case some members may not
    /// resemble one another.
    /// </summary>
    public required bool IsCohesive { get; init; }

    /// <summary>
    /// True when the cluster spans at least two projects and at least one instance is production
    /// code. A test-file copy of genuinely duplicated production code does not clear the flag.
    /// </summary>
    public required bool IsProductionDuplicate { get; init; }
}
