namespace DupDetector.Core.Model.Reporting;

/// <summary>
///     How source files were located for a run.
/// </summary>
public enum DiscoveryMode
{
    /// <summary>
    ///     Nothing was discovered.
    /// </summary>
    None,

    /// <summary>
    ///     Files were found by walking directories.
    /// </summary>
    FileSystem,

    /// <summary>
    ///     Files were found by loading a project or solution.
    /// </summary>
    Workspace,

    /// <summary>
    ///     Files were found by both means.
    /// </summary>
    Mixed,
}
