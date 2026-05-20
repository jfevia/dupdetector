using Microsoft.CodeAnalysis;

namespace DupDetector;

/// <summary>
/// Represents a loaded C# source file, with its parsed syntax tree and the name of
/// the MSBuild project it belongs to.
/// </summary>
public record SourceDocument(
    string FilePath,
    SyntaxTree SyntaxTree,
    string SourceText,
    string ProjectName
);
