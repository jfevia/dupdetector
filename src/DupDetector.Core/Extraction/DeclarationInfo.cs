using DupDetector.Core.Model;

namespace DupDetector.Core.Extraction;

/// <summary>
///     What a declaration is called and which detection kind it belongs to.
/// </summary>
public sealed record DeclarationInfo
{

    /// <summary>
    ///     Gets the detection kind the declaration belongs to.
    /// </summary>
    public DetectionKind Kind { get; }

    /// <summary>
    ///     Gets the reported member name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeclarationInfo"/> class.
    /// </summary>
    /// <param name="name">The reported member name.</param>
    /// <param name="kind">The detection kind the declaration belongs to.</param>
    public DeclarationInfo(string name, DetectionKind kind)
    {
        Name = name;
        Kind = kind;
    }
}
