namespace DupDetector.Core.Model;

/// <summary>
/// Counts physical lines without the trailing-newline inflation of <c>Split('\n').Length</c>.
/// </summary>
public static class LineCounter
{
    /// <summary>
    /// Returns the number of lines in <paramref name="text"/>. An empty string has zero lines,
    /// and a trailing newline does not add a phantom line.
    /// </summary>
    public static int Count(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return 0;
        }

        var newlines = text.AsSpan().Count('\n');
        return text[^1] == '\n' ? newlines : newlines + 1;
    }
}
