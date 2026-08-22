namespace DupDetector.Cli.Tests;

/// <summary>
///     One source file in a fixture project.
/// </summary>
public sealed record ProjectFile
{

    /// <summary>
    ///     
    /// </summary>
    public string File { get; }

    /// <summary>
    ///     
    /// </summary>
    public string Project { get; }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="project">The owning project name.</param>
    /// <param name="file">The file name inside the project.</param>
    public ProjectFile(string project, string file)
    {
        Project = project;
        File = file;
    }
}
