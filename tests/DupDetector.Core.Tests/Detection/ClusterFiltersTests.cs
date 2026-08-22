using DupDetector.Core.Matching;
using DupDetector.Core.Model;
using DupDetector.Core.Pipeline;
using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Detection;

/// <summary>
///     
/// </summary>
public class ClusterFiltersTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AllInstancesInMatchingProject_IsFalseWhenProjectIsUnknown()
    {
        var lineRange = new LineRange(1, 3);
        var codeLocation = new CodeLocation("/a.cs", ProjectIdentity.Unknown, false, lineRange);
        var codeInstance = new CodeInstance(codeLocation, "M", "h");
        var clusterSpread = new ClusterSpread(1, 0, false);
        var clusterMetrics = new ClusterMetrics(3, 1, clusterSpread);
        var cluster = new DuplicateCluster
        {
            Id = "dup-1",
            Instances = [codeInstance],
            Metrics = clusterMetrics,
            NormalizedSnippet = "n",
            RawSnippets = ["r"],
            IsCohesive = true,
            IsProductionDuplicate = false,
        };

        Assert.False(ClusterFilters.CanAllInstancesBeInMatchingProject(cluster, ["any"]));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AllInstancesInMatchingProject_RequiresEveryInstance()
    {
        var instanceSpec = new InstanceSpec("/a.cs", "Acme.Architecture.Tests");
        var instanceSpec2 = new InstanceSpec("/b.cs", "Other.Architecture.Tests");
        Assert.True(ClusterFilters.CanAllInstancesBeInMatchingProject(
            ClusterFixtures.Make([instanceSpec, instanceSpec2]), [".Architecture."]));

        var instanceSpec3 = new InstanceSpec("/a.cs", "Acme.Architecture.Tests");
        var instanceSpec4 = new InstanceSpec("/b.cs", "Acme.Core");
        Assert.False(ClusterFilters.CanAllInstancesBeInMatchingProject(
            ClusterFixtures.Make([instanceSpec3, instanceSpec4]), [".Architecture."]));

        var instanceSpec5 = new InstanceSpec("/a.cs", "P");
        Assert.False(ClusterFilters.CanAllInstancesBeInMatchingProject(ClusterFixtures.Make([instanceSpec5]), []));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void AllInstancesMatchGlob_KeepsClustersThatStraddleTheBoundary()
    {
        var globs = GlobSets.Parse(["**/Arch/*.cs"]);

        var instanceSpec6 = new InstanceSpec("/r/Arch/a.cs", "P");
        var instanceSpec7 = new InstanceSpec("/r/Arch/b.cs", "P");
        Assert.True(ClusterFilters.CanAllInstancesMatchGlob(ClusterFixtures.Make([instanceSpec6, instanceSpec7]), globs));
        var instanceSpec8 = new InstanceSpec("/r/Arch/a.cs", "P");
        var instanceSpec9 = new InstanceSpec("/r/Core/b.cs", "P");
        Assert.False(ClusterFilters.CanAllInstancesMatchGlob(ClusterFixtures.Make([instanceSpec8, instanceSpec9]), globs));
        var instanceSpec10 = new InstanceSpec("/r/Arch/a.cs", "P");
        Assert.False(ClusterFilters.CanAllInstancesMatchGlob(ClusterFixtures.Make([instanceSpec10]), GlobSet.Empty));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Apply_RemovesOnlyWhatEachRuleSelects()
    {
        var instanceSpec11 = new InstanceSpec("/r/Core/a.cs", "Acme.Core");
        var kept = ClusterFixtures.Make([instanceSpec11]);
        var instanceSpec12 = new InstanceSpec("/r/Arch/a.cs", "Acme.Architecture");
        var suppressed = ClusterFixtures.Make([instanceSpec12]);

        Assert.Equal([kept, suppressed], ClusterFilters.Apply([kept, suppressed], DetectionSettings.Default));

        Assert.Equal([kept], ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with
            {
                ExcludeClusterFileGlobs = ["**/Arch/*.cs"]
            }));

        Assert.Equal([kept], ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with
            {
                ExcludeProjectPatterns = [".Architecture"]
            }));

        Assert.Empty(ClusterFilters.Apply(
            [kept, suppressed],
            DetectionSettings.Default with
            {
                ExcludeSnippetPatterns = ["IArchRule"]
            }));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MatchesAnySnippetPattern_IsCaseInsensitive()
    {
        var instanceSpec13 = new InstanceSpec("/a.cs", "P");
        var cluster = ClusterFixtures.Make([instanceSpec13]);
        Assert.True(ClusterFilters.CanMatchAnySnippetPattern(cluster, ["iarchrule"]));
        Assert.False(ClusterFilters.CanMatchAnySnippetPattern(cluster, ["absent"]));
        Assert.False(ClusterFilters.CanMatchAnySnippetPattern(cluster, []));
    }
}
