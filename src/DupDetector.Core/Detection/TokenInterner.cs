namespace DupDetector.Core.Detection;

/// <summary>
///     Maps token text to dense integer ids so similarity can work on sorted integer arrays.
/// </summary>
public sealed class TokenInterner
{
    private readonly Dictionary<string, int> _ids;

    /// <summary>
    ///     Gets the number of distinct tokens seen so far.
    /// </summary>
    public int Count
    {
        get
        {
            return _ids.Count;
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TokenInterner"/> class.
    /// </summary>
    public TokenInterner()
    {
        var ids = new Dictionary<string, int>(StringComparer.Ordinal);
        _ids = ids;
    }

    /// <summary>
    ///     Returns the id for a token, assigning a new one when it is first seen.
    /// </summary>
    /// <param name="token">The token text.</param>
    /// <returns>The dense id for that token.</returns>
    public int Intern(string token)
    {
        if (!_ids.TryGetValue(token, out var id))
        {
            id = _ids.Count;
            _ids[token] = id;
        }

        return id;
    }
}
