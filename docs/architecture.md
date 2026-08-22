# Architecture

Four projects, layered so that the analysis itself never touches the outside world.

```
DupDetector.Cli          composition, argument parsing, console, exit codes
   |          \
   |           +--> DupDetector.Reporting   YAML / JSON / HTML serialization
   |          /
   +--> DupDetector.Sources                 filesystem, encoding, MSBuild
            |
            v
        DupDetector.Core                    PURE: no I/O, no console, no MSBuild
```

## The rule

`Core` references only Roslyn. `Sources` and `Reporting` reference `Core` and never each other.
`Cli` is the only project permitted to touch `System.Console`.

This is enforced by project references rather than by convention, which is what makes the whole
analysis reachable from a test instead of only from a process.

## Core

| Namespace | Responsibility |
|---|---|
| `Model` | Immutable records. `ProjectIdentity` makes "unknown project" a distinct state rather than an empty string. `DetectionSettings` validates on assignment, so invalid settings cannot be represented. |
| `Matching` | The single glob engine and test-file classification. |
| `Normalization` | Rewrites a member into its structural form and hashes it in one pass. |
| `Extraction` | Turns a parsed file into `CodeBlock` values. |
| `Detection` | Token multisets, the similarity join, clique grouping, cluster construction, and the tally of what each threshold withheld. |
| `Scoring` | Line-span merging, cluster severity, file/project/run aggregation. |
| `Pipeline` | `AnalysisPipeline`, the cluster filters, and `AnalysisScope`, which publishes what the run did not measure. |

## Sources

Every filesystem and MSBuild concern lives here. Diagnostics are returned as **data**
(`SourceLoadResult.Diagnostics`) rather than written to a console, so loading is usable as a library
and testable without capturing output.

`IWorkspaceHost` isolates MSBuild behind one adapter, which is why the loading rules above it can be
exercised without an SDK installed.

## Reporting

`ReportDocument` is a dedicated serialization shape rather than the domain model, so the on-disk
schema stays stable when the model changes.

`JsonReportWriter` exposes two encoder profiles. `Standalone` is relaxed for readability;
`EmbeddedInMarkup` is pinned to the strict encoder because it is a security control — it escapes the
characters that would otherwise let source content close the surrounding element.

`SarifReportWriter` emits SARIF 2.1.0 for code-scanning ingestion, and `Baseline` records just enough
of a run — cluster content key, size and copy count — for a later run to report what changed.

## Cli

`Program` is wiring only and is the sole member excluded from coverage. Everything it does is one
call into `CliHost`, which is fully covered.

## Data flow

```
argv
 -> ArgumentParser          -> CommandLineOptions | help | error
 -> SourceLoader            -> SourceUnit[] + DiscoveryStats + diagnostics
 -> MemberBlockExtractor    -> CodeBlock[]        (members and whole types; trees released here)
 -> DuplicateDetector       -> exact pass, then similarity join + clique grouping
 -> ClusterFilters          -> suppression rules, including containment
 -> AggregateScorer         -> file -> project -> run percentages, physical and analysable
 -> IReportWriter           -> stdout or --output
 -> BaselineDelta           -> optional comparison against a previous run
 -> ExitCode
```

## Testing

Each project has a matching suite plus `DupDetector.TestKit` for fixtures.
`build/Test-Coverage.ps1` merges the per-suite coverage reports by canonical file path and fails
below 100% line or branch coverage.
