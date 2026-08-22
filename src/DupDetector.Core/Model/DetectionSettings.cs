using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
/// Validated, immutable analysis settings. Every bound is enforced on assignment, so an invalid
/// configuration cannot be constructed and downstream code never has to re-check.
/// </summary>
public sealed record DetectionSettings
{
    private readonly int _minLines = 5;
    private readonly int _minTypeLines = 8;
    private readonly double _similarity = 0.90;
    private readonly int _minFileSpread = 2;
    private readonly int _minProjectSpread = 2;
    private readonly int _maxFileSpread = 20;
    private readonly int _maxOccurrences = 50;
    private readonly int _minProductionDuplicateLines = 10;

    /// <summary>Settings used when the caller specifies nothing.</summary>
    public static DetectionSettings Default { get; } = new();

    /// <summary>Smallest block, in lines, that is eligible for analysis.</summary>
    public int MinLines
    {
        get => _minLines;
        init => _minLines = Require.AtLeast(value, 1, nameof(MinLines));
    }

    /// <summary>
    /// Smallest whole type, in lines, that is eligible for analysis. Higher than
    /// <see cref="MinLines"/> because a very small type carries too little structure to be
    /// meaningfully duplicated.
    /// </summary>
    public int MinTypeLines
    {
        get => _minTypeLines;
        init => _minTypeLines = Require.AtLeast(value, 1, nameof(MinTypeLines));
    }

    /// <summary>Jaccard threshold for near-duplicate grouping. 1.0 disables the near-duplicate pass.</summary>
    public double Similarity
    {
        get => _similarity;
        init => _similarity = Require.InRange(value, 0.0, 1.0, nameof(Similarity));
    }

    /// <summary>Clusters spanning fewer than this many files are discarded.</summary>
    public int MinFileSpread
    {
        get => _minFileSpread;
        init => _minFileSpread = Require.AtLeast(value, 1, nameof(MinFileSpread));
    }

    /// <summary>Clusters spanning fewer than this many projects are discarded.</summary>
    public int MinProjectSpread
    {
        get => _minProjectSpread;
        init => _minProjectSpread = Require.AtLeast(value, 1, nameof(MinProjectSpread));
    }

    /// <summary>Upper bound on near-duplicate file spread. Zero means unlimited.</summary>
    public int MaxFileSpread
    {
        get => _maxFileSpread;
        init => _maxFileSpread = Require.AtLeast(value, 0, nameof(MaxFileSpread));
    }

    /// <summary>Upper bound on near-duplicate occurrences. Zero means unlimited.</summary>
    public int MaxOccurrences
    {
        get => _maxOccurrences;
        init => _maxOccurrences = Require.AtLeast(value, 0, nameof(MaxOccurrences));
    }

    /// <summary>Minimum average block size before a cluster can be a production duplicate.</summary>
    public int MinProductionDuplicateLines
    {
        get => _minProductionDuplicateLines;
        init => _minProductionDuplicateLines = Require.AtLeast(value, 1, nameof(MinProductionDuplicateLines));
    }

    public DetectionKind Kinds { get; init; } = DetectionKind.All;

    /// <summary>When true, test files are excluded from the entire pipeline, not merely from the listings.</summary>
    public bool ExcludeTestFiles { get; init; }

    public IReadOnlyList<string> ExcludeFileGlobs { get; init; } = [];

    public IReadOnlyList<string> ExcludeSnippetPatterns { get; init; } = [];

    public IReadOnlyList<string> ExcludeClusterFileGlobs { get; init; } = [];

    public IReadOnlyList<string> ExcludeProjectPatterns { get; init; } = [];
}
