using DupDetector.Core.Model.Reporting;

using DupDetector.Reporting.Documents;

namespace DupDetector.Reporting;

/// <summary>
///     Renders a report in one serialization format.
/// </summary>
public interface IReportWriter
{
    /// <summary>
    ///     
    /// </summary>
    ReportFormat Format { get; }

    /// <summary>
    ///     Provenance stamped into the output. Absent when the caller has none to give.
    /// </summary>
    MetadataDocument? Metadata { get; init; }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="report"></param>
    string Write(DetectionReport report);
}
