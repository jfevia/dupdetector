using DupDetector.Core.Model.Reporting;

namespace DupDetector.Core.Pipeline;

/// <summary>
///     The outcome of a run: the report plus anything the caller should be told.
/// </summary>
public sealed record AnalysisResult
{

    /// <summary>
    ///     Gets the conditions that change what the numbers mean.
    /// </summary>
    public IReadOnlyList<AnalysisNote> Notes { get; }

    /// <summary>
    ///     Gets the analysis result.
    /// </summary>
    public DetectionReport Report { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AnalysisResult"/> class.
    /// </summary>
    /// <param name="report">The analysis result.</param>
    /// <param name="notes">Conditions that change what the numbers mean.</param>
    public AnalysisResult(DetectionReport report, IReadOnlyList<AnalysisNote> notes)
    {
        Report = report;
        Notes = notes;
    }
}
