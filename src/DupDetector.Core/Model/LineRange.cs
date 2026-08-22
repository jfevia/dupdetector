using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
/// An inclusive, one-based range of source lines.
/// </summary>
public readonly record struct LineRange
{
    public LineRange(int start, int end)
    {
        Start = Require.AtLeast(start, 1, nameof(start));
        End = Require.AtLeast(end, start, nameof(end));
    }

    public int Start { get; }

    public int End { get; }

    /// <summary>Number of lines covered, inclusive of both endpoints.</summary>
    public int Count => End - Start + 1;

    public override string ToString() => $"{Start}-{End}";
}
