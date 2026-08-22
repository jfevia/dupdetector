namespace DupDetector.Core.Normalization;

/// <summary>
///     The normalized text of a member and its structural hash.
/// </summary>
public readonly record struct NormalizedBlock
{

    /// <summary>
    ///     Gets the hash of the structural form.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    ///     Gets the structural form.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NormalizedBlock"/> struct.
    /// </summary>
    /// <param name="text">The structural form.</param>
    /// <param name="hash">The hash of the structural form.</param>
    public NormalizedBlock(string text, string hash)
    {
        Text = text;
        Hash = hash;
    }
}
