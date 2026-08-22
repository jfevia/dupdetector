namespace DupDetector.Core.Model;

/// <summary>
///     What the pipeline keeps about a file once its blocks have been extracted.
/// </summary>
public sealed record SourceFile
{

    /// <summary>
    ///     Gets which of this file's lines carry code.
    /// </summary>
    public CodeLineMap CodeLines { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the file is classified as test code.
    /// </summary>
    public bool IsTestFile
    {
        get
        {
            return Origin.IsTestFile;
        }
    }

    /// <summary>
    ///     Gets the physical line count.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    ///     Gets where the file came from.
    /// </summary>
    public SourceOrigin Origin { get; }

    /// <summary>
    ///     Gets the absolute path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Gets the project the file belongs to.
    /// </summary>
    public ProjectIdentity Project
    {
        get
        {
            return Origin.Project;
        }
    }

    /// <summary>
    ///     Gets the path relative to the scan root.
    /// </summary>
    public string RelativePath
    {
        get
        {
            return Origin.RelativePath;
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SourceFile"/> class.
    /// </summary>
    /// <param name="path">The absolute path of the file.</param>
    /// <param name="origin">Where the file came from.</param>
    /// <param name="lineCount">The physical line count.</param>
    public SourceFile(string path, SourceOrigin origin, int lineCount)
    {
        Path = path;
        Origin = origin;
        LineCount = lineCount;
        CodeLines = CodeLineMap.Empty;
    }
}
