namespace DupDetector.Core.Detection;

/// <summary>
///     A pair of block indices found to meet the similarity threshold.
/// </summary>
public readonly record struct SimilarPair
{

    /// <summary>
    ///     Gets the lower block index.
    /// </summary>
    public int Left { get; }

    /// <summary>
    ///     Gets the higher block index.
    /// </summary>
    public int Right { get; }

    /// <summary>
    ///     Gets the measured similarity.
    /// </summary>
    public double Similarity { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SimilarPair"/> struct.
    /// </summary>
    /// <param name="left">The lower block index.</param>
    /// <param name="right">The higher block index.</param>
    /// <param name="similarity">The measured similarity.</param>
    public SimilarPair(int left, int right, double similarity)
    {
        Left = left;
        Right = right;
        Similarity = similarity;
    }
}
