namespace DupDetector.Core.Detection;

/// <summary>
///     A block's tokens as a multiset: distinct ids ascending with their repeat counts.
/// </summary>
public sealed class TokenMultiset
{

    /// <summary>
    ///     Gets the total token count, including repeats.
    /// </summary>
    public int Cardinality { get; }

    /// <summary>
    ///     Gets the repeat count for each entry of <see cref="Ids"/>.
    /// </summary>
    public int[] Counts { get; }

    /// <summary>
    ///     Gets the distinct token ids, ascending.
    /// </summary>
    public int[] Ids { get; }

    /// <param name="ids"></param>
    /// <param name="counts"></param>
    /// <param name="cardinality"></param>
    /// <summary>
    ///     
    /// </summary>
    public TokenMultiset(int[] ids, int[] counts, int cardinality)
    {
        Ids = ids;
        Counts = counts;
        Cardinality = cardinality;
    }
}
