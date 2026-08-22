namespace DupDetector.Core.Model;

/// <summary>
///     The two textual forms a block is kept in.
/// </summary>
public sealed record BlockContent
{

    /// <summary>
    ///     Gets the structural form.
    /// </summary>
    public string NormalizedText { get; }

    /// <summary>
    ///     Gets the verbatim source.
    /// </summary>
    public string RawText { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockContent"/> class.
    /// </summary>
    /// <param name="normalizedText">The structural form.</param>
    /// <param name="rawText">The verbatim source.</param>
    public BlockContent(string normalizedText, string rawText)
    {
        NormalizedText = normalizedText;
        RawText = rawText;
    }
}
