# dupdetector

A .NET 10 command-line tool that finds duplicated C# code and reports it as YAML, JSON or HTML.

## What it does

`dupdetector` parses C# with Roslyn, rewrites each member into a structural form, and groups members
that share that form. Verbatim copies are matched by hash; near-duplicates are matched by multiset
Jaccard similarity over an exact similarity join.

- **Exact and near-duplicate detection** — structural hashing plus a similarity join that is
  *complete*: it uses pruning rules that provably cannot discard a qualifying pair, so results match
  an exhaustive all-pairs comparison.
- **Cohesive clusters** — similarity is not transitive, so groups are maximal cliques rather than
  connected components. Every member of a reported cluster resembles every other member.
- **Debt-based severity** — a cluster's score is derived from the lines that would disappear if
  every copy but one were removed, not from an opaque product of unrelated dimensions.
- **Real project identity** — `.sln`, `.slnx` and `.csproj` inputs are loaded through MSBuild, so
  cross-project duplication is measured against actual projects.
- **Machine-readable output** — YAML, JSON, and a self-contained HTML report.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Getting started

```bash
git clone https://github.com/jfevia/dupdetector.git
cd dupdetector
dotnet build
dotnet test
```

### Install as a global tool

```bash
dotnet pack src/DupDetector.Cli/DupDetector.Cli.csproj -c Release -o ./artifacts/nupkg
dotnet tool install -g DupDetector --add-source ./artifacts/nupkg
```

## Usage

```
dupdetector <path> [<path>...] [options]
```

A path may be a directory, a `.cs` file, a `.csproj`, a `.sln` or a `.slnx`. Paths may appear
anywhere on the command line, before or after options.

| Option | Description | Default |
|---|---|---|
| `--detect <kinds>` | Comma-separated kinds: `methods`, `constructors`, `local-functions`, `accessors`, `operators`, `destructors`, `types`, `all` | `all` |
| `--min-lines <int>` | Smallest block, in lines, that is analysed | `5` |
| `--min-type-lines <int>` | Smallest whole type, in lines, that is analysed | `8` |
| `--similarity <0-1>` | Near-duplicate threshold; `1` disables the near-duplicate pass | `0.9` |
| `--min-file-spread <int>` | Discard clusters spanning fewer files than this | `2` |
| `--min-project-spread <int>` | Discard clusters spanning fewer projects than this | `2` |
| `--max-file-spread <int>` | Discard near-duplicate clusters spanning more files than this; `0` for no limit | `20` |
| `--max-occurrences <int>` | Discard near-duplicate clusters with more copies than this; `0` for no limit | `50` |
| `--min-prod-lines <int>` | Smallest average size that can be flagged a production duplicate | `10` |
| `--exclude <glob>` | Skip matching files before analysis (repeatable) | |
| `--exclude-cluster <glob>` | Suppress clusters whose instances all match (repeatable) | |
| `--exclude-snippet <text>` | Suppress clusters whose source contains this text (repeatable) | |
| `--exclude-project <text>` | Suppress clusters confined to matching projects (repeatable) | |
| `--exclude-test-files` | Exclude test files from the entire run, not merely from the listings | off |
| `--format <yaml\|json\|html\|sarif>` | Output format | `yaml` |
| `--output <path>` | Write to a file instead of standard output | stdout |
| `--no-raw-snippets` | Omit verbatim source from the report | included |
| `--fail-on <0-100>` | Exit with code 3 when duplication reaches this percentage | off |
| `--baseline <path>` | Compare against a previous baseline and report what changed | off |
| `--fail-on-new` | Exit with code 4 when duplication is new or has spread since the baseline | off |
| `--write-baseline <path>` | Record this run for a later comparison | off |
| `--verbose` | Report progress and diagnostics on standard error | off |
| `--help`, `--version` | Print and exit | |

Every option name, default and description above is generated from the same table the parser uses,
so `--help` and this document cannot disagree with the tool's behaviour.

### Globs

One engine backs both `--exclude` and `--exclude-cluster`, with gitignore semantics: matching is
case-insensitive, `*` stays inside a path segment, `**` spans segments, a bare name matches at any
depth, and naming a directory matches everything beneath it.

```bash
dupdetector ./src --exclude "**/obj/**" --exclude "Generated"
dupdetector MyApp.slnx --exclude-cluster "**/Arch/*.cs"
```

### Exit codes

| Code | Meaning |
|---:|---|
| `0` | Analysis completed; no threshold breached |
| `1` | The command line could not be understood |
| `2` | Analysis could not be completed |
| `3` | Duplication reached `--fail-on` |
| `4` | Duplication is new or has spread since the baseline, under `--fail-on-new` |

Unknown options and missing values are fatal. A typo never runs an analysis with silently different
settings.

### Examples

```bash
# Scan a solution
dupdetector MyApp.sln

# Cross-project duplication only, as JSON
dupdetector MyApp.slnx --min-project-spread 2 --format json --output dup.json

# Self-contained HTML report
dupdetector ./src --format html --output report.html

# Gate a build
dupdetector ./src --exclude-test-files --fail-on 15
```

## Understanding the output

See [docs/output-schema.md](docs/output-schema.md) for every field, [docs/scoring.md](docs/scoring.md)
for how severity is computed, [docs/normalization.md](docs/normalization.md) for what "structurally
identical" means, and [docs/architecture.md](docs/architecture.md) for how the pieces fit together.

Triage order:

1. **`score`** — the priority ranking; sorts the HTML table by default.
2. **`isProductionDuplicate`** — exact production code duplicated across projects. True when **any**
   instance is production, so a flagged cluster may still include a test copy.
3. **`removableLines`** — how many lines deduplicating this cluster would delete.
4. **`fileScores`** — which files are the densest hotspots.
5. **`isCohesive: false`** — a cluster grouped by connectivity because the clique budget was
   exhausted; its members may not all resemble one another.

Always read `scope.limitations` alongside the headline percentage: it states what the run did not
measure, and `scope.suppressed` counts the clusters each threshold withheld. Compare
`codeDuplicationPercentage`, not `duplicationPercentage`, against tools that measure NCLOC.

## CI integration

```yaml
- name: Detect duplication
  run: dupdetector ./src --exclude-test-files --fail-on 20 --format json --output dup.json

- uses: actions/upload-artifact@v4
  if: always()
  with:
    name: duplication-report
    path: dup.json
```

The `--fail-on` exit code removes the need to parse the report to decide whether a build passes.

### Gating brownfield code

An absolute threshold is unusable on a codebase that already has duplication, because every pull
request fails. Gate on change instead:

```yaml
- name: Detect new duplication
  run: dupdetector ./src --baseline dup-baseline.json --fail-on-new --format json --output dup.json
```

Without `--fail-on-new` the comparison is reported and the run still succeeds, which suits a
report-only step. With it, the run exits **4** — distinct from the **3** that `--fail-on` returns, so
a pipeline can tell new duplication apart from a codebase that was already over its absolute limit.
Refresh the baseline with
`--write-baseline`. Comparison is keyed on cluster content, not on cluster id, so a cluster that
gains a copy is reported as spreading rather than as an unrelated new finding.

### Code scanning

```yaml
- run: dupdetector ./src --format sarif --output dup.sarif
- uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: dup.sarif
```

> **Note:** reports include verbatim source by default so they are useful to tooling out of the box.
> Pass `--no-raw-snippets` before sharing a report outside your team. HTML reports never embed source.

## Dependencies

Roslyn and MSBuild for analysis, [YamlDotNet](https://github.com/aaubry/YamlDotNet) for YAML, and
`Microsoft.Extensions.Logging` for diagnostics. SonarAnalyzer runs at build time only.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). In short: `dotnet test` must pass and
`./build/Test-Coverage.ps1` must report 100% line and branch coverage.

## License

MIT — see [LICENSE](LICENSE).
