# Normalization

Two members are duplicates when their **structural form** is identical. This document defines that
form, because it decides everything the tool reports.

## What changes

| Element | Treatment | Why |
|---|---|---|
| Local variables, parameters, type parameters, pattern designations, `foreach` and `catch` variables | Renamed `var0`, `var1`, … in order of first appearance | A copy-pasted method usually renames its locals |
| The member's own name | Renamed | A copy is normally given a different name; that is the point |
| Literals | Replaced by a kind placeholder: `STR`, `NUM`, `CHR`, `BOOL`, `NULL`, `LIT` | Copies usually differ in constants |
| Type names | **Preserved** | An `Order` mapper and a `Customer` mapper are not the same code |
| Member-access names (`x.Total`, `x?.Total`, `A.B`) | **Preserved** | The member being called is the meaning of the expression |
| Comments and whitespace | Removed | Formatting is not behaviour |

## Worked example

```csharp
int Total(Order order)
{
    var running = order.Price;
    return running;
}
```

becomes

```
int var0 ( Order var1 ) { var var2 = var1 . Price ; return var2 ; }
```

`Total` and `order` and `running` are declarations, so they are renamed. `Order` is a type and
`Price` is a member, so both survive.

## Consequences

**A genuine copy still matches.** Renaming the method and its locals changes nothing structural:

```csharp
int Sum(Order invoice) { var accumulator = invoice.Price; return accumulator; }
```

hashes identically to the example above.

**Unrelated code no longer collides.** These produce different hashes, because `WidgetResult` and
`GadgetResult` are type names:

```csharp
WidgetResult Process(WidgetInput input) { var r = new WidgetResult(); r.Name = input.Name; return r; }
GadgetResult Handle(GadgetInput input)  { var g = new GadgetResult(); g.Name = input.Name; return g; }
```

Erasing type and member names as well would fuse every mapper, handler, repository and factory in a
codebase into one enormous false positive.

**A local that shadows a member name is still safe.** In `void M(Order Price) { var x = Price.Price; }`
the parameter becomes `var1` while `.Price` is left alone.

## A known trade-off

Declared types are preserved exactly as written, including the `var` keyword. That means
`var x = new Foo()` and `Foo x = new Foo()` are not considered identical. Erasing declared types
would reintroduce the asymmetry where a local's type is ignored but a parameter's type is not.

## Near-duplicates

Members that do not hash identically go to the near-duplicate pass, which compares **multiset**
Jaccard similarity over the normalized token stream. Counting repeats matters: with set semantics, a
three-statement method and a twelve-statement method built from the same few identifiers score as
identical.

Candidate pairs come from an inverted token index under two pruning rules — a pair sharing no token
has similarity zero, and similarity can never exceed `min(size) / max(size)`. Both are exact, so the
result is identical to comparing every pair.

## Language version

Source is parsed with `LanguageVersion.Preview` so the newest syntax still parses. Files that fail
to parse are reported, never silently dropped: a single unrecognised construct must not discard
every valid member around it.
