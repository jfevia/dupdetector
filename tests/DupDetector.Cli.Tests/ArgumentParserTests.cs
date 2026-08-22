using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using DupDetector.Reporting;
using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>
///     
/// </summary>
public class ArgumentParserTests
{

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Defaults_MatchTheSettingsDefaults()
    {
        var options = CliFixtures.Options(["./src"]);

        Assert.True(options.IsIncludeRawSnippets);
        Assert.False(options.IsVerbose);
        Assert.Null(options.FailOn);
        Assert.Null(options.OutputPath);
        Assert.Equal(DetectionSettings.Default.MinLines, options.Settings.MinLines);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("methods", DetectionKind.Methods)]
    [InlineData("methods,accessors", DetectionKind.Methods | DetectionKind.Accessors)]
    [InlineData("constructors", DetectionKind.Constructors)]
    [InlineData("local-functions", DetectionKind.LocalFunctions)]
    [InlineData("operators", DetectionKind.Operators)]
    [InlineData("destructors", DetectionKind.Destructors)]
    [InlineData("all", DetectionKind.All)]
    public void DetectionKinds_AreParsed(string value, DetectionKind expected)
    {
        Assert.Equal(expected, CliFixtures.Options(["./src", "--detect", value]).Settings.Kinds);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void DetectionKinds_DefaultToAll()
    {
        Assert.Equal(DetectionKind.All, CliFixtures.Options(["./src"]).Settings.Kinds);
        Assert.Equal(DetectionKind.All, CliFixtures.Options(["./src", "--detect", " "]).Settings.Kinds);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Find_ReturnsNullForAnUnknownName()
    {
        Assert.Null(ArgumentParser.Find("--nope"));
        Assert.NotNull(ArgumentParser.Find("--min-lines"));
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void FlagOptions_AreApplied()
    {
        var options = CliFixtures.Options(["./src", "--exclude-test-files", "--no-raw-snippets", "--verbose"]);

        Assert.True(options.Settings.IsExcludeTestFiles);
        Assert.False(options.IsIncludeRawSnippets);
        Assert.True(options.IsVerbose);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Format_DefaultsToYaml()
    {
        Assert.Equal(ReportFormat.Yaml, CliFixtures.Options(["./src"]).Format);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Help_IsRecognisedOptionRatherThanError()
    {
        var result = CliFixtures.Parse(["--help"]);

        Assert.Null(result.Error);
        Assert.NotNull(result.Message);
        Assert.Contains("Usage: dupdetector", result.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void HelpText_IsGeneratedFromTheSameTableAsTheDefaults()
    {
        var help = ArgumentParser.HelpText("9.9.9");

        Assert.Contains($"default: {DetectionSettings.Default.MinLines}", help, StringComparison.Ordinal);
        Assert.Contains($"default: {DetectionSettings.Default.MinFileSpread}", help, StringComparison.Ordinal);
        Assert.All(ArgumentParser.Options, option => Assert.Contains(option.Name, help, StringComparison.Ordinal));
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="expected"></param>
    [Theory]
    [InlineData("yaml", ReportFormat.Yaml)]
    [InlineData("json", ReportFormat.Json)]
    [InlineData("html", ReportFormat.Html)]
    public void KnownFormats_AreAccepted(string value, ReportFormat expected)
    {
        Assert.Equal(expected, CliFixtures.Options(["./src", "--format", value]).Format);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MissingPath_IsAnError()
    {
        var result = CliFixtures.Parse([]);

        Assert.NotNull(result.Error);
        Assert.Contains("At least one path is required", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void MissingValue_IsReportedAsMissingValueNotUnknownOption()
    {
        var result = CliFixtures.Parse(["./src", "--min-lines"]);

        Assert.NotNull(result.Error);
        Assert.Contains("requires a <int> value", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fragment"></param>
    /// <param name="option"></param>
    [Theory]
    [InlineData("--min-lines", "abc", "whole number")]
    [InlineData("--similarity", "zzz", "number")]
    [InlineData("--fail-on", "nope", "number")]
    public void NonNumericValues_AreRejected(string option, string value, string fragment)
    {
        var result = CliFixtures.Parse(["./src", option, value]);

        Assert.NotNull(result.Error);
        Assert.Contains(fragment, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void NumericOptions_AreApplied()
    {
        var options = CliFixtures.Options([
            "./src",
            "--min-lines", "7",
            "--similarity", "0.75",
            "--min-file-spread", "3",
            "--min-project-spread", "4",
            "--max-file-spread", "0",
            "--max-occurrences", "0",
            "--min-prod-lines", "12",
            "--fail-on", "42.5"]);

        Assert.Equal(7, options.Settings.MinLines);
        Assert.Equal(0.75, options.Settings.Similarity);
        Assert.Equal(3, options.Settings.MinFileSpread);
        Assert.Equal(4, options.Settings.MinProjectSpread);
        Assert.Equal(0, options.Settings.MaxFileSpread);
        Assert.Equal(0, options.Settings.MaxOccurrences);
        Assert.Equal(12, options.Settings.MinProductionDuplicateLines);
        Assert.Equal(42.5, options.FailOn);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OptionDefinitions_RenderTheirOwnHelp()
    {
        var flag = OptionDefinitions.Flag("--flag", "A flag");
        var valued = OptionDefinitions.Value("--value", "int", "A value", "5");

        Assert.Equal("--flag", flag.Display);
        Assert.Equal("A flag", flag.HelpText);
        Assert.Equal("--value <int>", valued.Display);
        Assert.Equal("A value (default: 5)", valued.HelpText);
    }

    /// <summary>
    ///     
    /// </summary>
    /// <param name="value"></param>
    /// <param name="option"></param>
    [Theory]
    [InlineData("--min-lines", "0")]
    [InlineData("--min-lines", "-1")]
    [InlineData("--similarity", "1.5")]
    [InlineData("--min-file-spread", "0")]
    [InlineData("--max-file-spread", "-5")]
    [InlineData("--fail-on", "101")]
    [InlineData("--fail-on", "-1")]
    public void OutOfRangeValues_AreRejected(string option, string value)
    {
        Assert.NotNull(CliFixtures.Parse(["./src", option, value]).Error);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void OutputPath_IsCaptured()
    {
        Assert.Equal("report.yaml", CliFixtures.Options(["./src", "--output", "report.yaml"]).OutputPath);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void ParseResult_ExposesItsThreeShapes()
    {
        Assert.NotNull(ParseResults.Print("hello").Message);
        Assert.NotNull(ParseResults.Failed("bad").Error);
        Assert.NotNull(ParseResults.Parsed(CliFixtures.Options(["./src"])).Options);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void PositionalPathsAreAcceptedAnywhere()
    {
        Assert.Equal(["./src"], CliFixtures.Options(["--min-lines", "10", "./src"]).InputPaths);
        Assert.Equal(["./a", "./b"], CliFixtures.Options(["./a", "--min-lines", "10", "./b"]).InputPaths);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void RepeatableOptions_Accumulate()
    {
        var options = CliFixtures.Options([
            "./src",
            "--exclude", "**/obj/**",
            "--exclude", "**/bin/**",
            "--exclude-cluster", "**/Arch/*.cs",
            "--exclude-snippet", "IArchRule",
            "--exclude-project", ".Architecture."]);

        Assert.Equal(["**/obj/**", "**/bin/**"], options.Settings.ExcludeFileGlobs);
        Assert.Equal(["**/Arch/*.cs"], options.Settings.ExcludeClusterFileGlobs);
        Assert.Equal(["IArchRule"], options.Settings.ExcludeSnippetPatterns);
        Assert.Equal([".Architecture."], options.Settings.ExcludeProjectPatterns);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void SingleValueOptions_TakeTheLastOccurrence()
    {
        Assert.Equal(9, CliFixtures.Options(["./src", "--min-lines", "3", "--min-lines", "9"]).Settings.MinLines);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnknownDetectionKind_IsRejected()
    {
        var result = CliFixtures.Parse(["./src", "--detect", "windows"]);

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown detection kind", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnknownFormat_IsRejected()
    {
        var result = CliFixtures.Parse(["./src", "--format", "xml"]);

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown format 'xml'", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void UnknownOption_IsFatalRatherThanWarning()
    {
        var result = CliFixtures.Parse(["./src", "--min-lnes", "10"]);

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown option '--min-lnes'", result.Error, StringComparison.Ordinal);
    }

    /// <summary>
    ///     
    /// </summary>
    [Fact]
    public void Version_PrintsTheVersion()
    {
        var result = CliFixtures.Parse(["--version"]);

        Assert.Null(result.Error);
        Assert.Equal("9.9.9", result.Message);
    }
}
