using DupDetector.Core.Model;

using Xunit;

namespace DupDetector.Core.Tests.Model;

/// <summary>
///     
/// </summary>
public class ProjectIdentityTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Equality_DistinguishesDifferentProjects()
    {
        Assert.NotEqual(ProjectIdentities.Named("Alpha"), ProjectIdentities.Named("Beta"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Equality_IgnoresCase()
    {
        Assert.Equal(ProjectIdentities.Named("Alpha"), ProjectIdentities.Named("ALPHA"));
        Assert.Equal(ProjectIdentities.Named("Alpha").GetHashCode(), ProjectIdentities.Named("ALPHA").GetHashCode());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Equals_HandlesNullAndForeignTypes()
    {
        var identity = ProjectIdentities.Named("Alpha");
        object text = "Alpha";
        object sameName = ProjectIdentities.Named("alpha");
        Assert.False(identity.Equals(null));
        Assert.False(identity.Equals(text));
        Assert.True(identity.Equals(sameName));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Named_ExposesTheName()
    {
        var identity = ProjectIdentities.Named("Alpha");
        Assert.Equal("Alpha", identity.Name);
        Assert.True(identity.IsKnown);
        Assert.Equal("Alpha", identity.ToString());
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Named_RejectsBlankNames()
    {
        Assert.Throws<ArgumentException>(() => ProjectIdentities.Named(" "));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Operators_MatchEqualsRatherThanReferenceIdentity()
    {
        Assert.True(ProjectIdentities.Named("Alpha") == ProjectIdentities.Named("ALPHA"));
        Assert.False(ProjectIdentities.Named("Alpha") != ProjectIdentities.Named("ALPHA"));
        Assert.True(ProjectIdentities.Named("Alpha") != ProjectIdentities.Named("Beta"));

        ProjectIdentity? nothing = null;
        ProjectIdentity? alsoNothing = null;
        Assert.True(nothing == alsoNothing);
        Assert.False(nothing == ProjectIdentities.Named("Alpha"));
        Assert.False(ProjectIdentities.Named("Alpha") == nothing);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Unknown_IsDistinctState()
    {
        Assert.Null(ProjectIdentity.Unknown.Name);
        Assert.False(ProjectIdentity.Unknown.IsKnown);
        Assert.Equal("<unknown>", ProjectIdentity.Unknown.ToString());
        Assert.Equal(0, ProjectIdentity.Unknown.GetHashCode());
    }
}
