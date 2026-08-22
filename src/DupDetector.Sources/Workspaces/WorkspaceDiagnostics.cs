using Microsoft.CodeAnalysis;

namespace DupDetector.Sources.Workspaces;

/// <summary>
///     Translates workspace diagnostics into loader diagnostics.
/// </summary>
public static class WorkspaceDiagnostics
{
    /// <summary>
    ///     Returns <c>null</c> for notices that carry no information, such as a repeated project.
    /// </summary>
    /// <returns></returns>
    /// <param name="diagnostic"></param>
    public static SourceDiagnostic? Describe(WorkspaceDiagnostic diagnostic)
    {
        if (diagnostic.Message.Contains("already part of the workspace", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var sourceDiagnostic = new SourceDiagnostic(
            diagnostic.Kind == WorkspaceDiagnosticKind.Failure
                ? SourceDiagnosticSeverity.Error
                : SourceDiagnosticSeverity.Warning,
            diagnostic.Message,
            null);

        return sourceDiagnostic;
    }
}
