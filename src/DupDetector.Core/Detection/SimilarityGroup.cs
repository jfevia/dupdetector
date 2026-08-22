namespace DupDetector.Core.Detection;

/// <summary>
///     A set of mutually similar block indices.
/// </summary>
public sealed record SimilarityGroup
{

    /// <summary>
    ///     Gets a value indicating whether every member is similar to every other member.
    /// </summary>
    public bool IsCohesive { get; }

    /// <summary>
    ///     Gets the block indices, ascending.
    /// </summary>
    public IReadOnlyList<int> Members { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SimilarityGroup"/> class.
    /// </summary>
    /// <param name="members">Block indices, ascending.</param>
    /// <param name="isCohesive">Whether every member is similar to every other member.</param>
    public SimilarityGroup(IReadOnlyList<int> members, bool isCohesive)
    {
        Members = members;
        IsCohesive = isCohesive;
    }
}
