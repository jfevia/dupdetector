namespace DupDetector.Reporting.Documents;

/// <summary>
///     Provenance for a report, so a stale file cannot be mistaken for a current one.
/// </summary>
public sealed class MetadataDocument
{

    /// <summary>
    ///     The command that produced this report.
    /// </summary>
    public required string CommandLine { get; init; }

    /// <summary>
    ///     Commit the analysed tree was on, when it could be determined.
    /// </summary>
    public string? Commit { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string GeneratedAtUtc { get; init; }

    /// <summary>
    ///     Incremented whenever the output shape changes in a way consumers must handle.
    /// </summary>
    public string SchemaVersion { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string ToolVersion { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public MetadataDocument()
    {
        SchemaVersion = "1.0";
    }
}
