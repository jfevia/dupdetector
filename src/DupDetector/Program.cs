using Microsoft.Build.Locator;
using DupDetector;

// MSBuildLocator must be called before any Roslyn workspace creation.
MSBuildLocator.RegisterDefaults();

var options = CliArgParser.Parse(args);
if (options.InputPaths.Count == 0)
{
    Console.Error.WriteLine("Usage: dupdetector <path> [<path>...] [options]");
    Console.Error.WriteLine("  --solution <path>                   Solution/project/directory path (repeatable)");
    Console.Error.WriteLine("  --min-lines <int>                   Minimum lines to consider (default: 5)");
    Console.Error.WriteLine("  --similarity <0-1>                  Similarity threshold (default: 0.90)");
    Console.Error.WriteLine("  --format yaml|json|html             Output format (default: yaml)");
    Console.Error.WriteLine("  --output <path>                     Write output to file instead of stdout");
    Console.Error.WriteLine("  --exclude <glob>                    Exclude pattern (repeatable)");
    Console.Error.WriteLine("  --include-generated                 Include auto-generated files");
    Console.Error.WriteLine("  --detect <kinds>                    Comma-separated kinds: methods,constructors,local-functions,windows (default: all without windows)");
    Console.Error.WriteLine("  --max-cluster-spread <int>          Discard near-dup clusters with spread above this (default: 20, 0=unlimited)");
    Console.Error.WriteLine("  --min-cluster-spread <int>          Discard clusters with file spread below this (default: 2). Set to 1 to include same-file clusters");
    Console.Error.WriteLine("  --min-project-spread <int>          Discard clusters with project spread below this (default: 1). Set to 2 to suppress intra-project clusters");
    Console.Error.WriteLine("  --max-cluster-occurrences <int>     Discard near-dup clusters with occurrences above this (default: 50, 0=unlimited)");
    Console.Error.WriteLine("  --exclude-test-files                Omit test files from file/project score output");
    return 1;
}

try
{
    // 1. Load source files from all specified paths
    var loader = new ProjectLoader(options);
    var allDocs = new List<SourceDocument>();
    foreach (var inputPath in options.InputPaths)
    {
        var docs = await loader.LoadAsync(inputPath);
        allDocs.AddRange(docs);
    }
    // Deduplicate by file path (a file may be reachable via multiple input paths)
    var documents = allDocs
        .GroupBy(d => d.FilePath, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .ToList();

    // 2. Extract code blocks
    var extractor = new FeatureExtractor();
    var allBlocks = new List<CodeBlock>();
    foreach (var doc in documents)
    {
        var blocks = extractor.Extract(doc.FilePath, doc.SyntaxTree, doc.SourceText, options.MinLines, options.DetectionKinds, doc.ProjectName);
        allBlocks.AddRange(blocks);
    }

    // 3. Detect duplicates
    var detector = new DuplicateDetector();
    var clusters = detector.Detect(allBlocks, options.Similarity, options.MaxClusterSpread, options.MaxClusterOccurrences, options.MinClusterSpread, options.MinProjectSpread);

    // 4. Build file-level line counts
    var fileLineCounts = documents
        .GroupBy(d => d.FilePath)
        .ToDictionary(
            g => g.Key,
            g => g.First().SourceText.Split('\n').Length);

    // Collect all duplicate intervals per file using unique-line merging (GAP-1 & GAP-2 fix).
    // Previously, additive counting caused duplicateLines > totalLines in 269 files.
    var fileIntervals = new Dictionary<string, List<(int Start, int End)>>(StringComparer.OrdinalIgnoreCase);
    foreach (var cluster in clusters)
    {
        foreach (var inst in cluster.Instances)
        {
            if (!fileIntervals.TryGetValue(inst.File, out var list))
            {
                list = new List<(int, int)>();
                fileIntervals[inst.File] = list;
            }
            list.Add((inst.StartLine, inst.EndLine));
        }
    }

    var fileDuplicateLines = fileIntervals.ToDictionary(
        kv => kv.Key,
        kv => LineCountHelper.CountUniqueLines(kv.Value),
        StringComparer.OrdinalIgnoreCase);

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
                Score = score,
                IsTestFile = TestFileHelper.IsTestFile(kv.Key)
            };
        })
        .Where(f => !options.ExcludeTestFiles || !f.IsTestFile)
        .OrderByDescending(f => f.Score)
        .ToList();

    // 5. Build project-level scores grouped by actual .csproj project name (GAP-5 fix).
    // Previously grouped by directory path, creating hundreds of spurious "project" entries.
    var projectGroups = documents
        .GroupBy(d => d.ProjectName, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var projectScores = projectGroups
        .Where(g => !options.ExcludeTestFiles || !g.Any(d => TestFileHelper.IsTestFile(d.FilePath)))
        .Select(g =>
        {
            var totalLines = g.Sum(d =>
            {
                fileLineCounts.TryGetValue(d.FilePath, out var tl);
                return tl;
            });
            var dupLines = g.Sum(d =>
            {
                fileDuplicateLines.TryGetValue(d.FilePath, out var dl);
                return dl;
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

    // 6. Build solution-level summary using unique covered lines (GAP-1 fix).
    // Previously used sum(lines × occurrences) which grossly overcounted overlapping ranges.
    var distinctFiles = documents.Select(d => d.FilePath).Distinct().Count();
    var totalDuplicateLines = fileDuplicateLines.Values.Sum();
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

    // 7. Render and output
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
