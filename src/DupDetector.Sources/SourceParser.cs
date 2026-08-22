using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;

namespace DupDetector.Sources;

/// <summary>
///     Parses C# with an explicit language version.
/// </summary>
public static class SourceParser
{
    /// <summary>
    ///     
    /// </summary>
    public static CSharpParseOptions Options { get; }

    static SourceParser()
    {
        Options = new(
        LanguageVersion.Preview,
        DocumentationMode.None,
        SourceCodeKind.Regular);
    }

    /// <summary>
    ///     Returns a diagnostic when the file failed to parse, naming how many members survived so the
    ///     loss is visible instead of silent.
    /// </summary>
    /// <returns></returns>
    /// <param name="tree"></param>
    /// <param name="path"></param>
    public static SourceDiagnostic? DescribeParseFailures(SyntaxTree tree, string path)
    {

        Diagnostic? first = null;
        var errorCount = 0;
        foreach (var diagnostic in tree.GetDiagnostics())
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error)
            {
                continue;
            }

            first ??= diagnostic;
            errorCount++;
        }

        if (first is null)
        {
            return null;
        }

        var line = first.Location.GetLineSpan().StartLinePosition.Line + 1;
        return SourceDiagnostics.Warning(
            $"{errorCount} parse error(s); members in this file may be missing from the analysis. First at line {line}: {first.Id} {first.GetMessage(CultureInfo.InvariantCulture)}",
            path);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="text"></param>
    /// <param name="path"></param>
    public static SyntaxTree Parse(string text, string path)
    {
        return CSharpSyntaxTree.ParseText(text, Options, path);
    }
}
