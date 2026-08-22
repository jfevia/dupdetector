namespace DupDetector.Core.Model;

/// <summary>
///     Declaration kinds eligible for duplication analysis.
/// </summary>
[Flags]
public enum DetectionKind
{
    /// <summary>
    ///     
    /// </summary>
    None = 0,
    /// <summary>
    ///     
    /// </summary>
    Methods = 1 << 0,
    /// <summary>
    ///     
    /// </summary>
    Constructors = 1 << 1,
    /// <summary>
    ///     
    /// </summary>
    LocalFunctions = 1 << 2,

    /// <summary>
    ///     Property, indexer and event accessor bodies, including expression-bodied ones.
    /// </summary>
    Accessors = 1 << 3,

    /// <summary>
    ///     Operator and conversion-operator declarations.
    /// </summary>
    Operators = 1 << 4,
    /// <summary>
    ///     
    /// </summary>
    Destructors = 1 << 5,

    /// <summary>
    ///     Whole type declarations: classes, records and structs.
    /// </summary>
    Types = 1 << 6,

    /// <summary>
    ///     Every member kind. Does not include <see cref="Types"/>.
    /// </summary>
    Members = Methods | Constructors | LocalFunctions | Accessors | Operators | Destructors,

    /// <summary>
    ///     Every supported declaration kind.
    /// </summary>
    All = Members | Types,
}
