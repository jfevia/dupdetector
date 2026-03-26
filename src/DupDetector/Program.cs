using Microsoft.Build.Locator;
using DupDetector;

// MSBuildLocator must be called before any Roslyn workspace creation.
MSBuildLocator.RegisterDefaults();

var options = ParseArgs(args);
if (string.IsNullOrEmpty(options.InputPath))
{
    Console.Error.WriteLine("Usage: dupdetector <path> [options]");
    Console.Error.WriteLine("  --solution <path>        Solution/project/directory path (alias for positional arg)");
    Console.Error.WriteLine("  --min-lines <int>        Minimum lines to consider (default: 5)");
    Console.Error.WriteLine("  --similarity <0-1>       Similarity threshold (default: 0.85)");
    Console.Error.WriteLine("  --format yaml|json|html  Output format (default: yaml)");
    Console.Error.WriteLine("  --output <path>          Write output to file instead of stdout");
    Console.Error.WriteLine("  --exclude <glob>         Exclude pattern (repeatable)");
    Console.Error.WriteLine("  --include-generated      Include auto-generated files");
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

    // 4. Build file-level and project-level line counts
    var fileLineCounts = documents
        .GroupBy(d => d.FilePath)
        .ToDictionary(
            g => g.Key,
            g => g.First().SourceText.Split('\n').Length);

    var fileDuplicateLines = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    foreach (var cluster in clusters)
    {
        foreach (var inst in cluster.Instances)
        {
            var dupLines = inst.EndLine - inst.StartLine + 1;
            fileDuplicateLines.TryGetValue(inst.File, out var existing);
            fileDuplicateLines[inst.File] = existing + dupLines;
        }
    }

    var fileScores = fileLineCounts
        .Select(kv =>
        {
            fileDuplicateLines.TryGetValue(kv.Key, out var dupLines);
            var score = kv.Value > 0 ? Math.Round(Math.Min(100.0, dupLines * 100.0 / kv.Value), 2) : 0.0;
            return new FileScore
            {
                File = kv.Key,
                DuplicateLines = dupLines,
                TotalLines = kv.Value,
                Score = score
            };
        })
        .OrderByDescending(f => f.Score)
        .ToList();

    // Project = directory containing the source files
    var projectGroups = fileLineCounts.Keys
        .GroupBy(f => Path.GetDirectoryName(f) ?? ".")
        .ToList();

    var projectScores = projectGroups
        .Select(g =>
        {
            var totalLines = g.Sum(f => fileLineCounts[f]);
            var dupLines = g.Sum(f =>
            {
                fileDuplicateLines.TryGetValue(f, out var d);
                return d;
            });
            var score = totalLines > 0 ? Math.Round(Math.Min(100.0, dupLines * 100.0 / totalLines), 2) : 0.0;
            return new ProjectScore
            {
                Project = g.Key,
                DuplicateLines = dupLines,
                TotalLines = totalLines,
                Score = score
            };
        })
        .OrderByDescending(p => p.Score)
        .ToList();

    // 5. Build report
    var distinctFiles = documents.Select(d => d.FilePath).Distinct().Count();
    var totalDuplicateLines = clusters.Sum(c => c.Metrics.Lines * c.Metrics.Occurrences);
    var totalLines = fileLineCounts.Values.Sum();
    var solutionScore = totalLines > 0
        ? Math.Round(Math.Min(100.0, totalDuplicateLines * 100.0 / totalLines), 2)
        : 0.0;
    var scoreLabel = solutionScore switch
    {
        < 10 => "low",
        < 30 => "medium",
        < 60 => "high",
        _ => "critical"
    };

    var report = new DetectionReport
    {
        Summary = new ReportSummary
        {
            TotalFiles = distinctFiles,
            TotalDuplicates = clusters.Count,
            TotalDuplicateLines = totalDuplicateLines,
            DuplicationScore = solutionScore,
            ScoreLabel = scoreLabel
        },
        Clusters = clusters,
        FileScores = fileScores,
        ProjectScores = projectScores
    };

    // 6. Render and output
    var reporter = new Reporter();
    var output = reporter.Render(report, options.Format);

    if (!string.IsNullOrEmpty(options.OutputPath))
    {
        var outDir = Path.GetDirectoryName(options.OutputPath);
        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
        await File.WriteAllTextAsync(options.OutputPath, output);
        Console.Error.WriteLine($"[info] Report written to: {options.OutputPath}");
    }
    else
    {
        Console.WriteLine(output);
    }
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
            case "--solution" when i + 1 < args.Length:
                opts.InputPath = args[++i];
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
            default:
                Console.Error.WriteLine($"[warn] Unknown argument: {args[i]}");
                break;
        }
        i++;
    }

    return opts;
}
