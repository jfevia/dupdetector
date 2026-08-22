using DupDetector.Core.Detection;
using DupDetector.Core.Model;

namespace DupDetector.Core.Pipeline;

/// <summary>
/// The boundaries of what a run actually measured.
/// </summary>
/// <remarks>
/// Published with every report so a low percentage cannot be read as a clean bill of health without
/// also seeing the thresholds that produced it.
/// </remarks>
public sealed record AnalysisScope
{
    public required DetectionSettings Settings { get; init; }

    public required SuppressionCounts Suppressed { get; init; }

    /// <summary>Plain-language statements of what the run excluded.</summary>
    public IReadOnlyList<string> Limitations
    {
        get
        {
            var notes = new List<string>
            {
                $"Only C# members of {Settings.MinLines} or more lines were analysed.",
                $"Whole types were analysed only at {Settings.MinTypeLines} or more lines.",
            };

            if (Settings.MinFileSpread > 1)
            {
                notes.Add($"Duplication confined to fewer than {Settings.MinFileSpread} files was excluded.");
            }

            if (Settings.MinProjectSpread > 1)
            {
                notes.Add($"Duplication confined to fewer than {Settings.MinProjectSpread} projects was excluded.");
            }

            if (Settings.MaxFileSpread > 0)
            {
                notes.Add($"Near-duplicate clusters spanning more than {Settings.MaxFileSpread} files were excluded.");
            }

            if (Settings.MaxOccurrences > 0)
            {
                notes.Add($"Near-duplicate clusters with more than {Settings.MaxOccurrences} copies were excluded.");
            }

            if (Settings.ExcludeTestFiles)
            {
                notes.Add("Test files were excluded from the run, including from the line totals.");
            }

            if ((Settings.Kinds & DetectionKind.Types) == 0)
            {
                notes.Add("Whole-type duplication was not analysed.");
            }

            notes.Add($"{Suppressed.Total} further clusters were found but withheld by these thresholds.");
            return notes;
        }
    }
}
