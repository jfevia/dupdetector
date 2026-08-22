namespace DupDetector.Core.Model;

/// <summary>
///     A group of code blocks that duplicate one another.
/// </summary>
public sealed record DuplicateCluster
{
    /// <summary>
    ///     Gets the identity that survives copies being added, which a baseline comparison keys on.
    /// </summary>
    public string ContentKey
    {
        get
        {
            var lowest = Instances[0].Hash;
            foreach (var instance in Instances)
            {
                if (string.CompareOrdinal(instance.Hash, lowest) < 0)
                {
                    lowest = instance.Hash;
                }
            }

            return lowest;
        }
    }

    /// <summary>
    ///     Gets the stable identifier of this cluster.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     Gets the places the duplicated code appears.
    /// </summary>
    public required IReadOnlyList<CodeInstance> Instances { get; init; }

    /// <summary>
    ///     Gets a value indicating whether every member resembles every other member.
    /// </summary>
    public required bool IsCohesive { get; init; }

    /// <summary>
    ///     Gets a value indicating whether every instance shares one structural hash.
    /// </summary>
    public bool IsExact
    {
        get
        {
            var first = Instances[0].Hash;
            foreach (var instance in Instances)
            {
                if (!string.Equals(instance.Hash, first, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether this cluster represents cross-project production debt.
    /// </summary>
    public required bool IsProductionDuplicate { get; init; }

    /// <summary>
    ///     Gets the measured facts about this cluster.
    /// </summary>
    public required ClusterMetrics Metrics { get; init; }

    /// <summary>
    ///     Gets the shared structural form of the duplicated code.
    /// </summary>
    public required string NormalizedSnippet { get; init; }

    /// <summary>
    ///     Gets the verbatim source of each instance.
    /// </summary>
    public required IReadOnlyList<string> RawSnippets { get; init; }
}
