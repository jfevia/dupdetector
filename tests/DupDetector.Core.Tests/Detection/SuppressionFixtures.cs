using DupDetector.Core.Detection;

using DupDetector.Core.Model;
using DupDetector.TestKit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     Helpers for <see cref="SuppressionAccountingTests" />.
/// </summary>
public static class SuppressionFixtures
{

    /// <returns></returns>
    /// <summary>
    ///     
    /// </summary>
    /// <param name="count"></param>
    /// <param name="fileCount"></param>
    public static IReadOnlyList<CodeBlock> Blocks(int count, int fileCount)
    {
        var blocks = new List<CodeBlock>(count);
        for (var index = 0; index < count; index++)
        {
            var spec = new BlockSpec("identical")
            {
                Path = $"/repo/File{index % fileCount}.cs",
                Project = $"Proj{index % fileCount}",
                Hash = "same",
                StartLine = (index * 20) + 1,
                EndLine = (index * 20) + 10
            };

            blocks.Add(Code.Block(spec));
        }

        return blocks;
    }

    /// <summary>
    ///     A two-file cluster spanning the given lines.
    /// </summary>
    /// <returns></returns>
    /// <param name="text">The normalized text shared by both blocks.</param>
    /// <param name="hash">The hash shared by both blocks.</param>
    /// <param name="startLine">The first line of each block.</param>
    /// <param name="endLine">The last line of each block.</param>
    public static DuplicateCluster Cluster(string text, string hash, int startLine, int endLine)
    {
        var first = new BlockSpec(text)
        {
            Path = "/a.cs",
            Hash = hash,
            StartLine = startLine,
            EndLine = endLine
        };

        var second = new BlockSpec(text)
        {
            Path = "/b.cs",
            Hash = hash,
            StartLine = startLine,
            EndLine = endLine
        };

        var settings = new DetectionSettings
        {
            MinLines = 1
        };

        return DuplicateDetector.Build([Code.Block(first), Code.Block(second)], settings, cohesive: true);
    }

    /// <summary>
    ///     Distinct hashes, so only the near-duplicate pass and its maximums can claim these.
    /// </summary>
    /// <returns></returns>
    /// <param name="count"></param>
    public static IReadOnlyList<CodeBlock> Similar(int count)
    {
        var blocks = new List<CodeBlock>(count);
        for (var index = 0; index < count; index++)
        {
            var spec = new BlockSpec($"alpha beta gamma delta epsilon zeta eta theta v{index}")
            {
                Path = $"/repo/File{index}.cs",
                Project = $"Proj{index}",
                Hash = $"hash{index}",
                StartLine = (index * 20) + 1,
                EndLine = (index * 20) + 10
            };

            blocks.Add(Code.Block(spec));
        }

        return blocks;
    }

    /// <summary>
    ///     A three-file cluster spanning the given lines.
    /// </summary>
    /// <returns></returns>
    /// <param name="text">The normalized text shared by every block.</param>
    /// <param name="hash">The hash shared by every block.</param>
    /// <param name="startLine">The first line of each block.</param>
    /// <param name="endLine">The last line of each block.</param>
    public static DuplicateCluster WideCluster(string text, string hash, int startLine, int endLine)
    {
        var settings = new DetectionSettings
        {
            MinLines = 1
        };

        var blocks = new List<CodeBlock>(3);
        string[] paths = ["/a.cs", "/b.cs", "/c.cs"];
        foreach (var path in paths)
        {
            var spec = new BlockSpec(text)
            {
                Path = path,
                Hash = hash,
                StartLine = startLine,
                EndLine = endLine
            };

            blocks.Add(Code.Block(spec));
        }

        return DuplicateDetector.Build(blocks, settings, cohesive: true);
    }
}
