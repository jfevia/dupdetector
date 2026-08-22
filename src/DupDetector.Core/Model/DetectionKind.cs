namespace DupDetector.Core.Model;

/// <summary>
/// Declaration kinds eligible for duplication analysis.
/// </summary>
[Flags]
public enum DetectionKind
{
    None = 0,
    Methods = 1 << 0,
    Constructors = 1 << 1,
    LocalFunctions = 1 << 2,

    /// <summary>Property, indexer and event accessor bodies, including expression-bodied ones.</summary>
    Accessors = 1 << 3,

    /// <summary>Operator and conversion-operator declarations.</summary>
    Operators = 1 << 4,
    Destructors = 1 << 5,

    /// <summary>
    /// Whole type declarations: classes, records and structs.
    /// </summary>
    // Needed to see a small type copied verbatim: each of its members can sit below the minimum size.
    Types = 1 << 6,

    /// <summary>Every member kind. Does not include <see cref="Types"/>.</summary>
    Members = Methods | Constructors | LocalFunctions | Accessors | Operators | Destructors,

    /// <summary>Every supported declaration kind.</summary>
    All = Members | Types,
}
