using System.Text;
using DupDetector.Cli.CommandLine;
using DupDetector.Core.Model;
using DupDetector.Reporting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DupDetector.Cli.Tests;

/// <summary>Captures everything a run produces.</summary>
internal sealed class RecordingSink : IOutputSink
{
    internal StringBuilder Report { get; } = new();

    internal StringBuilder Messages { get; } = new();

    internal Dictionary<string, string> Saved { get; } = [];

    public void WriteReport(string content) => Report.Append(content);

    public void WriteMessage(string message) => Messages.Append(message);

    public void Save(string path, string content) => Saved[path] = content;
}

/// <summary>Captures log entries without a console.</summary>
internal sealed class RecordingLogger : ILogger
{
    internal List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));

    internal bool Contains(LogLevel level, string fragment) =>
        Entries.Exists(entry => entry.Level == level && entry.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

/// <summary>A disposable source tree with duplicated code in two projects.</summary>
internal sealed class Workspace : IDisposable
{
    private const string Duplicated = """
        namespace Sample;

        public class Calculator
        {
            public int Total(Order order)
            {
                var running = order.Price;
                var adjusted = running;
                var final = adjusted;
                return final;
            }
        }
        """;

    internal Workspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "dupdetector-cli-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(Root, "App"));
        Directory.CreateDirectory(Path.Combine(Root, "Lib"));

        File.WriteAllText(Path.Combine(Root, "App", "App.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(Root, "Lib", "Lib.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(Root, "App", "AppCalculator.cs"), Duplicated);
        File.WriteAllText(Path.Combine(Root, "Lib", "LibCalculator.cs"), Duplicated);
    }

    internal string Root { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }
}

public class ArgumentParserTests
{
    private static ParseResult Parse(params string[] args) => ArgumentParser.Parse(args, "9.9.9");

    private static CommandLineOptions Options(params string[] args)
    {
        var result = Parse(args);
        Assert.Null(result.Error);
        Assert.NotNull(result.Options);
        return result.Options;
    }

    [Fact]
    public void Parse_RejectsNull() => Assert.Throws<ArgumentNullException>(() => ArgumentParser.Parse(null!, "1"));

    [Fact]
    public void Help_IsARecognisedOptionRatherThanAnError()
    {
        var result = Parse("--help");

        Assert.Null(result.Error);
        Assert.NotNull(result.Message);
        Assert.Contains("Usage: dupdetector", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Version_PrintsTheVersion()
    {
        var result = Parse("--version");

        Assert.Null(result.Error);
        Assert.Equal("9.9.9", result.Message);
    }

    [Fact]
    public void HelpText_IsGeneratedFromTheSameTableAsTheDefaults()
    {
        var help = ArgumentParser.HelpText("9.9.9");

        // Each documented default is the value the parser actually applies.
        Assert.Contains($"default: {DetectionSettings.Default.MinLines}", help, StringComparison.Ordinal);
        Assert.Contains($"default: {DetectionSettings.Default.MinFileSpread}", help, StringComparison.Ordinal);
        Assert.All(ArgumentParser.Options, option => Assert.Contains(option.Name, help, StringComparison.Ordinal));
    }

    [Fact]
    public void MissingPath_IsAnError()
    {
        var result = Parse();

        Assert.NotNull(result.Error);
        Assert.Contains("At least one path is required", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownOption_IsFatalRatherThanAWarning()
    {
        var result = Parse("./src", "--min-lnes", "10");

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown option '--min-lnes'", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingValue_IsReportedAsAMissingValueNotAnUnknownOption()
    {
        var result = Parse("./src", "--min-lines");

        Assert.NotNull(result.Error);
        Assert.Contains("requires a <int> value", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void PositionalPathsAreAcceptedAnywhere()
    {
        Assert.Equal(["./src"], Options("--min-lines", "10", "./src").InputPaths);
        Assert.Equal(["./a", "./b"], Options("./a", "--min-lines", "10", "./b").InputPaths);
    }

    [Theory]
    [InlineData("--min-lines", "abc", "whole number")]
    [InlineData("--similarity", "zzz", "number")]
    [InlineData("--fail-on", "nope", "number")]
    public void NonNumericValues_AreRejected(string option, string value, string fragment)
    {
        var result = Parse("./src", option, value);

        Assert.NotNull(result.Error);
        Assert.Contains(fragment, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("--min-lines", "0")]
    [InlineData("--min-lines", "-1")]
    [InlineData("--similarity", "1.5")]
    [InlineData("--min-file-spread", "0")]
    [InlineData("--max-file-spread", "-5")]
    [InlineData("--fail-on", "101")]
    public void OutOfRangeValues_AreRejected(string option, string value)
    {
        Assert.NotNull(Parse("./src", option, value).Error);
    }

    [Fact]
    public void UnknownFormat_IsRejected()
    {
        var result = Parse("./src", "--format", "xml");

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown format 'xml'", result.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("yaml", ReportFormat.Yaml)]
    [InlineData("json", ReportFormat.Json)]
    [InlineData("html", ReportFormat.Html)]
    public void KnownFormats_AreAccepted(string value, ReportFormat expected) =>
        Assert.Equal(expected, Options("./src", "--format", value).Format);

    [Fact]
    public void Format_DefaultsToYaml() => Assert.Equal(ReportFormat.Yaml, Options("./src").Format);

    [Fact]
    public void UnknownDetectionKind_IsRejected()
    {
        var result = Parse("./src", "--detect", "windows");

        Assert.NotNull(result.Error);
        Assert.Contains("Unknown detection kind", result.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("methods", DetectionKind.Methods)]
    [InlineData("methods,accessors", DetectionKind.Methods | DetectionKind.Accessors)]
    [InlineData("constructors", DetectionKind.Constructors)]
    [InlineData("local-functions", DetectionKind.LocalFunctions)]
    [InlineData("operators", DetectionKind.Operators)]
    [InlineData("destructors", DetectionKind.Destructors)]
    [InlineData("all", DetectionKind.All)]
    public void DetectionKinds_AreParsed(string value, DetectionKind expected) =>
        Assert.Equal(expected, Options("./src", "--detect", value).Settings.Kinds);

    [Fact]
    public void DetectionKinds_DefaultToAll()
    {
        Assert.Equal(DetectionKind.All, Options("./src").Settings.Kinds);
        Assert.Equal(DetectionKind.All, Options("./src", "--detect", " ").Settings.Kinds);
    }

    [Fact]
    public void RepeatableOptions_Accumulate()
    {
        var options = Options(
            "./src",
            "--exclude", "**/obj/**",
            "--exclude", "**/bin/**",
            "--exclude-cluster", "**/Arch/*.cs",
            "--exclude-snippet", "IArchRule",
            "--exclude-project", ".Architecture.");

        Assert.Equal(["**/obj/**", "**/bin/**"], options.Settings.ExcludeFileGlobs);
        Assert.Equal(["**/Arch/*.cs"], options.Settings.ExcludeClusterFileGlobs);
        Assert.Equal(["IArchRule"], options.Settings.ExcludeSnippetPatterns);
        Assert.Equal([".Architecture."], options.Settings.ExcludeProjectPatterns);
    }

    [Fact]
    public void SingleValueOptions_TakeTheLastOccurrence() =>
        Assert.Equal(9, Options("./src", "--min-lines", "3", "--min-lines", "9").Settings.MinLines);

    [Fact]
    public void NumericOptions_AreApplied()
    {
        var options = Options(
            "./src",
            "--min-lines", "7",
            "--similarity", "0.75",
            "--min-file-spread", "3",
            "--min-project-spread", "4",
            "--max-file-spread", "0",
            "--max-occurrences", "0",
            "--min-prod-lines", "12",
            "--fail-on", "42.5");

        Assert.Equal(7, options.Settings.MinLines);
        Assert.Equal(0.75, options.Settings.Similarity);
        Assert.Equal(3, options.Settings.MinFileSpread);
        Assert.Equal(4, options.Settings.MinProjectSpread);
        Assert.Equal(0, options.Settings.MaxFileSpread);
        Assert.Equal(0, options.Settings.MaxOccurrences);
        Assert.Equal(12, options.Settings.MinProductionDuplicateLines);
        Assert.Equal(42.5, options.FailOn);
    }

    [Fact]
    public void FlagOptions_AreApplied()
    {
        var options = Options("./src", "--exclude-test-files", "--no-raw-snippets", "--verbose");

        Assert.True(options.Settings.ExcludeTestFiles);
        Assert.False(options.IncludeRawSnippets);
        Assert.True(options.Verbose);
    }

    [Fact]
    public void Defaults_MatchTheSettingsDefaults()
    {
        var options = Options("./src");

        Assert.True(options.IncludeRawSnippets);
        Assert.False(options.Verbose);
        Assert.Null(options.FailOn);
        Assert.Null(options.OutputPath);
        Assert.Equal(DetectionSettings.Default.MinLines, options.Settings.MinLines);
    }

    [Fact]
    public void OutputPath_IsCaptured() =>
        Assert.Equal("report.yaml", Options("./src", "--output", "report.yaml").OutputPath);

    [Fact]
    public void OptionDefinitions_RenderTheirOwnHelp()
    {
        var flag = new OptionDefinition("--flag", OptionArity.None, "", "A flag");
        var valued = new OptionDefinition("--value", OptionArity.SingleValue, "int", "A value", "5");

        Assert.Equal("--flag", flag.Display);
        Assert.Equal("A flag", flag.HelpText);
        Assert.Equal("--value <int>", valued.Display);
        Assert.Equal("A value (default: 5)", valued.HelpText);
    }

    [Fact]
    public void Find_ReturnsNullForAnUnknownName()
    {
        Assert.Null(ArgumentParser.Find("--nope"));
        Assert.NotNull(ArgumentParser.Find("--min-lines"));
    }

    [Fact]
    public void ParseResult_ExposesItsThreeShapes()
    {
        Assert.NotNull(ParseResult.Print("hello").Message);
        Assert.NotNull(ParseResult.Failed("bad").Error);
        Assert.NotNull(ParseResult.Parsed(Options("./src")).Options);
    }
}
