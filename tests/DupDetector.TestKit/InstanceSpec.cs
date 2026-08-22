namespace DupDetector.TestKit;

/// <summary>
///     One instance of a duplicate cluster fixture.
/// </summary>
public sealed record InstanceSpec
{

    /// <summary>
    ///     
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     
    /// </summary>
    public string Project { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="path">The file the instance lives in.</param>
    /// <param name="project">The owning project name.</param>
    public InstanceSpec(string path, string project)
    {
        Path = path;
        Project = project;
    }
}
