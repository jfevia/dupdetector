namespace DupDetector.Core.Detection;

/// <summary>
///     Limits on clique enumeration, which is exponential in the worst case.
/// </summary>
public readonly record struct CliqueBudget
{

    /// <summary>
    ///     Gets the budget applied when a caller does not supply one.
    /// </summary>
    public static CliqueBudget Default { get; }

    /// <summary>
    ///     Gets the largest connected component that will be enumerated exactly.
    /// </summary>
    public int MaxGroupSize { get; }

    /// <summary>
    ///     Gets the ceiling on recursive expansion steps within one component.
    /// </summary>
    public int MaxWork { get; }

    static CliqueBudget()
    {
        var fallback = new CliqueBudget(64, 20_000);
        Default = fallback;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CliqueBudget"/> struct.
    /// </summary>
    /// <param name="maxGroupSize">Largest connected component that will be enumerated exactly.</param>
    /// <param name="maxWork">Ceiling on recursive expansion steps within one component.</param>
    public CliqueBudget(int maxGroupSize, int maxWork)
    {
        MaxGroupSize = maxGroupSize;
        MaxWork = maxWork;
    }
}
