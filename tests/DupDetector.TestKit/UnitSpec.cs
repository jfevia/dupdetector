using DupDetector.Core.Model;

namespace DupDetector.TestKit;

/// <summary>
///     Describes a source unit for a test fixture.
/// </summary>
public sealed record UnitSpec
{

    /// <summary>
    ///     Gets a value indicating whether the file is test code.
    /// </summary>
    public bool IsTestFile { get; init; }

    /// <summary>
    ///     Gets the file path.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    ///     Gets the project name, or <c>null</c> for an unknown project.
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    ///     Gets the path relative to the scan root, or <c>null</c> to derive it.
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    ///     Gets the settings used when extracting blocks.
    /// </summary>
    public DetectionSettings? Settings { get; init; }

    /// <summary>
    ///     Gets the file contents.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnitSpec"/> class.
    /// </summary>
    /// <param name="text">The file contents.</param>
    public UnitSpec(string text)
    {
        Text = text;
        Path = "/repo/File.cs";
        Project = "Proj";
    }
}
