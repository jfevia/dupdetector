namespace DupDetector.TestKit;

/// <summary>
///     An undirected edge between two block indices, ordered low to high.
/// </summary>
public readonly record struct Edge
{

    /// <summary>
    ///     
    /// </summary>
    public int Left { get; }

    /// <summary>
    ///     
    /// </summary>
    public int Right { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="left">The lower block index.</param>
    /// <param name="right">The higher block index.</param>
    public Edge(int left, int right)
    {
        Left = left;
        Right = right;
    }
}
