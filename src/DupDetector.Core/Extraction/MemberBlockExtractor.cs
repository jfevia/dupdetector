using DupDetector.Core.Model;
using DupDetector.Core.Normalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Extraction;

/// <summary>
///     Extracts whole members, and optionally whole types, from a parsed file and normalizes each one.
/// </summary>
public static class MemberBlockExtractor
{
    /// <summary>
    ///     Names a node and reports which detection kind it belongs to.
    /// </summary>
    /// <param name="node">The syntax node to classify.</param>
    /// <returns>The declaration, or <c>null</c> when the node is not extractable.</returns>
    public static DeclarationInfo? Describe(SyntaxNode node)
    {
        switch (node)
        {
            case MethodDeclarationSyntax method:
                return new DeclarationInfo(method.Identifier.ValueText, DetectionKind.Methods);
            case ConstructorDeclarationSyntax constructor:
                return new DeclarationInfo(constructor.Identifier.ValueText, DetectionKind.Constructors);
            case LocalFunctionStatementSyntax local:
                return new DeclarationInfo(local.Identifier.ValueText, DetectionKind.LocalFunctions);
            case AccessorDeclarationSyntax accessor:
                return new DeclarationInfo(AccessorName(accessor), DetectionKind.Accessors);
            case PropertyDeclarationSyntax { ExpressionBody: not null } property:
                return new DeclarationInfo(property.Identifier.ValueText, DetectionKind.Accessors);
            case IndexerDeclarationSyntax { ExpressionBody: not null }:
                return new DeclarationInfo("this[]", DetectionKind.Accessors);
            case OperatorDeclarationSyntax op:
                return new DeclarationInfo($"operator {op.OperatorToken.ValueText}", DetectionKind.Operators);
            case ConversionOperatorDeclarationSyntax conversion:
                return new DeclarationInfo($"operator {conversion.Type}", DetectionKind.Operators);
            case DestructorDeclarationSyntax destructor:
                return new DeclarationInfo($"~{destructor.Identifier.ValueText}", DetectionKind.Destructors);
            case BaseTypeDeclarationSyntax type:
                return new DeclarationInfo($"{Keyword(type)} {type.Identifier.ValueText}", DetectionKind.Types);
            default:
                return null;
        }
    }

    /// <summary>
    ///     Returns every declaration that matches the requested kinds and meets the minimum size.
    /// </summary>
    /// <param name="unit">The parsed source file to extract from.</param>
    /// <param name="settings">The kinds and size thresholds to apply.</param>
    /// <returns>The extracted blocks, in document order.</returns>
    public static IReadOnlyList<CodeBlock> Extract(SourceUnit unit, DetectionSettings settings)
    {
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
            var location = new CodeLocation(unit.Path, unit.Project, unit.IsTestFile, lines);
            var content = new BlockContent(normalized.Text, node.ToString());
            var block = new CodeBlock(location, declaration.Name, normalized.Hash, content);

            blocks.Add(block);
        }

        return blocks;
    }

    private static string AccessorName(AccessorDeclarationSyntax accessor)
    {
        var owner = accessor.FirstAncestorOrSelf<BasePropertyDeclarationSyntax>() switch
        {
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            EventDeclarationSyntax declaration => declaration.Identifier.ValueText,
            _ => "?",
        };

        return $"{owner}.{accessor.Keyword.ValueText}";
    }

    /// <summary>
    ///     The declaring keyword, taken from the source so <c>record struct</c> reads as <c>record</c>.
    /// </summary>
    /// <param name="type">The type declaration to name.</param>
    /// <returns>The keyword that introduced the type.</returns>
    private static string Keyword(BaseTypeDeclarationSyntax type)
    {
        return type is TypeDeclarationSyntax declaration ? declaration.Keyword.ValueText : "enum";
    }
}
