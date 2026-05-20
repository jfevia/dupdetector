namespace DupDetector;

/// <summary>
/// Specifies the kinds of code declarations to include in duplication detection.
/// </summary>
[Flags]
public enum DetectionKind
{
    /// <summary>No declarations selected.</summary>
    None = 0,

    /// <summary>Regular method declarations.</summary>
    Methods = 1,

    /// <summary>Constructor declarations.</summary>
    Constructors = 2,

    /// <summary>Local function declarations inside method bodies.</summary>
    LocalFunctions = 4,

    /// <summary>
    /// Sliding-window sub-method fragments. Produces overlapping <c>&lt;window@N&gt;</c> blocks
    /// inside method bodies. Disabled by default because it generates a very high rate of
    /// false-positive near-duplicate clusters. Enable explicitly with <c>--detect windows</c>.
    /// </summary>
    Windows = 8,

    /// <summary>All supported declaration kinds (default). Does NOT include Windows.</summary>
    All = Methods | Constructors | LocalFunctions,
}
