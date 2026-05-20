namespace DupDetector;

/// <summary>
/// Parses command-line arguments into a <see cref="DetectionOptions"/> instance.
/// Extracted from Program.cs to enable unit testing of CLI flag parsing.
/// </summary>
public static class CliArgParser
{
    public static DetectionOptions Parse(string[] args)
    {
        var opts = new DetectionOptions();
        int i = 0;
        bool detectExplicit = false;

        // Collect leading positional arguments as input paths (until the first --option)
        while (i < args.Length && !args[i].StartsWith("--"))
        {
            opts.InputPaths.Add(args[i]);
            i++;
        }

        while (i < args.Length)
        {
            switch (args[i])
            {
                case "--solution" when i + 1 < args.Length:
                    opts.InputPaths.Add(args[++i]);
                    break;
                case "--min-lines" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var ml)) opts.MinLines = ml;
                    break;
                case "--similarity" when i + 1 < args.Length:
                    if (double.TryParse(args[++i], System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var sim))
                        opts.Similarity = Math.Clamp(sim, 0.0, 1.0);
                    break;
                case "--format" when i + 1 < args.Length:
                    opts.Format = args[++i].ToLowerInvariant();
                    break;
                case "--output" when i + 1 < args.Length:
                    opts.OutputPath = args[++i];
                    break;
                case "--exclude" when i + 1 < args.Length:
                    opts.Exclude.Add(args[++i]);
                    break;
                case "--include-generated":
                    opts.IncludeGenerated = true;
                    break;
                case "--min-project-spread" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var minps)) opts.MinProjectSpread = Math.Max(1, minps);
                    break;
                case "--min-cluster-spread" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var mincs)) opts.MinClusterSpread = Math.Max(1, mincs);
                    break;
                case "--max-cluster-spread" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var mcs)) opts.MaxClusterSpread = mcs;
                    break;
                case "--max-cluster-occurrences" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var mco)) opts.MaxClusterOccurrences = mco;
                    break;
                case "--exclude-test-files":
                    opts.ExcludeTestFiles = true;
                    break;
                case "--exclude-pattern" when i + 1 < args.Length:
                    opts.ExcludePatterns.Add(args[++i]);
                    break;
                case "--detect" when i + 1 < args.Length:
                    // First --detect flag transitions from default (All) to an explicit inclusion set.
                    // Subsequent --detect flags accumulate into the same set.
                    if (!detectExplicit)
                    {
                        opts.DetectionKinds = DetectionKind.None;
                        detectExplicit = true;
                    }
                    foreach (var part in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var kind = part.ToLowerInvariant();
                        if (kind == "all")
                        {
                            opts.DetectionKinds = DetectionKind.All;
                            continue;
                        }
                        var resolved = kind switch
                        {
                            "methods" => DetectionKind.Methods,
                            "constructors" => DetectionKind.Constructors,
                            "local-functions" => DetectionKind.LocalFunctions,
                            "windows" => DetectionKind.Windows,
                            _ => DetectionKind.None
                        };
                        if (resolved == DetectionKind.None)
                            Console.Error.WriteLine($"[warn] Unknown detection kind '{part}'. Valid values: methods, constructors, local-functions, windows, all");
                        else
                            opts.DetectionKinds |= resolved;
                    }
                    break;
                default:
                    Console.Error.WriteLine($"[warn] Unknown argument: {args[i]}");
                    break;
            }
            i++;
        }

        return opts;
    }
}
