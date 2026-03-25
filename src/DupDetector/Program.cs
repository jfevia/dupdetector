using Microsoft.Build.Locator;
using DupDetector;

// MSBuildLocator must be called before any Roslyn workspace creation.
MSBuildLocator.RegisterDefaults();

var options = ParseArgs(args);
if (string.IsNullOrEmpty(options.InputPath))
{
    Console.Error.WriteLine("Usage: dupdetector <path> [options]");
    Console.Error.WriteLine("  --min-lines <int>      Minimum lines to consider (default: 5)");
    Console.Error.WriteLine("  --similarity <0-1>     Similarity threshold (default: 0.85)");
    Console.Error.WriteLine("  --format json|yaml     Output format (default: json)");
    Console.Error.WriteLine("  --exclude <glob>       Exclude pattern (repeatable)");
    Console.Error.WriteLine("  --include-generated    Include auto-generated files");
    return 1;
}

try
{
    // 1. Load source files
    var loader = new ProjectLoader(options);
    var documents = await loader.LoadAsync(options.InputPath);

    // 2. Extract code blocks
    var extractor = new FeatureExtractor();
    var allBlocks = new List<CodeBlock>();
    foreach (var (filePath, syntaxTree, sourceText) in documents)
    {
        var blocks = extractor.Extract(filePath, syntaxTree, sourceText, options.MinLines);
        allBlocks.AddRange(blocks);
    }

    // 3. Detect duplicates
    var detector = new DuplicateDetector();
    var clusters = detector.Detect(allBlocks, options.Similarity);

    // 4. Build report
    var distinctFiles = documents.Select(d => d.FilePath).Distinct().Count();
    var totalDuplicateLines = clusters.Sum(c => c.Metrics.Lines * c.Metrics.Occurrences);
    var report = new DetectionReport
    {
        Summary = new ReportSummary
        {
            TotalFiles = distinctFiles,
            TotalDuplicates = clusters.Count,
            TotalDuplicateLines = totalDuplicateLines
        },
        Clusters = clusters
    };

    // 5. Render and output
    var reporter = new Reporter();
    var output = reporter.Render(report, options.Format);
    Console.WriteLine(output);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[error] {ex.Message}");
    return 1;
}

static DetectionOptions ParseArgs(string[] args)
{
    var opts = new DetectionOptions();
    int i = 0;

    // First positional argument is the input path
    if (args.Length > 0 && !args[0].StartsWith("--"))
    {
        opts.InputPath = args[0];
        i = 1;
    }

    while (i < args.Length)
    {
        switch (args[i])
        {
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
            case "--exclude" when i + 1 < args.Length:
                opts.Exclude.Add(args[++i]);
                break;
            case "--include-generated":
                opts.IncludeGenerated = true;
                break;
            default:
                Console.Error.WriteLine($"[warn] Unknown argument: {args[i]}");
                break;
        }
        i++;
    }

    return opts;
}
