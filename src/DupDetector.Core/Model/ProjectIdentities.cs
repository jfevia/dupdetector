using DupDetector.Core.Internal;

namespace DupDetector.Core.Model;

/// <summary>
///     Creates <see cref="ProjectIdentity"/> values.
/// </summary>
public static class ProjectIdentities
{
    /// <summary>
    ///     Creates an identity for a named project.
    /// </summary>
    /// <param name="name">The project name, which must not be blank.</param>
    /// <returns>The identity for that project.</returns>
    public static ProjectIdentity Named(string name)
    {
        var identity = new ProjectIdentity(Require.NotBlank(name, nameof(name)));
        return identity;
    }
}
