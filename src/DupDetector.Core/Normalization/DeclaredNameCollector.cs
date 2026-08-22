using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Normalization;

/// <summary>
/// Collects the identifiers a block declares: locals, parameters, type parameters, pattern
/// designations and the block's own name. Only these are renamed during normalization, which is
/// what keeps two unrelated members that merely share a shape from hashing alike.
/// </summary>
internal sealed class DeclaredNameCollector : CSharpSyntaxWalker
{
    private readonly HashSet<string> _names = new(StringComparer.Ordinal);

    private DeclaredNameCollector()
    {
    }

    internal static HashSet<string> Collect(SyntaxNode node)
    {
        var collector = new DeclaredNameCollector();
        collector.Visit(node);
        return collector._names;
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        Add(node.Identifier);
        base.VisitMethodDeclaration(node);
    }

    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        Add(node.Identifier);
        base.VisitLocalFunctionStatement(node);
    }

    public override void VisitParameter(ParameterSyntax node)
    {
        Add(node.Identifier);
        base.VisitParameter(node);
    }

    public override void VisitTypeParameter(TypeParameterSyntax node)
    {
        Add(node.Identifier);
        base.VisitTypeParameter(node);
    }

    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        Add(node.Identifier);
        base.VisitVariableDeclarator(node);
    }

    public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
    {
        Add(node.Identifier);
        base.VisitSingleVariableDesignation(node);
    }

    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        Add(node.Identifier);
        base.VisitForEachStatement(node);
    }

    public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
    {
        Add(node.Identifier);
        base.VisitCatchDeclaration(node);
    }

    private void Add(SyntaxToken identifier) => _names.Add(identifier.ValueText);
}
