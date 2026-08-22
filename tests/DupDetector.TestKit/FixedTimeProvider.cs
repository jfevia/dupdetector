namespace DupDetector.TestKit;

/// <summary>
///     A clock frozen at one instant, so timestamps in tests are reproducible.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _instant;

    /// <summary>
    ///     A clock frozen at 2024-01-01T00:00:00Z.
    /// </summary>
    public FixedTimeProvider()
        : this(DateTimeOffset.UnixEpoch.AddYears(54))
    {
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="instant">The instant the clock always reports.</param>
    public FixedTimeProvider(DateTimeOffset instant)
    {
        _instant = instant;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    public override DateTimeOffset GetUtcNow()
    {
        return _instant;
    }
}
