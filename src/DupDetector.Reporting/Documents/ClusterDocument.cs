namespace DupDetector.Reporting.Documents;

/// <summary>
///     
/// </summary>
public sealed class ClusterDocument
{

    /// <summary>
    ///     
    /// </summary>
    public required int FileSpread { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<InstanceDocument> Instances { get; init; }

    /// <summary>
    ///     False when the grouping budget was exhausted and members may not all resemble one another.
    /// </summary>
    public required bool IsCohesive { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required bool IsExact { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required bool IsProductionDuplicate { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required bool IsProjectSpreadKnown { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int Lines { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string NormalizedSnippet { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int Occurrences { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required int ProjectSpread { get; init; }

    /// <summary>
    ///     Verbatim source. Omitted unless explicitly requested, because it leaks real code.
    /// </summary>
    public IReadOnlyList<string>? RawSnippets { get; init; }

    /// <summary>
    ///     Lines that disappear if every copy but one is removed.
    /// </summary>
    public required int RemovableLines { get; init; }

    /// <summary>
    ///     Priority ranking that weighs removable lines against how far the copies have spread.
    /// </summary>
    public required double Score { get; init; }
}
