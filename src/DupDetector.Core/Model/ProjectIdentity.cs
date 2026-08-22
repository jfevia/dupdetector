namespace DupDetector.Core.Model;

/// <summary>
///     The project a source file belongs to, where unknown is a distinct state.
/// </summary>
public sealed class ProjectIdentity : IEquatable<ProjectIdentity>
{

    /// <summary>
    ///     Gets the identity used when the project could not be determined.
    /// </summary>
    public static ProjectIdentity Unknown { get; }

    /// <summary>
    ///     Gets a value indicating whether the project was determined.
    /// </summary>
    public bool IsKnown
    {
        get
        {
            return Name is not null;
        }
    }

    /// <summary>
    ///     Gets the project name, or <c>null</c> when unknown.
    /// </summary>
    public string? Name { get; }

    static ProjectIdentity()
    {
        var unknown = new ProjectIdentity(null);
        Unknown = unknown;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectIdentity"/> class.
    /// </summary>
    /// <param name="name">The project name, or <c>null</c> when unknown.</param>
    public ProjectIdentity(string? name)
    {
        Name = name;
    }

    /// <summary>
    ///     Compares two identities by name, ignoring case.
    /// </summary>
    /// <param name="other">The identity to compare against.</param>
    /// <returns><c>true</c> when both name the same project.</returns>
    public bool Equals(ProjectIdentity? other)
    {
        return other is not null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ProjectIdentity);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name ?? "<unknown>";
    }

    /// <inheritdoc/>
    public static bool operator !=(ProjectIdentity? left, ProjectIdentity? right)
    {
        return !(left == right);
    }

    /// <inheritdoc/>
    public static bool operator ==(ProjectIdentity? left, ProjectIdentity? right)
    {
        return left is null ? right is null : left.Equals(right);
    }
}
