using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.TestKit;

/// <summary>
/// Fixture builders shared by the test suites.
/// </summary>
public static class Code
{
    /// <summary>Parses <paramref name="text"/> into a <see cref="SourceUnit"/>.</summary>
    public static SourceUnit Unit(
        string text,
        string path = "/repo/File.cs",
        string? project = "Proj",
        bool isTestFile = false,
        string? relativePath = null)
    {
        var tree = CSharpSyntaxTree.ParseText(text, path: path);
        var identity = project is null ? ProjectIdentity.Unknown : ProjectIdentity.Named(project);
        return new SourceUnit(path, relativePath ?? path.TrimStart('/'), text, tree, identity, isTestFile);
    }

    /// <summary>Extracts blocks from source text using the supplied settings.</summary>
    public static IReadOnlyList<CodeBlock> Blocks(
        string text,
        DetectionSettings? settings = null,
        string path = "/repo/File.cs",
        string? project = "Proj",
        bool isTestFile = false)
        => MemberBlockExtractor.Extract(
            Unit(text, path, project, isTestFile),
            settings ?? new DetectionSettings { MinLines = 1 });

    /// <summary>Builds a block directly, bypassing parsing, for detector-level fixtures.</summary>
    public static CodeBlock Block(
        string normalizedText,
        string path = "/repo/File.cs",
        string? project = "Proj",
        bool isTestFile = false,
        string hash = "hash",
        int startLine = 1,
        int endLine = 10,
        string? memberName = null,
        string? rawText = null)
        => new(
            path,
            project is null ? ProjectIdentity.Unknown : ProjectIdentity.Named(project),
            isTestFile,
            memberName ?? "Member",
            new LineRange(startLine, endLine),
            hash,
            normalizedText,
            rawText ?? normalizedText);

    /// <summary>A method whose body is <paramref name="statementCount"/> distinct assignments.</summary>
    public static string Method(string name, int statementCount, string type = "int")
    {
        var body = string.Join(
            Environment.NewLine,
            Enumerable.Range(0, statementCount).Select(index => $"        {type} v{index} = default;"));

        return $$"""
            public class Holder
            {
                public {{type}} {{name}}({{type}} input)
                {
            {{body}}
                    return input;
                }
            }
            """;
    }
}
