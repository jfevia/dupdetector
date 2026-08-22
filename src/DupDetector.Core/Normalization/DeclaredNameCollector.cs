using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DupDetector.Core.Normalization;

/// <summary>
///     Collects the identifiers a block declares, which are the only ones normalization renames.
/// </summary>
public sealed class DeclaredNameCollector : CSharpSyntaxWalker
{

    /// <summary>
    ///     Gets the identifiers collected so far.
    /// </summary>
    public HashSet<string> Names { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeclaredNameCollector"/> class.
    /// </summary>
    public DeclaredNameCollector()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        Names = names;
    }

    /// <inheritdoc/>
    public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
    {
        Add(node.Identifier);
        base.VisitCatchDeclaration(node);
    }

    /// <inheritdoc/>
    public override void VisitForEachStatement(ForEachStatementSyntax node)
    {
        Add(node.Identifier);
        base.VisitForEachStatement(node);
    }

    /// <inheritdoc/>
    public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        Add(node.Identifier);
        base.VisitLocalFunctionStatement(node);
    }

    /// <inheritdoc/>
    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        Add(node.Identifier);
        base.VisitMethodDeclaration(node);
    }

    /// <inheritdoc/>
    public override void VisitParameter(ParameterSyntax node)
    {
        Add(node.Identifier);
        base.VisitParameter(node);
    }

    /// <inheritdoc/>
    public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
    {
        Add(node.Identifier);
        base.VisitSingleVariableDesignation(node);
    }

    /// <inheritdoc/>
    public override void VisitTypeParameter(TypeParameterSyntax node)
    {
        Add(node.Identifier);
        base.VisitTypeParameter(node);
    }

    /// <inheritdoc/>
    public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
    {
        Add(node.Identifier);
        base.VisitVariableDeclarator(node);
    }

    private void Add(SyntaxToken identifier)
    {
        Names.Add(identifier.ValueText);
    }
}
