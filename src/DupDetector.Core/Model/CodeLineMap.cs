namespace DupDetector.Core.Model;

/// <summary>
///     Which physical lines of a file carry code rather than blanks or comments.
/// </summary>
public sealed class CodeLineMap
{
    private readonly bool[] _isCode;

    /// <summary>
    ///     Gets a map for a file with no analysable lines.
    /// </summary>
    public static CodeLineMap Empty { get; }

    /// <summary>
    ///     Gets the number of lines carrying code.
    /// </summary>
    public int Total { get; }

    static CodeLineMap()
    {
        var empty = new CodeLineMap([]);
        Empty = empty;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="isCode"></param>
    public CodeLineMap(bool[] isCode)
    {
        _isCode = isCode;

        var total = 0;
        foreach (var value in isCode)
        {
            if (value)
            {
                total++;
            }
        }

        Total = total;
    }

    /// <summary>
    ///     Counts code lines inside a one-based, inclusive range.
    /// </summary>
    /// <param name="range">The range to count within.</param>
    /// <returns>The number of code lines in the range.</returns>
    public int CountIn(LineRange range)
    {
        var count = 0;
        var last = Math.Min(range.End, _isCode.Length);

        for (var line = range.Start; line <= last; line++)
        {
            if (_isCode[line - 1])
            {
                count++;
            }
        }

        return count;
    }
}
