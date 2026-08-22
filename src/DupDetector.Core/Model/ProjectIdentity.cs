using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
/// The project a source file belongs to. <see cref="Unknown"/> is a distinct state rather than an
/// empty name, so project spread can never be silently substituted with file spread.
/// </summary>
public sealed class ProjectIdentity : IEquatable<ProjectIdentity>
{
    private ProjectIdentity(string? name) => Name = name;

    /// <summary>The project could not be determined.</summary>
    public static ProjectIdentity Unknown { get; } = new(null);

    /// <summary>Project name, or <c>null</c> when unknown.</summary>
    public string? Name { get; }

    public bool IsKnown => Name is not null;

    public static ProjectIdentity Named(string name) => new(Require.NotBlank(name, nameof(name)));

    public static bool operator ==(ProjectIdentity? left, ProjectIdentity? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(ProjectIdentity? left, ProjectIdentity? right) => !(left == right);

    public bool Equals(ProjectIdentity? other) =>
        other is not null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as ProjectIdentity);

    public override int GetHashCode() =>
        Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public override string ToString() => Name ?? "<unknown>";
}
