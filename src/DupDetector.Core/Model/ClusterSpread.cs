namespace DupDetector.Core.Model;

/// <summary>
///     How far a cluster's copies reach across files and projects.
/// </summary>
public readonly record struct ClusterSpread
{

    /// <summary>
    ///     Gets the number of distinct files.
    /// </summary>
    public int Files { get; }

    /// <summary>
    ///     Gets a value indicating whether every instance knew its project.
    /// </summary>
    public bool IsProjectSpreadKnown { get; }

    /// <summary>
    ///     Gets the number of distinct known projects.
    /// </summary>
    public int Projects { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ClusterSpread"/> struct.
    /// </summary>
    /// <param name="files">The number of distinct files.</param>
    /// <param name="projects">The number of distinct known projects.</param>
    /// <param name="isProjectSpreadKnown">Whether every instance knew its project.</param>
    public ClusterSpread(int files, int projects, bool isProjectSpreadKnown)
    {
        Files = files;
        Projects = projects;
        IsProjectSpreadKnown = isProjectSpreadKnown;
    }
}
