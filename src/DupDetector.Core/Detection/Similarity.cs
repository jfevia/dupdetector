namespace DupDetector.Core.Detection;

/// <summary>
/// Multiset Jaccard similarity.
/// </summary>
public static class Similarity
{
    /// <summary>
    /// Returns the multiset Jaccard similarity of two token multisets, in the range 0 to 1.
    /// Two empty multisets are identical.
    /// </summary>
    // Union is derived as |a| + |b| - overlap, so no intermediate collection is allocated per pair.
    public static double Jaccard(TokenMultiset a, TokenMultiset b)
    {
        var overlap = Overlap(a, b);
        var union = a.Cardinality + b.Cardinality - overlap;
        return union == 0 ? 1.0 : (double)overlap / union;
    }

    /// <summary>
    /// Sum of <c>min(count)</c> over tokens present in both multisets, by merge-walking the two
    /// ascending id arrays.
    /// </summary>
    public static int Overlap(TokenMultiset a, TokenMultiset b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var overlap = 0;
        int left = 0, right = 0;

        while (left < a.Ids.Length && right < b.Ids.Length)
        {
            var difference = a.Ids[left] - b.Ids[right];
            if (difference == 0)
            {
                overlap += Math.Min(a.Counts[left], b.Counts[right]);
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
    /// Largest similarity two multisets of these sizes could possibly reach.
    /// </summary>
    // Exact bound: a pair it rejects can never meet the threshold, so pruning on it loses no results.
    public static double UpperBound(int cardinalityA, int cardinalityB)
    {
        var larger = Math.Max(cardinalityA, cardinalityB);
        return larger == 0 ? 1.0 : (double)Math.Min(cardinalityA, cardinalityB) / larger;
    }
}
