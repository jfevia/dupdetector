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
    /// <remarks>
    /// Required to see a small type that is copied verbatim. Such a type is invisible to member-level
    /// extraction alone, because each of its members can sit below the minimum size while the type as
    /// a whole is substantial.
    /// </remarks>
    Types = 1 << 6,

    /// <summary>Every member kind. Does not include <see cref="Types"/>.</summary>
    Members = Methods | Constructors | LocalFunctions | Accessors | Operators | Destructors,

    /// <summary>Every supported declaration kind.</summary>
    All = Members | Types,
}
