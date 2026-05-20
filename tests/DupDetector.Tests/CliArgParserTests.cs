using Xunit;

namespace DupDetector.Tests;

/// <summary>
/// Tests for <see cref="CliArgParser.Parse"/>.
/// Verifies that all CLI flags — including the new ones added to address the report
/// gaps — are parsed correctly into <see cref="DetectionOptions"/>.
/// </summary>
public class CliArgParserTests
{
    // ──── Default values ───────────────────────────────────────────────────────

    [Fact]
    public void Defaults_AreCorrect()
    {
        var opts = CliArgParser.Parse(["path/to/solution.sln"]);

        Assert.Equal(5, opts.MinLines);
        Assert.Equal(0.90, opts.Similarity, precision: 2);
        Assert.Equal("yaml", opts.Format);
        Assert.Equal(DetectionKind.All, opts.DetectionKinds);
        Assert.Equal(2, opts.MinClusterSpread);
        Assert.Equal(1, opts.MinProjectSpread);
        Assert.Equal(20, opts.MaxClusterSpread);
        Assert.Equal(50, opts.MaxClusterOccurrences);
        Assert.False(opts.IncludeGenerated);
        Assert.False(opts.ExcludeTestFiles);
        Assert.Empty(opts.Exclude);
        Assert.Empty(opts.OutputPath);
    }

    // ──── Positional input paths ───────────────────────────────────────────────

    [Fact]
    public void PositionalArgs_AreCollectedAsInputPaths()
    {
        var opts = CliArgParser.Parse(["solution.sln", "project.csproj"]);
        Assert.Equal(2, opts.InputPaths.Count);
        Assert.Contains("solution.sln", opts.InputPaths);
        Assert.Contains("project.csproj", opts.InputPaths);
    }

    [Fact]
    public void SolutionFlag_AddsToInputPaths()
    {
        var opts = CliArgParser.Parse(["--solution", "my.sln"]);
        Assert.Contains("my.sln", opts.InputPaths);
    }

    // ──── Existing flags ───────────────────────────────────────────────────────

    [Fact]
    public void MinLinesFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--min-lines", "8"]);
        Assert.Equal(8, opts.MinLines);
    }

    [Fact]
    public void SimilarityFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--similarity", "0.75"]);
        Assert.Equal(0.75, opts.Similarity, precision: 2);
    }

    [Fact]
    public void SimilarityFlag_IsClamped_BelowZero()
    {
        var opts = CliArgParser.Parse(["path.sln", "--similarity", "-0.5"]);
        Assert.Equal(0.0, opts.Similarity, precision: 2);
    }

    [Fact]
    public void SimilarityFlag_IsClamped_AboveOne()
    {
        var opts = CliArgParser.Parse(["path.sln", "--similarity", "1.5"]);
        Assert.Equal(1.0, opts.Similarity, precision: 2);
    }

    [Fact]
    public void FormatFlag_IsParsedLowercase()
    {
        var opts = CliArgParser.Parse(["path.sln", "--format", "JSON"]);
        Assert.Equal("json", opts.Format);
    }

    [Fact]
    public void OutputFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--output", "report.yaml"]);
        Assert.Equal("report.yaml", opts.OutputPath);
    }

    [Fact]
    public void ExcludeFlag_IsRepeatableAndAccumulates()
    {
        var opts = CliArgParser.Parse(["path.sln", "--exclude", "*.g.cs", "--exclude", "tests/**"]);
        Assert.Equal(2, opts.Exclude.Count);
        Assert.Contains("*.g.cs", opts.Exclude);
        Assert.Contains("tests/**", opts.Exclude);
    }

    [Fact]
    public void IncludeGeneratedFlag_SetsOption()
    {
        var opts = CliArgParser.Parse(["path.sln", "--include-generated"]);
        Assert.True(opts.IncludeGenerated);
    }

    // ──── --detect flag ────────────────────────────────────────────────────────

    [Fact]
    public void DetectMethods_SetsMethodsOnly()
    {
        var opts = CliArgParser.Parse(["path.sln", "--detect", "methods"]);
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Methods));
        Assert.False(opts.DetectionKinds.HasFlag(DetectionKind.Constructors));
        Assert.False(opts.DetectionKinds.HasFlag(DetectionKind.LocalFunctions));
        Assert.False(opts.DetectionKinds.HasFlag(DetectionKind.Windows));
    }

    [Fact]
    public void DetectWindows_SetsWindowsFlag()
    {
        var opts = CliArgParser.Parse(["path.sln", "--detect", "methods,windows"]);
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Methods));
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Windows));
    }

    [Fact]
    public void DetectAll_SetsAllFlag()
    {
        var opts = CliArgParser.Parse(["path.sln", "--detect", "all"]);
        Assert.Equal(DetectionKind.All, opts.DetectionKinds);
    }

    [Fact]
    public void DetectMultipleKinds_AccumulatesFlags()
    {
        var opts = CliArgParser.Parse(["path.sln", "--detect", "methods,constructors"]);
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Methods));
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Constructors));
        Assert.False(opts.DetectionKinds.HasFlag(DetectionKind.LocalFunctions));
    }

    [Fact]
    public void DetectRepeatableFlag_AccumulatesAcrossCalls()
    {
        var opts = CliArgParser.Parse(["path.sln", "--detect", "methods", "--detect", "constructors"]);
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Methods));
        Assert.True(opts.DetectionKinds.HasFlag(DetectionKind.Constructors));
    }

    // ──── New flags (GAP-4, GAP-6/7) ──────────────────────────────────────────

    [Fact]
    public void MaxClusterSpreadFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--max-cluster-spread", "15"]);
        Assert.Equal(15, opts.MaxClusterSpread);
    }

    [Fact]
    public void MaxClusterSpreadZero_DisablesFilter()
    {
        var opts = CliArgParser.Parse(["path.sln", "--max-cluster-spread", "0"]);
        Assert.Equal(0, opts.MaxClusterSpread);
    }

    [Fact]
    public void MaxClusterOccurrencesFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--max-cluster-occurrences", "25"]);
        Assert.Equal(25, opts.MaxClusterOccurrences);
    }

    [Fact]
    public void ExcludeTestFilesFlag_SetsOption()
    {
        var opts = CliArgParser.Parse(["path.sln", "--exclude-test-files"]);
        Assert.True(opts.ExcludeTestFiles);
    }

    // ──── --min-cluster-spread flag (GAP-2/3) ─────────────────────────────────

    [Fact]
    public void MinClusterSpreadDefault_IsTwo()
    {
        var opts = CliArgParser.Parse(["path.sln"]);
        Assert.Equal(2, opts.MinClusterSpread);
    }

    [Fact]
    public void MinClusterSpreadFlag_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--min-cluster-spread", "2"]);
        Assert.Equal(2, opts.MinClusterSpread);
    }

    [Fact]
    public void MinClusterSpreadFlag_HighValue_IsParsed()
    {
        var opts = CliArgParser.Parse(["path.sln", "--min-cluster-spread", "10"]);
        Assert.Equal(10, opts.MinClusterSpread);
    }

    [Fact]
    public void MinClusterSpreadFlag_Zero_ClampedToOne()
    {
        // 0 would mean "no clusters" which is never useful; clamp to 1
        var opts = CliArgParser.Parse(["path.sln", "--min-cluster-spread", "0"]);
        Assert.Equal(1, opts.MinClusterSpread);
    }

    [Fact]
    public void MinClusterSpread_CombinedWithMaxClusterSpread_BothParsed()
    {
        var opts = CliArgParser.Parse([
            "path.sln",
            "--min-cluster-spread", "2",
            "--max-cluster-spread", "15"
        ]);
        Assert.Equal(2, opts.MinClusterSpread);
        Assert.Equal(15, opts.MaxClusterSpread);
    }

    // ──── No input paths ─────────────────────────────────────────────────────

    [Fact]
    public void NoInputPaths_ReturnsEmptyInputPaths()
    {
        var opts = CliArgParser.Parse([]);
        Assert.Empty(opts.InputPaths);
    }

    // ──── Combined real-world invocations ─────────────────────────────────────

    [Fact]
    public void RealWorldInvocation_ParsesCorrectly()
    {
        var opts = CliArgParser.Parse([
            "solution.slnx",
            "--format", "html",
            "--output", "report.html",
            "--min-lines", "6",
            "--similarity", "0.90",
            "--max-cluster-spread", "20",
            "--max-cluster-occurrences", "50",
            "--exclude-test-files",
            "--exclude", "**/*.g.cs"
        ]);

        Assert.Contains("solution.slnx", opts.InputPaths);
        Assert.Equal("html", opts.Format);
        Assert.Equal("report.html", opts.OutputPath);
        Assert.Equal(6, opts.MinLines);
        Assert.Equal(0.90, opts.Similarity, precision: 2);
        Assert.Equal(20, opts.MaxClusterSpread);
        Assert.Equal(50, opts.MaxClusterOccurrences);
        Assert.True(opts.ExcludeTestFiles);
        Assert.Contains("**/*.g.cs", opts.Exclude);
    }

    [Fact]
    public void RealWorldInvocation_WithMinClusterSpread_ParsesCorrectly()
    {
        var opts = CliArgParser.Parse([
            "solution.slnx",
            "--format", "yaml",
            "--min-lines", "5",
            "--similarity", "0.90",
            "--min-cluster-spread", "2",
            "--max-cluster-spread", "20",
            "--max-cluster-occurrences", "50"
        ]);

        Assert.Contains("solution.slnx", opts.InputPaths);
        Assert.Equal(2, opts.MinClusterSpread);
        Assert.Equal(20, opts.MaxClusterSpread);
        Assert.Equal(50, opts.MaxClusterOccurrences);
    }
}
