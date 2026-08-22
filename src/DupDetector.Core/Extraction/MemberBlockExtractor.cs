using DupDetector.Core.Model;
using DupDetector.Core.Normalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Extraction;

/// <summary>
/// Extracts whole members, and optionally whole types, from a parsed file and normalizes each one.
/// </summary>
public static class MemberBlockExtractor
{
    /// <summary>
    /// Returns every declaration in <paramref name="unit"/> that matches the requested kinds and
    /// meets the minimum size.
    /// </summary>
    public static IReadOnlyList<CodeBlock> Extract(SourceUnit unit, DetectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(settings);

        var blocks = new List<CodeBlock>();

        foreach (var node in unit.Tree.GetRoot().DescendantNodes())
        {
            if (Describe(node) is not { } declaration || !settings.Kinds.HasFlag(declaration.Kind))
            {
                continue;
            }

            var span = node.GetLocation().GetLineSpan();
            var lines = new LineRange(span.StartLinePosition.Line + 1, span.EndLinePosition.Line + 1);

            var minimum = declaration.Kind == DetectionKind.Types ? settings.MinTypeLines : settings.MinLines;
            if (lines.Count < minimum)
            {
                continue;
            }

            var normalized = StructuralNormalizer.Normalize(node);
            blocks.Add(new CodeBlock(
                unit.Path,
                unit.Project,
                unit.IsTestFile,
                declaration.Name,
                lines,
                normalized.Hash,
                normalized.Text,
                node.ToString()));
        }

        return blocks;
    }

    /// <summary>
    /// Names a node and reports which detection kind it belongs to, or <c>null</c> when the node is
    /// not an extractable declaration.
    /// </summary>
    internal static (string Name, DetectionKind Kind)? Describe(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax method => (method.Identifier.ValueText, DetectionKind.Methods),
        ConstructorDeclarationSyntax constructor => (constructor.Identifier.ValueText, DetectionKind.Constructors),
        LocalFunctionStatementSyntax local => (local.Identifier.ValueText, DetectionKind.LocalFunctions),
        AccessorDeclarationSyntax accessor => (AccessorName(accessor), DetectionKind.Accessors),

        // Only when arrow-bodied: a block-bodied property is covered by its accessors, and matching
        // it here as well would report the same code twice.
        PropertyDeclarationSyntax { ExpressionBody: not null } property => (property.Identifier.ValueText, DetectionKind.Accessors),
        IndexerDeclarationSyntax { ExpressionBody: not null } => ("this[]", DetectionKind.Accessors),

        OperatorDeclarationSyntax op => ($"operator {op.OperatorToken.ValueText}", DetectionKind.Operators),
        ConversionOperatorDeclarationSyntax conversion => ($"operator {conversion.Type}", DetectionKind.Operators),
        DestructorDeclarationSyntax destructor => ($"~{destructor.Identifier.ValueText}", DetectionKind.Destructors),

        // Every type kind at once. Matching the shared base rather than each derived kind removes the
        // ordering hazard that a record, being also a class, would otherwise be labelled one.
        BaseTypeDeclarationSyntax type => ($"{Keyword(type)} {type.Identifier.ValueText}", DetectionKind.Types),

        _ => null,
    };

    /// <summary>
    /// The declaring keyword, taken from the source rather than from the node type, so
    /// <c>record struct</c> reads as <c>record</c>.
    /// </summary>
    private static string Keyword(BaseTypeDeclarationSyntax type) =>
        // An enum is the one type declaration that carries no shared Keyword token.
        type is TypeDeclarationSyntax declaration ? declaration.Keyword.ValueText : "enum";

    private static string AccessorName(AccessorDeclarationSyntax accessor)
    {
        var owner = accessor.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>() switch
        {
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            EventDeclarationSyntax evt => evt.Identifier.ValueText,
            _ => "?",
        };

        return $"{owner}.{accessor.Keyword.ValueText}";
    }
}
