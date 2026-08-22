namespace DupDetector.Core.Model;

/// <summary>
/// A single extracted, normalized unit of code.
/// </summary>
public sealed record CodeBlock(
    string FilePath,
    ProjectIdentity Project,
    bool IsTestFile,
    string MemberName,
    LineRange Lines,
    string Hash,
    string NormalizedText,
    string RawText)
{
    public CodeInstance ToInstance() => new(FilePath, Project, IsTestFile, MemberName, Lines, Hash);
}

/// <summary>
/// One member of a duplicate cluster.
/// </summary>
public sealed record CodeInstance(
    string FilePath,
    ProjectIdentity Project,
    bool IsTestFile,
    string MemberName,
    LineRange Lines,
    string Hash);
