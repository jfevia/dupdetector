using DupDetector.Core.Extraction;
using DupDetector.Core.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.TestKit;

/// <summary>
///     Fixture builders shared by the test suites.
/// </summary>
public static class Code
{
    /// <summary>
    ///     Builds a block directly, bypassing parsing, for detector-level fixtures.
    /// </summary>
    /// <param name="normalizedText">The structural form of the block.</param>
    /// <returns>The block.</returns>
    public static CodeBlock Block(string normalizedText)
    {
        var spec = new BlockSpec(normalizedText);
        return Block(spec);
    }

    /// <summary>
    ///     Builds a block directly, bypassing parsing, for detector-level fixtures.
    /// </summary>
    /// <param name="spec">The block to build.</param>
    /// <returns>The block.</returns>
    public static CodeBlock Block(BlockSpec spec)
    {
        var identity = spec.Project is null
            ? ProjectIdentity.Unknown
            : ProjectIdentities.Named(spec.Project);

        var lines = new LineRange(spec.StartLine, spec.EndLine);
        var location = new CodeLocation(spec.Path, identity, spec.IsTestFile, lines);
        var content = new BlockContent(spec.NormalizedText, spec.RawText ?? spec.NormalizedText);
        var block = new CodeBlock(location, spec.MemberName, spec.Hash, content);
        return block;
    }

    /// <summary>
    ///     Extracts blocks from source text using the default settings.
    /// </summary>
    /// <param name="text">The source to parse.</param>
    /// <returns>The extracted blocks.</returns>
    public static IReadOnlyList<CodeBlock> Blocks(string text)
    {
        var spec = new UnitSpec(text);
        return Blocks(spec);
    }

    /// <summary>
    ///     Extracts blocks from source text using the supplied settings.
    /// </summary>
    /// <param name="text">The source to parse.</param>
    /// <param name="settings">The extraction settings.</param>
    /// <returns>The extracted blocks.</returns>
    public static IReadOnlyList<CodeBlock> Blocks(string text, DetectionSettings settings)
    {
        var spec = new UnitSpec(text)
        {
            Settings = settings
        };
        return Blocks(spec);
    }

    /// <summary>
    ///     Extracts blocks from a described source unit.
    /// </summary>
    /// <param name="spec">The unit to parse.</param>
    /// <returns>The extracted blocks.</returns>
    public static IReadOnlyList<CodeBlock> Blocks(UnitSpec spec)
    {
        var fallback = new DetectionSettings
        {
            MinLines = 1
        };

        var settings = spec.Settings ?? fallback;
        var unit = Unit(spec);
        return MemberBlockExtractor.Extract(unit, settings);
    }

    /// <summary>
    ///     The member names of a set of blocks, in order.
    /// </summary>
    /// <param name="blocks">The blocks to name.</param>
    /// <returns>The member names.</returns>
    public static List<string> MemberNames(IEnumerable<CodeBlock> blocks)
    {
        var names = new List<string>();
        foreach (var block in blocks)
        {
            names.Add(block.MemberName);
        }

        return names;
    }

    /// <summary>
    ///     A method whose body is a number of distinct assignments.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="statementCount">How many assignments the body contains.</param>
    /// <returns>The source of a class holding that method.</returns>
    public static string Method(string name, int statementCount)
    {
        return Method(name, statementCount, "int");
    }

    /// <summary>
    ///     A method whose body is a number of distinct assignments.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="statementCount">How many assignments the body contains.</param>
    /// <param name="type">The parameter and return type.</param>
    /// <returns>The source of a class holding that method.</returns>
    public static string Method(string name, int statementCount, string type)
    {
        var statements = new List<string>(statementCount);
        for (var index = 0; index < statementCount; index++)
        {
            statements.Add($"        {type} v{index} = default;");
        }

        var body = string.Join(Environment.NewLine, statements);

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

    /// <summary>
    ///     Parses text into a source unit.
    /// </summary>
    /// <param name="text">The source to parse.</param>
    /// <returns>The parsed unit.</returns>
    public static SourceUnit Unit(string text)
    {
        var spec = new UnitSpec(text);
        return Unit(spec);
    }

    /// <summary>
    ///     Parses text into a source unit at a given path and project.
    /// </summary>
    /// <param name="text">The source to parse.</param>
    /// <param name="path">The file path.</param>
    /// <param name="project">The owning project name.</param>
    /// <returns>The parsed unit.</returns>
    public static SourceUnit Unit(string text, string path, string project)
    {
        var spec = new UnitSpec(text)
        {
            Path = path,
            Project = project
        };

        return Unit(spec);
    }

    /// <summary>
    ///     Parses a described source unit.
    /// </summary>
    /// <param name="spec">The unit to parse.</param>
    /// <returns>The parsed unit.</returns>
    public static SourceUnit Unit(UnitSpec spec)
    {
        var tree = CSharpSyntaxTree.ParseText(spec.Text, path: spec.Path);
        var identity = spec.Project is null
            ? ProjectIdentity.Unknown
            : ProjectIdentities.Named(spec.Project);

        var relative = spec.RelativePath ?? spec.Path.TrimStart('/');
        var origin = new SourceOrigin(relative, identity, spec.IsTestFile);
        var unit = new SourceUnit(spec.Path, spec.Text, tree, origin);
        return unit;
    }
}
