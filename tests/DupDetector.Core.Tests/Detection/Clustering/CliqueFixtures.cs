using DupDetector.Core.Detection;

using DupDetector.TestKit;

using Xunit;

namespace DupDetector.Core.Tests.Detection.Clustering;

/// <summary>
///     Helpers for <see cref="CliqueGrouperTests" />.
/// </summary>
public static class CliqueFixtures
{
    /// <summary>
    ///     Fails unless every member of a group is joined to every other member.
    /// </summary>
    /// <param name="members"></param>
    /// <param name="edges"></param>
    public static void AssertFullyConnected(IReadOnlyList<int> members, IReadOnlyList<Edge> edges)
    {
        foreach (var left in members)
        {
            foreach (var right in members)
            {
                if (right != left)
                {
                    var edge = new Edge(Math.Min(left, right), Math.Max(left, right));
                    Assert.Contains(edge, edges);
                }
            }
        }
    }

    /// <summary>
    ///     The member indices of each group, in order.
    /// </summary>
    /// <returns></returns>
    /// <param name="groups"></param>
    public static IReadOnlyList<int[]> Members(IEnumerable<SimilarityGroup> groups)
    {
        var members = new List<int[]>();
        foreach (var group in groups)
        {
            var values = new int[group.Members.Count];
            for (var index = 0; index < group.Members.Count; index++)
            {
                values[index] = group.Members[index];
            }

            members.Add(values);
        }

        return members;
    }

    /// <summary>
    ///     A pair of block indices at full similarity.
    /// </summary>
    /// <returns></returns>
    /// <param name="left"></param>
    /// <param name="right"></param>
    public static SimilarPair Pair(int left, int right)
    {
        var value = new SimilarPair(left, right, 1.0);
        return value;
    }

    /// <summary>
    ///     A deterministic set of distinct undirected edges over the given node count.
    /// </summary>
    /// <returns></returns>
    /// <param name="attempts"></param>
    /// <param name="nodeCount"></param>
    public static List<Edge> RandomEdges(int attempts, int nodeCount)
    {
        var random = new Random(20260822);
        var edges = new List<Edge>();
        for (var index = 0; index < attempts; index++)
        {
            var left = random.Next(0, nodeCount);
            var right = random.Next(0, nodeCount);
            if (left == right)
            {
                continue;
            }

            var edge = new Edge(Math.Min(left, right), Math.Max(left, right));
            if (!edges.Contains(edge))
            {
                edges.Add(edge);
            }
        }

        return edges;
    }
}
