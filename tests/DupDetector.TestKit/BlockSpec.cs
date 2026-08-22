namespace DupDetector.TestKit;

/// <summary>
///     Describes a code block for a test fixture.
/// </summary>
public sealed record BlockSpec
{

    /// <summary>
    ///     Gets the last line of the block.
    /// </summary>
    public int EndLine { get; init; }

    /// <summary>
    ///     Gets the structural hash.
    /// </summary>
    public string Hash { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the block is test code.
    /// </summary>
    public bool IsTestFile { get; init; }

    /// <summary>
    ///     Gets the reported member name.
    /// </summary>
    public string MemberName { get; init; }

    /// <summary>
    ///     Gets the structural form of the block.
    /// </summary>
    public string NormalizedText { get; }

    /// <summary>
    ///     Gets the file path.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    ///     Gets the project name, or <c>null</c> for an unknown project.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    ///     Gets the verbatim source, or <c>null</c> to reuse the normalized text.
    /// </summary>
    public string? RawText { get; init; }

    /// <summary>
    ///     Gets the first line of the block.
    /// </summary>
    public int StartLine { get; init; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockSpec"/> class.
    /// </summary>
    /// <param name="normalizedText">The structural form of the block.</param>
    public BlockSpec(string normalizedText)
    {
        NormalizedText = normalizedText;
        Path = "/repo/File.cs";
        Project = "Proj";
        Hash = "hash";
        StartLine = 1;
        EndLine = 10;
        MemberName = "Member";
    }
}
