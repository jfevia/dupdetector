using DupDetector.Core.Model;
using DupDetector.Reporting;
using System.Globalization;
using System.Text;

namespace DupDetector.Cli.CommandLine;

/// <summary>
///     Parses the command line.
/// </summary>
public static class ArgumentParser
{
    private static readonly DetectionSettings Defaults;

    /// <summary>
    ///     Every option, in help order.
    /// </summary>
    public static IReadOnlyList<OptionDefinition> Options { get; }

    static ArgumentParser()
    {
        Defaults = DetectionSettings.Default;

        Options =
        [
            OptionDefinitions.Value("--detect", "kinds", "Comma-separated kinds: methods, constructors, local-functions, accessors, operators, destructors, types, all", "all"),
            OptionDefinitions.Value("--min-lines", "int", "Smallest block, in lines, that is analysed", Text(Defaults.MinLines)),
            OptionDefinitions.Value("--min-type-lines", "int", "Smallest whole type, in lines, that is analysed", Text(Defaults.MinTypeLines)),
            OptionDefinitions.Value("--similarity", "0-1", "Near-duplicate threshold; 1 disables the near-duplicate pass", Text(Defaults.Similarity)),
            OptionDefinitions.Value("--min-file-spread", "int", "Discard clusters spanning fewer files than this", Text(Defaults.MinFileSpread)),
            OptionDefinitions.Value("--min-project-spread", "int", "Discard clusters spanning fewer projects than this", Text(Defaults.MinProjectSpread)),
            OptionDefinitions.Value("--max-file-spread", "int", "Discard near-duplicate clusters spanning more files than this; 0 for no limit", Text(Defaults.MaxFileSpread)),
            OptionDefinitions.Value("--max-occurrences", "int", "Discard near-duplicate clusters with more copies than this; 0 for no limit", Text(Defaults.MaxOccurrences)),
            OptionDefinitions.Value("--min-prod-lines", "int", "Smallest average size that can be flagged a production duplicate", Text(Defaults.MinProductionDuplicateLines)),
            OptionDefinitions.Repeatable("--exclude", "glob", "Skip matching files before analysis"),
            OptionDefinitions.Repeatable("--exclude-cluster", "glob", "Suppress clusters whose instances all match"),
            OptionDefinitions.Repeatable("--exclude-snippet", "text", "Suppress clusters whose source contains this text"),
            OptionDefinitions.Repeatable("--exclude-project", "text", "Suppress clusters confined to projects matching this text"),
            OptionDefinitions.Flag("--exclude-test-files", "Exclude test files from the entire run, not merely from the listings"),
            OptionDefinitions.Value("--format", "yaml|json|markup|sarif", "Output format", "yaml"),
            OptionDefinitions.Value("--output", "path", "Write to a file instead of standard output"),
            OptionDefinitions.Flag("--no-raw-snippets", "Omit verbatim source from the report"),
            OptionDefinitions.Value("--fail-on", "0-100", "Exit with code 3 when duplication reaches this percentage"),
            OptionDefinitions.Value("--baseline", "path", "Compare against a previous baseline and report what changed"),
            OptionDefinitions.Flag("--fail-on-new", "Exit with code 4 when duplication is new or has spread since the baseline"),
            OptionDefinitions.Value("--write-baseline", "path", "Write a JSON baseline for a later run to compare against"),
            OptionDefinitions.Flag("--verbose", "Report progress and diagnostics on standard error"),
            OptionDefinitions.Flag("--help", "Show this help and exit"),
            OptionDefinitions.Flag("--version", "Show the version and exit"),
        ];
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="name"></param>
    public static OptionDefinition? Find(string name)
    {
        foreach (var option in Options)
        {
            if (string.Equals(option.Name, name, StringComparison.Ordinal))
            {
                return option;
            }
        }

        return null;
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="version"></param>
    public static string HelpText(string version)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"dupdetector {version}")
            .AppendLine()
            .AppendLine("Usage: dupdetector <path> [<path>...] [options]")
            .AppendLine()
            .AppendLine("A path may be a directory, a .cs file, a .csproj, a .sln or a .slnx.")
            .AppendLine()
            .AppendLine("Options:");

        var width = 0;
        foreach (var option in Options)
        {
            width = Math.Max(width, option.Display.Length);
        }

        foreach (var option in Options)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {option.Display.PadRight(width)}  {option.HelpText}");
        }

        return builder.ToString();
    }

    /// <summary>
    ///     
    /// </summary>
    /// <returns></returns>
    /// <param name="args"></param>
    /// <param name="version"></param>
    public static ParseResult Parse(IReadOnlyList<string> args, string version)
    {

        var paths = new List<string>();
        var values = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var cursor = 0;
        while (cursor < args.Count)
        {
            var argument = args[cursor];
            cursor++;

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                paths.Add(argument);
                continue;
            }

            if (Find(argument) is not { } option)
            {
                return ParseResults.Failed($"Unknown option '{argument}'. Run --help to see the available options.");
            }

            if (option.Arity == OptionArity.None)
            {
                Add(values, option.Name, "true");
                continue;
            }

            if (cursor >= args.Count)
            {
                return ParseResults.Failed($"Option '{option.Name}' requires a <{option.ValueName}> value.");
            }

            Add(values, option.Name, args[cursor]);
            cursor++;
        }

        if (values.ContainsKey("--help"))
        {
            return ParseResults.Print(HelpText(version));
        }

        if (values.ContainsKey("--version"))
        {
            return ParseResults.Print(version);
        }

        if (paths.Count == 0)
        {
            return ParseResults.Failed($"At least one path is required.{Environment.NewLine}{Environment.NewLine}{HelpText(version)}");
        }

        return Build(paths, values);
    }

    private static void Add(Dictionary<string, List<string>> values, string name, string value)
    {
        if (!values.TryGetValue(name, out var existing))
        {
            existing = [];
            values[name] = existing;
        }

        existing.Add(value);
    }

    private static List<string> All(Dictionary<string, List<string>> values, string name)
    {
        return values.TryGetValue(name, out var found) ? found : [];
    }

    private static ParseResult Build(List<string> paths, Dictionary<string, List<string>> values)
    {
        try
        {
            if (!ReportFormats.CanTryParse(Single(values, "--format") ?? "yaml", out var format))
            {
                return ParseResults.Failed(
                    $"Unknown format '{Single(values, "--format")}'. Valid formats: {string.Join(", ", ReportFormats.Names)}.");
            }

            if (ParseKinds(Single(values, "--detect")) is not { } kinds)
            {
                return ParseResults.Failed(
                    "Unknown detection kind. Valid kinds: methods, constructors, local-functions, accessors, operators, destructors, all.");
            }

            var commandLineOptions = new CommandLineOptions
            {
                InputPaths = paths,
                Settings = BuildSettings(values, kinds),
                Format = format,
                OutputPath = Single(values, "--output"),
                IsIncludeRawSnippets = !values.ContainsKey("--no-raw-snippets"),
                IsVerbose = values.ContainsKey("--verbose"),
                FailOn = BuildFailOn(values),
                BaselinePath = Single(values, "--baseline"),
                IsFailOnNew = values.ContainsKey("--fail-on-new"),
                WriteBaselinePath = Single(values, "--write-baseline"),
            };
            return ParseResults.Parsed(commandLineOptions);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException)
        {
            return ParseResults.Failed(exception.Message);
        }
    }

    private static double? BuildFailOn(Dictionary<string, List<string>> values)
    {
        if (!values.ContainsKey("--fail-on"))
        {
            return null;
        }

        var threshold = Number(values, "--fail-on", 0.0);
        if (threshold is < 0 or > 100)
        {
            var formatException = new FormatException("Option '--fail-on' must be between 0 and 100.");
            throw formatException;
        }

        return threshold;
    }

    private static DetectionSettings BuildSettings(Dictionary<string, List<string>> values, DetectionKind kinds)
    {
        var settings = new DetectionSettings
        {
            Kinds = kinds,
            MinLines = Integer(values, "--min-lines", Defaults.MinLines),
            MinTypeLines = Integer(values, "--min-type-lines", Defaults.MinTypeLines),
            Similarity = Number(values, "--similarity", Defaults.Similarity),
            MinFileSpread = Integer(values, "--min-file-spread", Defaults.MinFileSpread),
            MinProjectSpread = Integer(values, "--min-project-spread", Defaults.MinProjectSpread),
            MaxFileSpread = Integer(values, "--max-file-spread", Defaults.MaxFileSpread),
            MaxOccurrences = Integer(values, "--max-occurrences", Defaults.MaxOccurrences),
            MinProductionDuplicateLines = Integer(values, "--min-prod-lines", Defaults.MinProductionDuplicateLines),
            IsExcludeTestFiles = values.ContainsKey("--exclude-test-files"),
            ExcludeFileGlobs = All(values, "--exclude"),
            ExcludeClusterFileGlobs = All(values, "--exclude-cluster"),
            ExcludeSnippetPatterns = All(values, "--exclude-snippet"),
            ExcludeProjectPatterns = All(values, "--exclude-project"),
        };

        return settings;
    }

    private static int Integer(Dictionary<string, List<string>> values, string name, int fallback)
    {
        if (Single(values, name) is not { } raw)
        {
            return fallback;
        }

        var formatException2 = new FormatException($"Option '{name}' expects a whole number but received '{raw}'.");
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw formatException2;
    }

    private static double Number(Dictionary<string, List<string>> values, string name, double fallback)
    {
        if (Single(values, name) is not { } raw)
        {
            return fallback;
        }

        var formatException3 = new FormatException($"Option '{name}' expects a number but received '{raw}'.");
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw formatException3;
    }

    private static DetectionKind? ParseKinds(string? value)
    {
        if (value is null)
        {
            return DetectionKind.All;
        }

        var kinds = DetectionKind.None;
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var resolved = part.ToLowerInvariant() switch
            {
                "methods" => DetectionKind.Methods,
                "constructors" => DetectionKind.Constructors,
                "local-functions" => DetectionKind.LocalFunctions,
                "accessors" => DetectionKind.Accessors,
                "operators" => DetectionKind.Operators,
                "destructors" => DetectionKind.Destructors,
                "types" => DetectionKind.Types,
                "all" => DetectionKind.All,
                _ => DetectionKind.None,
            };

            if (resolved == DetectionKind.None)
            {
                return null;
            }

            kinds |= resolved;
        }

        return kinds == DetectionKind.None ? DetectionKind.All : kinds;
    }

    private static string? Single(Dictionary<string, List<string>> values, string name)
    {
        return values.TryGetValue(name, out var found) ? found[^1] : null;
    }

    private static string Text(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Text(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
