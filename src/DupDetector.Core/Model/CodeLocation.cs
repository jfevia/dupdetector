namespace DupDetector.Core.Model;

/// <summary>
///     Where a block of code lives.
/// </summary>
public sealed record CodeLocation
{

    /// <summary>
    ///     Gets the absolute path of the file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    ///     Gets a value indicating whether the file is classified as test code.
    /// </summary>
    public bool IsTestFile { get; }

    /// <summary>
    ///     Gets the lines the block occupies.
    /// </summary>
    public LineRange Lines { get; }

    /// <summary>
    ///     Gets the project the file belongs to.
    /// </summary>
    public ProjectIdentity Project { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CodeLocation"/> class.
    /// </summary>
    /// <param name="filePath">The absolute path of the file.</param>
    /// <param name="project">The project the file belongs to.</param>
    /// <param name="isTestFile">Whether the file is classified as test code.</param>
    /// <param name="lines">The lines the block occupies.</param>
    public CodeLocation(string filePath, ProjectIdentity project, bool isTestFile, LineRange lines)
    {
        FilePath = filePath;
        Project = project;
        IsTestFile = isTestFile;
        Lines = lines;
    }
}
