namespace DupDetector.Core.Model;

/// <summary>
///     Where a source file came from and how it is classified.
/// </summary>
public sealed record SourceOrigin
{

    /// <summary>
    ///     Gets a value indicating whether the file is classified as test code.
    /// </summary>
    public bool IsTestFile { get; }

    /// <summary>
    ///     Gets the project the file belongs to.
    /// </summary>
    public ProjectIdentity Project { get; }

    /// <summary>
    ///     Gets the path relative to the scan root.
    /// </summary>
    public string RelativePath { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SourceOrigin"/> class.
    /// </summary>
    /// <param name="relativePath">The path relative to the scan root.</param>
    /// <param name="project">The project the file belongs to.</param>
    /// <param name="isTestFile">Whether the file is classified as test code.</param>
    public SourceOrigin(string relativePath, ProjectIdentity project, bool isTestFile)
    {
        RelativePath = relativePath;
        Project = project;
        IsTestFile = isTestFile;
    }
}
