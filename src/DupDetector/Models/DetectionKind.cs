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

    /// <summary>All supported declaration kinds (default).</summary>
    All = Methods | Constructors | LocalFunctions,
}
