# Changelog

All notable changes to this project are documented here.
This project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] — 2026-08-22

Complete rewrite. The output schema, the CLI surface and the reported numbers all change.

### Fixed — reported duplication was understated

- **Whole types are analysed.** A class whose every member falls below `--min-lines` was invisible
  even when copied verbatim across dozens of files. On a 850-file codebase this alone surfaced a
  25-copy class spanning 5 projects that had been reported as no duplication at all. Types have
  their own minimum, `--min-type-lines`, and a cluster fully contained in a larger one is suppressed
  so the same code is never described twice.
- **`score` is emitted.** It was computed and documented but never serialized, so the HTML report
  silently fell back to `removableLines` and showed the same number in two columns.
- **Expression-bodied properties and indexers are extracted.** Their block-bodied equivalents
  already were, so behaviour depended on syntax rather than on semantics.

### Added

- **Scope disclosure.** Every report carries the thresholds the run applied, a per-reason count of
  the clusters they withheld, and plain-language limitations, so a low percentage cannot be read as
  a clean bill of health.
- **Analysable-line percentage.** `codeDuplicationPercentage` measures against lines carrying code
  rather than physical lines, which is the figure comparable with tools that report against NCLOC.
  Both are emitted; neither replaces the other.
- **Report provenance.** `metadata` records schema version, tool version, UTC timestamp, target
  paths, commit and command line.
- **Baseline comparison.** `--write-baseline` and `--baseline` gate on change rather than on an
  absolute threshold, which is what makes the tool usable on brownfield code. Comparison is keyed on
  cluster content, so a cluster that gains a copy is reported as spreading, not as a new finding.
- **SARIF output.** `--format sarif` emits SARIF 2.1.0 for GitHub code scanning, with the effective
  settings in `runs[].invocations[]` and the scope block in `run.properties`.
- **HTML report accessibility and data.** Sort headers are real buttons with `aria-sort`, contrast
  meets WCAG AA, the filter is labelled and debounced, and the page now renders project scores, the
  normalized shape of each cluster, per-file detail and linkable cluster ids.

### Changed — breaking

- **Severity bands aligned with industry defaults.** `low` is now below **3%**, `medium` 3–10%,
  `high` 10–20%, `critical` 20% and above, matching the SonarQube "Sonar way" gate. The previous
  `low <10` band let a codebase read `low` here while failing that gate everywhere else. The label
  is now derived from `codeDuplicationPercentage`, not the physical figure. **Existing reports will
  be relabelled.**
- **`--baseline` no longer fails a build on its own.** It reports what changed and exits 0. Pass
  `--fail-on-new` to gate, which exits **4** — distinct from the **3** that `--fail-on` returns, so
  a pipeline can tell new duplication apart from an absolute-threshold breach.

- **Normalization preserves type and member names.** Only declaration-site identifiers are renamed.
  Previously every identifier was erased, so unrelated mappers and handlers were reported as exact
  duplicates. See [docs/normalization.md](docs/normalization.md).
- **Clusters are maximal cliques, not connected components.** Similarity is not transitive; grouping
  by connectivity merged blocks that shared no tokens. A block may now belong to more than one
  cluster, and `isCohesive` reports when a group fell back to connectivity under budget.
- **Severity is derived from removable lines.** `score` replaces the capped product of size,
  occurrences and spread, which saturated at ordinary inputs and gave unrelated shapes the same
  number. See [docs/scoring.md](docs/scoring.md).
- **CLI renamed for coherence.** `--exclude-file-pattern` is now `--exclude-cluster`,
  `--exclude-pattern` is `--exclude-snippet`, `--exclude-project-pattern` is `--exclude-project`,
  `--min-cluster-spread` is `--min-file-spread`, `--max-cluster-spread` is `--max-file-spread`,
  `--max-cluster-occurrences` is `--max-occurrences`, and `--min-prod-dup-lines` is
  `--min-prod-lines`. `--solution` is removed; paths are positional and accepted anywhere.
- **Unknown options and missing values are fatal.** A typo previously warned and still exited 0.
- **`--exclude-test-files` excludes test files from the whole run**, including the summary, rather
  than only hiding them from the listings.
- **Sliding-window detection removed.** It was off by default, produced a very high false-positive
  rate, and was the cause of a crash at `--min-lines 0`.
- **Output schema reshaped.** `metrics` is flattened onto the cluster; `rawScore` is gone;
  `removableLines`, `isCohesive` and `projectSpreadKnown` are new.

### Added

- Exit code `3` and `--fail-on <0-100>` for build gating, plus `--help`, `--version` and `--verbose`.
- HTML report as a self-contained page that never embeds verbatim source.
- `--no-raw-snippets` for sharing reports safely.
- Accessors, operators, conversion operators and destructors are now analysed.

### Fixed

- `.slnx` projects already loaded as a transitive reference are no longer skipped. Previously every
  file they owned was silently dropped, which could hide a cross-project duplicate entirely.
- Source is parsed with an explicit language version, and parse failures are reported. Previously a
  single unrecognised construct silently discarded every valid member in the file.
- A permission-denied directory or a directory junction no longer aborts or hangs a scan.
- Generated-code markers are honoured only in the file header, so a file that merely mentions one no
  longer excludes itself.
- Trailing `**` in a glob now matches; `src/**` previously matched nothing.
- One glob engine backs both exclusion options, so a pattern means the same thing everywhere.
- Test-file classification matches whole words, so `Latest.cs` and `Contest.cs` stay production code,
  and uses root-relative paths, so a checkout under a directory named `test` is not misclassified.
- Line counting no longer adds a phantom line for a trailing newline.
- Unknown project is a distinct state; project spread is never fabricated from file spread.
- Verbatim copies keep `isExact` even when an upstream filter rerouted them.
- Cluster ids depend only on member content, so renaming a file does not change them.
- Empty collections serialize as `[]` in YAML as well as JSON, and numbers are culture-invariant.
- The near-duplicate pass is no longer all-pairs; pruning is exact, so no qualifying pair is lost.
- Project lookup is cached, removing the dominant cost of a directory scan.
- Cancellation is supported throughout.