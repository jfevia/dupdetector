using DupDetector.Core.Pipeline;

namespace DupDetector.Reporting.Documents;

/// <summary>
///     Helpers for <see cref="ScopeDocument" />.
/// </summary>
public static class ScopeDocuments
{

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="scope"></param>
    public static ScopeDocument From(AnalysisScope scope)
    {

        var scopeDocument = new ScopeDocument
        {
            MinLines = scope.Settings.MinLines,
            MinTypeLines = scope.Settings.MinTypeLines,
            MinFileSpread = scope.Settings.MinFileSpread,
            MinProjectSpread = scope.Settings.MinProjectSpread,
            MaxFileSpread = scope.Settings.MaxFileSpread,
            MaxOccurrences = scope.Settings.MaxOccurrences,
            Similarity = scope.Settings.Similarity,
            Kinds = scope.Settings.Kinds.ToString().ToLowerInvariant(),
            IsExcludeTestFiles = scope.Settings.IsExcludeTestFiles,
            Suppressed = SuppressedDocuments.From(scope.Suppressed),
            Limitations = scope.Limitations,
        };
        return scopeDocument;
    }
}
