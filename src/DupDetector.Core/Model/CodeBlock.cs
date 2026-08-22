namespace DupDetector.Core.Model;

/// <summary>
///     A single extracted, normalized unit of code.
/// </summary>
public sealed record CodeBlock
{

    /// <summary>
    ///     Gets the absolute path of the file the block came from.
    /// </summary>
    public string FilePath
    {
        get
        {
            return Location.FilePath;
        }
    }

    /// <summary>
    ///     Gets the structural hash of the normalized text.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    ///     Gets a value indicating whether the block came from test code.
    /// </summary>
    public bool IsTestFile
    {
        get
        {
            return Location.IsTestFile;
        }
    }

    /// <summary>
    ///     Gets the lines the block occupies.
    /// </summary>
    public LineRange Lines
    {
        get
        {
            return Location.Lines;
        }
    }

    /// <summary>
    ///     Gets where the block was found.
    /// </summary>
    public CodeLocation Location { get; }

    /// <summary>
    ///     Gets the reported member name.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    ///     Gets the structural form the hash was taken from.
    /// </summary>
    public string NormalizedText { get; }

    /// <summary>
    ///     Gets the project the block belongs to.
    /// </summary>
    public ProjectIdentity Project
    {
        get
        {
            return Location.Project;
        }
    }

    /// <summary>
    ///     Gets the verbatim source of the block.
    /// </summary>
    public string RawText { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CodeBlock"/> class.
    /// </summary>
    /// <param name="location">Where the block was found.</param>
    /// <param name="memberName">The reported member name.</param>
    /// <param name="hash">The structural hash.</param>
    /// <param name="content">The normalized and verbatim text.</param>
    public CodeBlock(CodeLocation location, string memberName, string hash, BlockContent content)
    {
        Location = location;
        MemberName = memberName;
        Hash = hash;
        NormalizedText = content.NormalizedText;
        RawText = content.RawText;
    }

    /// <summary>
    ///     Projects this block into a cluster instance.
    /// </summary>
    /// <returns>The instance describing where this block lives.</returns>
    public CodeInstance ToInstance()
    {
        var instance = new CodeInstance(Location, MemberName, Hash);
        return instance;
    }
}
