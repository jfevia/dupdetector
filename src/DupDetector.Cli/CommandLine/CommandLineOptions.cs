using DupDetector.Core.Model;
using DupDetector.Reporting;

namespace DupDetector.Cli.CommandLine;

/// <summary>
///     A fully parsed command line, or the reason it could not be parsed.
/// </summary>
public sealed record CommandLineOptions
{

    /// <summary>
    ///     Previous report to compare against, so a run reports change rather than absolute state.
    /// </summary>
    public string? BaselinePath { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public double? FailOn { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required ReportFormat Format { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required IReadOnlyList<string> InputPaths { get; init; }

    /// <summary>
    ///     Whether a baseline regression should fail the run rather than only be reported.
    /// </summary>
    public bool IsFailOnNew { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public bool IsIncludeRawSnippets { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public bool IsVerbose { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required DetectionSettings Settings { get; init; }

    /// <summary>
    ///     Where to record this run for a later comparison.
    /// </summary>
    public string? WriteBaselinePath { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public CommandLineOptions()
    {
        IsIncludeRawSnippets = true;
    }
}
