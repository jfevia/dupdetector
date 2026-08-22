namespace DupDetector.Core.Model;

/// <summary>
///     Measured facts about a cluster, deliberately carrying no severity score.
/// </summary>
public sealed record ClusterMetrics
{

    /// <summary>
    ///     Gets the number of distinct files the copies occupy.
    /// </summary>
    public int FileSpread { get; }

    /// <summary>
    ///     Gets a value indicating whether every instance knew its project.
    /// </summary>
    public bool IsProjectSpreadKnown { get; }

    /// <summary>
    ///     Gets the rounded average member size.
    /// </summary>
    public int Lines { get; }

    /// <summary>
    ///     Gets the number of copies.
    /// </summary>
    public int Occurrences { get; }

    /// <summary>
    ///     Gets the number of distinct known projects the copies occupy.
    /// </summary>
    public int ProjectSpread { get; }

    /// <summary>
    ///     Gets the lines that would disappear if every copy but one were removed.
    /// </summary>
    public int RemovableLines
    {
        get
        {
            return Lines * (Occurrences - 1);
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClusterMetrics"/> class.
    /// </summary>
    /// <param name="lines">The rounded average member size.</param>
    /// <param name="occurrences">The number of copies.</param>
    /// <param name="spread">How far the copies reach.</param>
    public ClusterMetrics(int lines, int occurrences, ClusterSpread spread)
    {
        Lines = lines;
        Occurrences = occurrences;
        FileSpread = spread.Files;
        ProjectSpread = spread.Projects;
        IsProjectSpreadKnown = spread.IsProjectSpreadKnown;
    }
}
