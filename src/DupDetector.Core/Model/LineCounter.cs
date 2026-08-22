namespace DupDetector.Core.Model;

/// <summary>
///     Counts physical lines without the trailing-newline inflation of <c>Split</c>.
/// </summary>
public static class LineCounter
{
    /// <summary>
    ///     Returns the number of lines, treating an empty string as zero.
    /// </summary>
    /// <param name="text">The text to count.</param>
    /// <returns>The number of physical lines.</returns>
    public static int Count(string text)
    {
        if (text.Length == 0)
        {
            return 0;
        }

        var newlines = text.AsSpan().Count('\n');
        return text[^1] == '\n' ? newlines : newlines + 1;
    }
}
