namespace DupDetector.Core.Model;

/// <summary>
///     One member of a duplicate cluster.
/// </summary>
public sealed record CodeInstance
{

    /// <summary>
    ///     Gets the absolute path of the file the instance lives in.
    /// </summary>
    public string FilePath
    {
        get
        {
            return Location.FilePath;
        }
    }

    /// <summary>
    ///     Gets the structural hash.
    /// </summary>
    public string Hash { get; }

    /// <summary>
    ///     Gets a value indicating whether the instance is in test code.
    /// </summary>
    public bool IsTestFile
    {
        get
        {
            return Location.IsTestFile;
        }
    }

    /// <summary>
    ///     Gets the lines the instance occupies.
    /// </summary>
    public LineRange Lines
    {
        get
        {
            return Location.Lines;
        }
    }

    /// <summary>
    ///     Gets where the instance lives.
    /// </summary>
    public CodeLocation Location { get; }

    /// <summary>
    ///     Gets the reported member name.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    ///     Gets the project the instance belongs to.
    /// </summary>
    public ProjectIdentity Project
    {
        get
        {
            return Location.Project;
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CodeInstance"/> class.
    /// </summary>
    /// <param name="location">Where the instance lives.</param>
    /// <param name="memberName">The reported member name.</param>
    /// <param name="hash">The structural hash.</param>
    public CodeInstance(CodeLocation location, string memberName, string hash)
    {
        Location = location;
        MemberName = memberName;
        Hash = hash;
    }
}
