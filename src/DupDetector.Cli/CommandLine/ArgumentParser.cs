using System.Globalization;
using System.Text;
using DupDetector.Core.Model;
using DupDetector.Reporting;

namespace DupDetector.Cli.CommandLine;

/// <summary>
/// A fully parsed command line, or the reason it could not be parsed.
/// </summary>
public sealed record CommandLineOptions
{
    public required IReadOnlyList<string> InputPaths { get; init; }

    public required DetectionSettings Settings { get; init; }

    public required ReportFormat Format { get; init; }

    public string? OutputPath { get; init; }

    public bool IncludeRawSnippets { get; init; } = true;

    public bool Verbose { get; init; }

    public double? FailOn { get; init; }

    /// <summary>Previous report to compare against, so a run reports change rather than absolute state.</summary>
    public string? BaselinePath { get; init; }

    /// <summary>Whether a baseline regression should fail the run rather than only be reported.</summary>
    public bool FailOnNew { get; init; }

    /// <summary>Where to record this run for a later comparison.</summary>
    public string? WriteBaselinePath { get; init; }
}

/// <summary>The outcome of parsing, which is either options, a message to print, or an error.</summary>
public sealed record ParseResult(CommandLineOptions? Options, string? Message, string? Error)
{
    public static ParseResult Parsed(CommandLineOptions options) => new(options, null, null);

    public static ParseResult Print(string message) => new(null, message, null);

    public static ParseResult Failed(string error) => new(null, null, error);
}

/// <summary>
/// Parses the command line.
/// </summary>
/// <remarks>
/// Unknown options and missing values are fatal, and each is reported for what it is. Silently
/// continuing turns a typo into a green run with the wrong settings.
/// </remarks>
public static class ArgumentParser
{
    private static readonly DetectionSettings Defaults = DetectionSettings.Default;

    /// <summary>Every option, in help order.</summary>
    public static IReadOnlyList<OptionDefinition> Options { get; } =
    [
        new("--detect", OptionArity.SingleValue, "kinds", "Comma-separated kinds: methods, constructors, local-functions, accessors, operators, destructors, types, all", "all"),
        new("--min-lines", OptionArity.SingleValue, "int", "Smallest block, in lines, that is analysed", Text(Defaults.MinLines)),
        new("--min-type-lines", OptionArity.SingleValue, "int", "Smallest whole type, in lines, that is analysed", Text(Defaults.MinTypeLines)),
        new("--similarity", OptionArity.SingleValue, "0-1", "Near-duplicate threshold; 1 disables the near-duplicate pass", Text(Defaults.Similarity)),
        new("--min-file-spread", OptionArity.SingleValue, "int", "Discard clusters spanning fewer files than this", Text(Defaults.MinFileSpread)),
        new("--min-project-spread", OptionArity.SingleValue, "int", "Discard clusters spanning fewer projects than this", Text(Defaults.MinProjectSpread)),
        new("--max-file-spread", OptionArity.SingleValue, "int", "Discard near-duplicate clusters spanning more files than this; 0 for no limit", Text(Defaults.MaxFileSpread)),
        new("--max-occurrences", OptionArity.SingleValue, "int", "Discard near-duplicate clusters with more copies than this; 0 for no limit", Text(Defaults.MaxOccurrences)),
        new("--min-prod-lines", OptionArity.SingleValue, "int", "Smallest average size that can be flagged a production duplicate", Text(Defaults.MinProductionDuplicateLines)),
        new("--exclude", OptionArity.Repeatable, "glob", "Skip matching files before analysis", null),
        new("--exclude-cluster", OptionArity.Repeatable, "glob", "Suppress clusters whose instances all match", null),
        new("--exclude-snippet", OptionArity.Repeatable, "text", "Suppress clusters whose source contains this text", null),
        new("--exclude-project", OptionArity.Repeatable, "text", "Suppress clusters confined to projects matching this text", null),
        new("--exclude-test-files", OptionArity.None, "", "Exclude test files from the entire run, not merely from the listings", null),
        new("--format", OptionArity.SingleValue, "yaml|json|html|sarif", "Output format", "yaml"),
        new("--output", OptionArity.SingleValue, "path", "Write to a file instead of standard output", null),
        new("--no-raw-snippets", OptionArity.None, "", "Omit verbatim source from the report", null),
        new("--fail-on", OptionArity.SingleValue, "0-100", "Exit with code 3 when duplication reaches this percentage", null),
        new("--baseline", OptionArity.SingleValue, "path", "Compare against a previous baseline and report what changed", null),
        new("--fail-on-new", OptionArity.None, "", "Exit with code 4 when duplication is new or has spread since the baseline", null),
        new("--write-baseline", OptionArity.SingleValue, "path", "Write a JSON baseline for a later run to compare against", null),
        new("--verbose", OptionArity.None, "", "Report progress and diagnostics on standard error", null),
        new("--help", OptionArity.None, "", "Show this help and exit", null),
        new("--version", OptionArity.None, "", "Show the version and exit", null),
    ];

    public static ParseResult Parse(IReadOnlyList<string> args, string version)
    {
        ArgumentNullException.ThrowIfNull(args);

        var paths = new List<string>();
        var values = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var cursor = 0;
        while (cursor < args.Count)
        {
            var argument = args[cursor];
            cursor++;

            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                // Positional paths are accepted anywhere, not only before the first option.
                paths.Add(argument);
                continue;
            }

            if (Find(argument) is not { } option)
            {
                return ParseResult.Failed($"Unknown option '{argument}'. Run --help to see the available options.");
            }

            if (option.Arity == OptionArity.None)
            {
                Add(values, option.Name, "true");
                continue;
            }

            if (cursor >= args.Count)
            {
                return ParseResult.Failed($"Option '{option.Name}' requires a <{option.ValueName}> value.");
            }

            Add(values, option.Name, args[cursor]);
            cursor++;
        }

        if (values.ContainsKey("--help"))
        {
            return ParseResult.Print(HelpText(version));
        }

        if (values.ContainsKey("--version"))
        {
            return ParseResult.Print(version);
        }

        if (paths.Count == 0)
        {
            return ParseResult.Failed($"At least one path is required.{Environment.NewLine}{Environment.NewLine}{HelpText(version)}");
        }

        return Build(paths, values);
    }

    public static string HelpText(string version)
    {
        var builder = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"dupdetector {version}")
            .AppendLine()
            .AppendLine("Usage: dupdetector <path> [<path>...] [options]")
            .AppendLine()
            .AppendLine("A path may be a directory, a .cs file, a .csproj, a .sln or a .slnx.")
            .AppendLine()
            .AppendLine("Options:");

        var width = Options.Max(option => option.Display.Length);
        foreach (var option in Options)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {option.Display.PadRight(width)}  {option.HelpText}");
        }

        return builder.ToString();
    }

    internal static OptionDefinition? Find(string name) =>
        Options.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal));

    private static void Add(Dictionary<string, List<string>> values, string name, string value)
    {
        if (!values.TryGetValue(name, out var existing))
        {
            existing = [];
            values[name] = existing;
        }

        existing.Add(value);
    }

    private static ParseResult Build(List<string> paths, Dictionary<string, List<string>> values)
    {
        try
        {
            if (!ReportFormats.TryParse(Single(values, "--format") ?? "yaml", out var format))
            {
                return ParseResult.Failed(
                    $"Unknown format '{Single(values, "--format")}'. Valid formats: {string.Join(", ", ReportFormats.Names)}.");
            }

            if (ParseKinds(Single(values, "--detect")) is not { } kinds)
            {
                return ParseResult.Failed(
                    "Unknown detection kind. Valid kinds: methods, constructors, local-functions, accessors, operators, destructors, all.");
            }

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
                ExcludeTestFiles = values.ContainsKey("--exclude-test-files"),
                ExcludeFileGlobs = All(values, "--exclude"),
                ExcludeClusterFileGlobs = All(values, "--exclude-cluster"),
                ExcludeSnippetPatterns = All(values, "--exclude-snippet"),
                ExcludeProjectPatterns = All(values, "--exclude-project"),
            };

            double? failOn = null;
            if (values.ContainsKey("--fail-on"))
            {
                var threshold = Number(values, "--fail-on", 0.0);
                if (threshold is < 0 or > 100)
                {
                    throw new FormatException("Option '--fail-on' must be between 0 and 100.");
                }

                failOn = threshold;
            }

            return ParseResult.Parsed(new CommandLineOptions
            {
                InputPaths = paths,
                Settings = settings,
                Format = format,
                OutputPath = Single(values, "--output"),
                IncludeRawSnippets = !values.ContainsKey("--no-raw-snippets"),
                Verbose = values.ContainsKey("--verbose"),
                FailOn = failOn,
                BaselinePath = Single(values, "--baseline"),
                FailOnNew = values.ContainsKey("--fail-on-new"),
                WriteBaselinePath = Single(values, "--write-baseline"),
            });
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException)
        {
            return ParseResult.Failed(exception.Message);
        }
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

    private static string? Single(Dictionary<string, List<string>> values, string name) =>
        values.TryGetValue(name, out var found) ? found[^1] : null;

    private static List<string> All(Dictionary<string, List<string>> values, string name) =>
        values.TryGetValue(name, out var found) ? found : [];

    private static int Integer(Dictionary<string, List<string>> values, string name, int fallback)
    {
        if (Single(values, name) is not { } raw)
        {
            return fallback;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"Option '{name}' expects a whole number but received '{raw}'.");
    }

    private static double Number(Dictionary<string, List<string>> values, string name, double fallback)
    {
        if (Single(values, name) is not { } raw)
        {
            return fallback;
        }

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new FormatException($"Option '{name}' expects a number but received '{raw}'.");
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Text(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
