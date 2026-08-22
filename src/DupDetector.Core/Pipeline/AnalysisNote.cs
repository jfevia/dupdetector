namespace DupDetector.Core.Pipeline;

/// <summary>
///     A note the pipeline needs to surface to its caller.
/// </summary>
public sealed record AnalysisNote
{

    /// <summary>
    ///     Gets the text the caller should be shown.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnalysisNote"/> class.
    /// </summary>
    /// <param name="message">What the caller should be told.</param>
    public AnalysisNote(string message)
    {
        Message = message;
    }
}
