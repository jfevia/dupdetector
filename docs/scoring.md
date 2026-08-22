# Scoring

Four levels, each answering a different question.

## Cluster severity

```
removableLines = lines * (occurrences - 1)
score          = 100 * ln(1 + removableLines) / ln(1 + 1000)
```

`score` is emitted on every cluster as `clusters[].score`, and the HTML report sorts by it.

`removableLines` is the concrete debt a cluster represents: the lines that disappear if every copy
but one is deleted. The curve is logarithmic so ordinary duplication sits mid-range and the top is
reserved for genuinely pervasive debt. `1000` removable lines scores 100.

| Cluster | removableLines | score |
|---|---:|---:|
| 36 lines copied once | 36 | 52.3 |
| 6 lines copied 11 times | 66 | 60.9 |
| 50 lines copied 9 times | 450 | 88.4 |

### Why not size × occurrences × spread

A product of three capped dimensions saturates at ordinary inputs and conflates unrelated shapes: a
36-line block copied twice and a 6-line block copied twelve times scored identically, though they
call for completely different responses. Deriving severity from removable lines separates them and
produces a number that means something on its own.

## File, project and run percentages

```
percentage = duplicateLines / totalLines * 100
```

`duplicateLines` counts **distinct** lines. `LineSpanMerger` merges overlapping and touching ranges
before counting, so a line covered by several clusters is counted once. The numerator therefore can
never exceed the denominator, and no clamp is needed to keep the value sane.

Because aggregation de-duplicates, a block belonging to more than one cluster does not distort file,
project or run percentages.

`totalLines` counts physical lines: an empty file has zero, and a trailing newline does not add a
phantom line.

### Physical lines versus analysable lines

The physical denominator includes blanks, comments, `using` directives and namespace lines, none of
which extraction can ever yield as a duplicate. That systematically understates duplication, so a
second figure is reported against analysable lines only:

```
codeDuplicationPercentage = totalDuplicateCodeLines / totalCodeLines * 100
```

A line counts as analysable when it carries at least one syntax token, decided from the parsed tree
rather than by text matching, so `//` inside a string literal is not mistaken for a comment. This is
the figure to compare against tools that measure NCLOC, such as SonarQube. Both are reported; neither
replaces the other.

## Rounding

Every percentage is rounded to two places with `MidpointRounding.AwayFromZero`, so `6.625` becomes
`6.63` rather than the banker's-rounded `6.62`.

## Labels

Applied to `codeDuplicationPercentage`, the analysable-line figure, because blanks and comments
cannot be duplicated and only dilute the rate. Falls back to the physical figure when no
analysable-line count was measured.

| Percentage | Label |
|---|---|
| below 3 | `low` |
| 3 to 10 | `medium` |
| 10 to 20 | `high` |
| 20 and above | `critical` |

These bands match the SonarQube "Sonar way" gate, which fails at 3% duplicated lines on new code, so
a codebase labelled `low` here will not quietly fail that gate elsewhere.

## Production duplicates

The name describes the cluster, not every instance: it is true when the cluster represents production
debt, even if some copies are tests.

`isProductionDuplicate` is true when **all** of:

- the cluster is exact (every member shares one structural hash);
- it spans at least two projects;
- its average size reaches `--min-prod-lines`;
- **at least one** instance is production code.

The last condition is deliberate. A test-file copy of genuinely duplicated production code does not
clear the flag, because the production debt is still real. It follows that a flagged cluster may
still pair a production file with a test file.

Because the flag requires an exact cluster, a large near-duplicate cluster carries no flag even when
it is the largest finding in a run. Sort by `score` rather than by this flag to rank work.

## Whole-type duplication

Types are extracted alongside members, under their own, higher minimum (`--min-type-lines`). Without
this, a small class whose every member falls below `--min-lines` is invisible even when it is copied
verbatim into dozens of files.

To avoid describing the same code twice, a cluster whose every instance sits inside an instance of a
larger cluster is suppressed, and the count appears in `scope.suppressed.containedInLargerCluster`.

## Project spread when projects are unknown

Project spread counts only instances whose project is known, and `projectSpreadKnown` records
whether every instance was measured. When it is false, `--min-project-spread` cannot be evaluated and
is not enforced; the run emits a warning instead. Enforcing it would empty the report on any tree
without project files, and substituting file spread would silently admit clusters the setting was
meant to suppress.
