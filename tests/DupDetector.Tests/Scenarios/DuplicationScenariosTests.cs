using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Theory-based tests that verify the duplicate detector correctly identifies
/// duplication across 5000+ generated scenarios.
/// </summary>
public class DuplicationScenariosTests
{
    private readonly FeatureExtractor _extractor = new();
    private readonly DuplicateDetector _detector = new();

    /// <summary>
    /// Verifies that each scenario produces at least one duplicate cluster.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScenarioTestData.AllScenarios), MemberType = typeof(ScenarioTestData))]
    public void Detects_Duplication_In_Scenario(
        string scenarioName,
        string code1,
        string code2,
        int minLines,
        double similarity)
    {
        var tree1 = CSharpSyntaxTree.ParseText(code1);
        var tree2 = CSharpSyntaxTree.ParseText(code2);

        var blocks1 = _extractor.Extract("file1.cs", tree1, code1, minLines);
        var blocks2 = _extractor.Extract("file2.cs", tree2, code2, minLines);

        var allBlocks = blocks1.Concat(blocks2).ToList();
        var clusters = _detector.Detect(allBlocks, similarity);

        Assert.True(clusters.Count > 0, $"Expected to detect duplication for scenario: '{scenarioName}'");
    }
}
