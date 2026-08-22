namespace DupDetector.Reporting.Sarif.Model;

/// <summary>
///     A SARIF rule descriptor.
/// </summary>
public sealed record SarifRule
{

    /// <summary>
    ///     
    /// </summary>
    public required SarifConfiguration DefaultConfiguration { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifText FullDescription { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string HelpUri { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     
    /// </summary>
    public required SarifText ShortDescription { get; init; }
}
