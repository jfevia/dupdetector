# Audit findings that were disproven

An audit of the reporting pipeline raised four defects that were subsequently **investigated and
disproven**. They are recorded here so they are not re-litigated or "fixed".

## 1. "Cluster ids are null"

**False.** The JSON property is `id`, not `clusterId`. All clusters carry a populated, content-derived
identifier such as `dup-b8a59d9a1619`. The original claim came from querying a property name that
does not exist, which yields null for every row.

## 2. "`endLine` does not equal `startLine + lines - 1`"

**Correct by design.** `lines` is the *rounded average* of the member sizes in the cluster, which
[output-schema.md](output-schema.md) documents as "Average member size". Near-duplicate members
legitimately differ in length, so individual instances diverging from the average is expected. The
equality only holds for a cluster whose members happen to be the same size.

## 3. "Clusters are never sorted"

**False.** The sort lives in `DuplicateDetector.Detect`, which orders by removable lines descending
before returning. `ClusterFilters.Apply` uses `Where`, which preserves order, so the ordering
survives to the report. Checking `AnalysisPipeline` alone misses it.

## 4. "Static constructors are skipped because they are `StaticConstructorDeclarationSyntax`"

**False.** No such type exists in Roslyn. A static constructor is an ordinary
`ConstructorDeclarationSyntax` carrying a `static` modifier, so the existing constructor arm already
matches it.

## Related false alarm

A reviewer counted 65 methods named `CreateWorldState` and concluded the tool had under-clustered
them. Those overloads take different parameters and build different values; they are not copies of
one another. Separating them is the correct result.

---

**Lesson for future audits:** verify a property name against the actual document, follow the call
chain rather than one file, and confirm a framework type exists before asserting it is mishandled.
