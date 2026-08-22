# Output schema

YAML and JSON share one shape. Keys are camelCase in both.

## Top level

| Field | Type | Meaning |
|---|---|---|
| `summary` | object | Run totals |
| `clusters` | array | Duplicate groups, most severe first |
| `fileScores` | array | Per-file duplication, densest first |
| `projectScores` | array | Per-project duplication, densest first |
| `scope` | object | What the run measured, and what it found but withheld |
| `metadata` | object | Provenance for the run |

Empty collections are always `[]`, never `null`, in both formats.

## `summary`

| Field | Type | Meaning |
|---|---|---|
| `totalFiles` | int | Files analysed |
| `totalClusters` | int | Clusters reported after filtering |
| `totalDuplicateLines` | int | Distinct lines belonging to at least one cluster |
| `totalLines` | int | Physical lines across analysed files |
| `duplicationPercentage` | number | `totalDuplicateLines / totalLines * 100` |
| `totalCodeLines` | int | Analysable lines: blanks and comments excluded |
| `totalDuplicateCodeLines` | int | Duplicated lines that carry code |
| `codeDuplicationPercentage` | number | Duplication over analysable lines. Always the higher figure, and the one comparable with tools that measure against NCLOC |
| `label` | string | `low`, `medium`, `high` or `critical`, from `codeDuplicationPercentage` |
| `discoveredFiles` | int | Files seen before exclusions |
| `excludedFiles` | int | Files skipped by any rule |
| `discoveryMode` | string | `filesystem`, `workspace`, `mixed` or `none` |

## `clusters[]`

| Field | Type | Meaning |
|---|---|---|
| `id` | string | `dup-` plus a digest of the member hashes and sizes. Stable across runs and machines, and unaffected by renaming or adding unrelated files |
| `lines` | int | Average member size |
| `occurrences` | int | Number of copies |
| `fileSpread` | int | Distinct files |
| `projectSpread` | int | Distinct **known** projects |
| `projectSpreadKnown` | bool | False when some instance has no project; `--min-project-spread` is then not enforced |
| `removableLines` | int | Lines that disappear if every copy but one is removed |
| `score` | number | Priority ranking, 0-100. See [scoring](scoring.md) |
| `isExact` | bool | Every member shares one structural hash |
| `isCohesive` | bool | Every member resembles every other. False only when the grouping budget was exhausted |
| `isProductionDuplicate` | bool | **Any** instance is production code, not all of them. See [scoring](scoring.md) |
| `normalizedSnippet` | string | The shared structural form |
| `instances` | array | Where the copies are |
| `rawSnippets` | array | Verbatim source. Omitted entirely with `--no-raw-snippets`, and never present in HTML |

### `instances[]`

| Field | Type | Meaning |
|---|---|---|
| `file` | string | Absolute path |
| `project` | string | Project name, or `<unknown>` |
| `member` | string | Member name, such as `Total` or `Total.get` or `operator +` |
| `startLine`, `endLine` | int | Inclusive, one-based |
| `isTestFile` | bool | Classified as test code |
| `hash` | string | Structural hash |

## `fileScores[]` and `projectScores[]`

| Field | Type | Meaning |
|---|---|---|
| `file` / `project` | string | Identity |
| `duplicateLines` | int | Distinct duplicated lines |
| `totalLines` | int | Physical lines |
| `percentage` | number | Share of lines duplicated |
| `isTestFile` | bool | File scores only |
| `clusterCount` | int | File scores only: clusters touching this file |
| `widestClusterSpread` | int | File scores only: file spread of the widest cluster here |
| `codeLines` | int | File scores only: analysable lines |
| `duplicateCodeLines` | int | File scores only: duplicated analysable lines |

## `scope`

Published so a low percentage cannot be read as a clean bill of health without the thresholds that
produced it. Carries every active threshold (`minLines`, `minTypeLines`, `minFileSpread`,
`minProjectSpread`, `maxFileSpread`, `maxOccurrences`, `similarity`, `kinds`, `excludeTestFiles`),
plus:

| Field | Type | Meaning |
|---|---|---|
| `suppressed` | object | Per-reason counts of clusters found but not reported |
| `limitations` | array | Plain-language statements of what was not measured |

`suppressed` breaks down as `belowFileSpread`, `belowProjectSpread`, `aboveFileSpread`,
`aboveOccurrences`, `containedInLargerCluster`, `excludedBySnippetPattern`, `excludedByFileGlob`,
`excludedByProjectPattern`, and their `total`.

Note the asymmetry: `maxFileSpread` and `maxOccurrences` apply only to near-duplicate clusters, never
to exact ones. This is deliberate. They are a precision guard for the similarity join, where a
sub-1.0 threshold can assemble a large, weakly related clique. An exact cluster shares one structural
hash by construction, so it cannot be a false positive, and its width is the finding rather than the
noise — at the default limit of 20 files, applying these to exact clusters would discard a class
duplicated verbatim across 25 files.

## `metadata`

| Field | Type | Meaning |
|---|---|---|
| `schemaVersion` | string | Incremented when this shape changes |
| `toolVersion` | string | Version that produced the report |
| `generatedAtUtc` | string | ISO 8601, UTC |
| `targetPath` | string | Paths analysed |
| `commit` | string | Commit analysed, when `GITHUB_SHA` is set |
| `commandLine` | string | Arguments the run received |

## Guarantees

- **Deterministic.** Identical input produces byte-identical output, regardless of input order or
  thread scheduling.
- **Culture-invariant.** Numbers use `.` as the decimal separator in every locale.
- **Internally consistent.** Suppressed clusters are excluded from the summary too, so the totals
  always describe the clusters actually listed.

## Privacy

`rawSnippets` contains verbatim production source and is **included by default**. Pass
`--no-raw-snippets` before sharing a report outside your team. The HTML report never embeds source,
because it never displays it.
