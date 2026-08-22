namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     Share of a project's lines that participate in at least one duplicate cluster.
/// </summary>
public sealed record ProjectScore
{
    /// <summary>
    ///     Gets the distinct duplicated lines in the project.
    /// </summary>
    public required int DuplicateLines { get; init; }

    /// <summary>
    ///     Gets the share of the project's lines that are duplicated.
    /// </summary>
    public required double Percentage { get; init; }

    /// <summary>
    ///     Gets the project identity.
    /// </summary>
    public required ProjectIdentity Project { get; init; }

    /// <summary>
    ///     Gets the physical line count of the project.
    /// </summary>
    public required int TotalLines { get; init; }
}
