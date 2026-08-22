using Microsoft.CodeAnalysis;

namespace DupDetector.Core.Model;

/// <summary>
/// A parsed source file together with its project and its path relative to the scan root.
/// Test classification uses the relative path, so a checkout living under a directory such as
/// <c>C:\test\</c> cannot mark an entire tree as tests.
/// </summary>
public sealed record SourceUnit(
    string Path,
    string RelativePath,
    string Text,
    SyntaxTree Tree,
    ProjectIdentity Project,
    bool IsTestFile)
{
    /// <summary>
    /// Projects to the descriptor retained after extraction, allowing the syntax tree to be released.
    /// </summary>
    public SourceFile ToFile()
    {
        var lineCount = LineCounter.Count(Text);
        return new SourceFile(Path, RelativePath, Project, lineCount, IsTestFile)
        {
            CodeLines = CodeLineMap.Create(Tree, lineCount),
        };
    }
}

/// <summary>
/// What the pipeline keeps about a file once its blocks have been extracted.
/// </summary>
public sealed record SourceFile(
    string Path,
    string RelativePath,
    ProjectIdentity Project,
    int LineCount,
    bool IsTestFile)
{
    /// <summary>Which of this file's lines carry code.</summary>
    public CodeLineMap CodeLines { get; init; } = CodeLineMap.Empty;
}
