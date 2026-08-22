using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DupDetector.Sources;

/// <summary>
/// Parses C# with an explicit language version.
/// </summary>
// LanguageVersion.Preview is pinned: the default tracks the shipped Roslyn and rejects newer syntax.
public static class SourceParser
{
    public static CSharpParseOptions Options { get; } = new(
        LanguageVersion.Preview,
        DocumentationMode.None,
        SourceCodeKind.Regular);

    public static SyntaxTree Parse(string text, string path) =>
        CSharpSyntaxTree.ParseText(text, Options, path);

    /// <summary>
    /// Returns a diagnostic when the file failed to parse, naming how many members survived so the
    /// loss is visible instead of silent.
    /// </summary>
    public static SourceDiagnostic? DescribeParseFailures(SyntaxTree tree, string path)
    {
        ArgumentNullException.ThrowIfNull(tree);

        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length == 0)
        {
            return null;
        }

        var first = errors[0];
        var line = first.Location.GetLineSpan().StartLinePosition.Line + 1;
        return SourceDiagnostic.Warning(
            $"{errors.Length} parse error(s); members in this file may be missing from the analysis. First at line {line}: {first.Id} {first.GetMessage(CultureInfo.InvariantCulture)}",
            path);
    }
}
