using Microsoft.CodeAnalysis;

namespace DupDetector.Core.Model;

/// <summary>
///     A parsed source file together with its project and its path relative to the scan root.
/// </summary>
public sealed record SourceUnit
{

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
    ///     Gets the file contents.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Gets the parsed syntax tree.
    /// </summary>
    public SyntaxTree Tree { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SourceUnit"/> class.
    /// </summary>
    /// <param name="path">The absolute path of the file.</param>
    /// <param name="text">The file contents.</param>
    /// <param name="tree">The parsed syntax tree.</param>
    /// <param name="origin">Where the file came from.</param>
    public SourceUnit(string path, string text, SyntaxTree tree, SourceOrigin origin)
    {
        Path = path;
        Text = text;
        Tree = tree;
        Origin = origin;
    }

    /// <summary>
    ///     Projects to the descriptor retained after extraction, releasing the syntax tree.
    /// </summary>
    /// <returns>The retained file descriptor.</returns>
    public SourceFile ToFile()
    {
        var lineCount = LineCounter.Count(Text);
        var file = new SourceFile(Path, Origin, lineCount)
        {
            CodeLines = CodeLineMaps.Create(Tree, lineCount),
        };

        return file;
    }
}
