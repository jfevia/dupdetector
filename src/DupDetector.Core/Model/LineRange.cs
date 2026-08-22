using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
///     An inclusive, one-based range of source lines.
/// </summary>
public readonly record struct LineRange
{

    /// <summary>
    ///     Gets the number of lines covered, inclusive of both endpoints.
    /// </summary>
    public int Count
    {
        get
        {
            return End - Start + 1;
        }
    }

    /// <summary>
    ///     Gets the last line.
    /// </summary>
    public int End { get; }

    /// <summary>
    ///     Gets the first line.
    /// </summary>
    public int Start { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LineRange"/> struct.
    /// </summary>
    /// <param name="start">The first line, at least one.</param>
    /// <param name="end">The last line, at least <paramref name="start"/>.</param>
    public LineRange(int start, int end)
    {
        Start = Require.AtLeast(start, 1, nameof(start));
        End = Require.AtLeast(end, start, nameof(end));
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Start}-{End}";
    }
}
