namespace DupDetector.Core.Detection;

/// <summary>
///     Multiset Jaccard similarity.
/// </summary>
public static class Similarity
{
    /// <summary>
    ///     Returns the multiset Jaccard similarity of two token multisets, treating two empty
    ///     multisets as identical.
    /// </summary>
    /// <param name="first">The first multiset.</param>
    /// <param name="second">The second multiset.</param>
    /// <returns>The similarity, in the range 0 to 1.</returns>
    public static double Jaccard(TokenMultiset first, TokenMultiset second)
    {
        var overlap = Overlap(first, second);
        var union = first.Cardinality + second.Cardinality - overlap;
        return union == 0 ? 1.0 : (double)overlap / union;
    }

    /// <summary>
    ///     Sum of the smaller count over tokens present in both multisets.
    /// </summary>
    /// <param name="first">The first multiset.</param>
    /// <param name="second">The second multiset.</param>
    /// <returns>The number of shared token occurrences.</returns>
    public static int Overlap(TokenMultiset first, TokenMultiset second)
    {
        var overlap = 0;
        var left = 0;
        var right = 0;

        while (left < first.Ids.Length && right < second.Ids.Length)
        {
            var difference = first.Ids[left] - second.Ids[right];
            if (difference == 0)
            {
                overlap += Math.Min(first.Counts[left], second.Counts[right]);
                left++;
                right++;
            }
            else if (difference < 0)
            {
                left++;
            }
            else
            {
                right++;
            }
        }

        return overlap;
    }

    /// <summary>
    ///     Largest similarity two multisets of these sizes could possibly reach.
    /// </summary>
    /// <param name="firstCardinality">The size of the first multiset.</param>
    /// <param name="secondCardinality">The size of the second multiset.</param>
    /// <returns>The exact upper bound, in the range 0 to 1.</returns>
    public static double UpperBound(int firstCardinality, int secondCardinality)
    {
        var larger = Math.Max(firstCardinality, secondCardinality);
        return larger == 0 ? 1.0 : (double)Math.Min(firstCardinality, secondCardinality) / larger;
    }
}
