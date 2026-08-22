using Microsoft.CodeAnalysis;

namespace DupDetector.Core.Normalization;

/// <summary>
///     Collects the identifiers a block declares.
/// </summary>
public static class DeclaredNames
{
    /// <summary>
    ///     Collects every identifier declared within a node.
    /// </summary>
    /// <param name="node">The syntax node to walk.</param>
    /// <returns>The declared identifiers.</returns>
    public static HashSet<string> Collect(SyntaxNode node)
    {
        var collector = new DeclaredNameCollector();
        collector.Visit(node);
        return collector.Names;
    }
}
