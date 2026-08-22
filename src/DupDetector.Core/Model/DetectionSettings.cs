using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
///     Validated, immutable analysis settings. Every bound is enforced on assignment, so an invalid
///     configuration cannot be constructed.
/// </summary>
public sealed record DetectionSettings
{
    private readonly int _maxFileSpread;
    private readonly int _maxOccurrences;
    private readonly int _minFileSpread;
    private readonly int _minLines;
    private readonly int _minProductionDuplicateLines;
    private readonly int _minProjectSpread;
    private readonly int _minTypeLines;
    private readonly double _similarity;

    /// <summary>
    ///     Gets the settings used when the caller specifies nothing.
    /// </summary>
    public static DetectionSettings Default { get; }

    /// <summary>
    ///     Gets the globs whose matching clusters are suppressed after detection.
    /// </summary>
    public IReadOnlyList<string> ExcludeClusterFileGlobs { get; init; }

    /// <summary>
    ///     Gets the globs whose matching files are skipped before analysis.
    /// </summary>
    public IReadOnlyList<string> ExcludeFileGlobs { get; init; }

    /// <summary>
    ///     Gets the project name fragments whose clusters are suppressed.
    /// </summary>
    public IReadOnlyList<string> ExcludeProjectPatterns { get; init; }

    /// <summary>
    ///     Gets the source fragments whose clusters are suppressed.
    /// </summary>
    public IReadOnlyList<string> ExcludeSnippetPatterns { get; init; }

    /// <summary>
    ///     Gets a value indicating whether test files are excluded from the entire pipeline.
    /// </summary>
    public bool IsExcludeTestFiles { get; init; }

    /// <summary>
    ///     Gets the declaration kinds that are eligible for analysis.
    /// </summary>
    public DetectionKind Kinds { get; init; }

    /// <summary>
    ///     Gets the upper bound on near-duplicate file spread. Zero means unlimited.
    /// </summary>
    public int MaxFileSpread
    {
        get
        {
            return _maxFileSpread;
        }

        init
        {
            _maxFileSpread = Require.AtLeast(value, 0, nameof(MaxFileSpread));
        }
    }

    /// <summary>
    ///     Gets the upper bound on near-duplicate occurrences. Zero means unlimited.
    /// </summary>
    public int MaxOccurrences
    {
        get
        {
            return _maxOccurrences;
        }

        init
        {
            _maxOccurrences = Require.AtLeast(value, 0, nameof(MaxOccurrences));
        }
    }

    /// <summary>
    ///     Gets the fewest files a cluster must span before it is reported.
    /// </summary>
    public int MinFileSpread
    {
        get
        {
            return _minFileSpread;
        }

        init
        {
            _minFileSpread = Require.AtLeast(value, 1, nameof(MinFileSpread));
        }
    }

    /// <summary>
    ///     Gets the smallest block, in lines, that is eligible for analysis.
    /// </summary>
    public int MinLines
    {
        get
        {
            return _minLines;
        }

        init
        {
            _minLines = Require.AtLeast(value, 1, nameof(MinLines));
        }
    }

    /// <summary>
    ///     Gets the minimum average block size before a cluster can be a production duplicate.
    /// </summary>
    public int MinProductionDuplicateLines
    {
        get
        {
            return _minProductionDuplicateLines;
        }

        init
        {
            _minProductionDuplicateLines = Require.AtLeast(value, 1, nameof(MinProductionDuplicateLines));
        }
    }

    /// <summary>
    ///     Gets the fewest projects a cluster must span before it is reported.
    /// </summary>
    public int MinProjectSpread
    {
        get
        {
            return _minProjectSpread;
        }

        init
        {
            _minProjectSpread = Require.AtLeast(value, 1, nameof(MinProjectSpread));
        }
    }

    /// <summary>
    ///     Gets the smallest whole type, in lines, that is eligible for analysis.
    /// </summary>
    public int MinTypeLines
    {
        get
        {
            return _minTypeLines;
        }

        init
        {
            _minTypeLines = Require.AtLeast(value, 1, nameof(MinTypeLines));
        }
    }

    /// <summary>
    ///     Gets the Jaccard threshold for near-duplicate grouping. 1.0 disables that pass.
    /// </summary>
    public double Similarity
    {
        get
        {
            return _similarity;
        }

        init
        {
            _similarity = Require.InRange(value, 0.0, 1.0, nameof(Similarity));
        }
    }

    static DetectionSettings()
    {
        var defaults = new DetectionSettings();
        Default = defaults;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DetectionSettings"/> class with the defaults.
    /// </summary>
    public DetectionSettings()
    {
        _maxFileSpread = 20;
        _maxOccurrences = 50;
        _minFileSpread = 2;
        _minLines = 5;
        _minProductionDuplicateLines = 10;
        _minProjectSpread = 2;
        _minTypeLines = 8;
        _similarity = 0.90;

        ExcludeClusterFileGlobs = [];
        ExcludeFileGlobs = [];
        ExcludeProjectPatterns = [];
        ExcludeSnippetPatterns = [];
        Kinds = DetectionKind.All;
    }
}
