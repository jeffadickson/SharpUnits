# Parser gap implementation review

Starting commit: `9d469b2`. This increment targets the three executable gaps
previously recorded as P-LIMIT-02 (compound units), P-LIMIT-03 (whitespace after
rational division), and P-LIMIT-04 (Celsius alias). Arbitrary Java Number storage
remains a separate architectural gap.

Fifty numbered Java/CLR scenarios will be reviewed, with compile/full-test
checkpoints every ten, plus coverage, seeded properties/fuzzing and mutation checks.
These are scenario reviews, not fifty commits or formal TCK closures. CI stays off.

Source review identified an important constraint: pinned SimpleUnitFormat parses
division chains recursively on the right. The existing CLR UnitExpressionParser
uses conventional left association. Reusing it unchanged would produce wrong
dimensions for expressions such as A/A/A. The Java parsing path must preserve its
own convention while retaining the old public parser's behavior.

## Three repaired incompatibilities

| Original probe | Input | CLR at 9d469b2 | Pinned Java | CLR now |
|---|---|---|---|---|
| P-LIMIT-02 | `1 A/V` | rejected | accepted | accepted |
| P-LIMIT-03 | `1÷ 2 A` | rejected | accepted | accepted |
| P-LIMIT-04 | `1 ℃` | rejected | accepted | accepted |

These original probes were re-executed through both runners. Their baseline and
current results remain in [the probe ledger](parser-known-differences.json).
They are three resolved behavioral gaps, not three formally closed TCK assertions.
`1e309 A` still differs: Java retains a finite BigDecimal; the CLR adapter rejects
it rather than losing information to infinity.

The Java parsing path now supports registered linear factors with `*`, `·`, `/`,
parentheses and bounded integer powers. Compound definitions retain their expression
text and computed dimensions/factor; they do not claim named alternate-unit identity.
Exact registrations win, then `℃` resolves locally to registered `°C`; the registry
is not modified. Rational denominator whitespace uses Java's character classification.

```csharp
var registry = UnitRegistry.CreateStandard();
var format = new QuantityTextFormat(registry);
var current = format.ParseJavaQuantity("1÷ 2 V/Ω").To(registry.Resolve("A")); // 0.5 A
var voltage = format.ParseJavaQuantity("5 mA*kΩ").To(registry.Resolve("V")); // 5 V
var temperature = format.ParseJavaQuantity("20 ℃"); // absolute Celsius, 293.15 K
```

Input limits remain 4096 characters and 1024 numeric characters across both rational
literals plus `÷`. Unit groups/operators are limited to depth 64; powers to -1024
through 1024. Nonfinite/zero compound factors and affine products are rejected.
The dedicated physical temperature APIs and legacy SI-only `Parse` are unchanged.

## Fifty executed reviews

| Reviews | Focus | Passing .NET tests | Shared cases | Mismatches |
|---|---|---:|---:|---:|
| 1–10 | Rational denominator whitespace and rejection boundaries | 697 | 318 | 0 |
| 11–20 | Celsius alias, magnitude, scale and unit | 707 | 328 | 0 |
| 21–30 | Compound units, prefix factors and conversions | 723 | 338 | 0 |
| 31–40 | Right-associated division and integer powers | 733 | 348 | 0 |
| 41–50 | Combined syntax and malformed expressions | 744 | 358 | 0 |

All fifty new observations match Java exactly with zero tolerance. Existing cases
retain their explicit tolerances. Admission probes compare acceptance/rejection,
not exception class identity. Compilation and full tests ran at every checkpoint.
The final restored-source build and coverage execution pass all 744 tests. Existing
library XML warnings remain; no build errors. Pinned Java TCK: 112 tests pass with
zero failures/errors/skips. No GitHub CI run was requested.

Commits: `14c9a73` (requirements), `fc083da` (1–20), `4c790f2` (21–30),
`7fb614e` (31–40), plus the final increment containing 41–50 and final evidence.
The [scenario record](parser-gap-review-50.json) retains all Java/CLR observations,
checkpoint counts and source fingerprints. Original-probe before results are
preserved separately; no per-scenario baseline output is fabricated.

## Coverage, mutations and generated tests

| Instrumented scope | Lines | Branches |
|---|---:|---:|
| Managed library | 1118/1215 (92.02%) | 914/1170 (78.12%) |
| JavaQuantityTextParser | 35/35 (100%) | 58/58 (100%) |
| JavaUnitTextParser, including its expression reader | 50/50 (100%) | 70/74 (94.59%) |
| QuantityTextFormat | 14/14 (100%) | 7/8 (87.5%) |

[Checkpoint 30](parser-gap-coverage-30.json) and [final coverage](parser-gap-coverage-50.json)
retain measured counters and report hashes. Four branches in the compound reader
remain unhit at source lines 45, 56 and 99 (suffix, operator and whitespace paths).
Java execution, automation, test code and generated regex/runtime internals are
unmeasured. Coverage alone does not establish correctness or Java equivalence.

Both new FsCheck properties run 300 cases with replay seeds `385,389` and `385,391`:
denominator-whitespace value preservation and compound prefix/dimension cancellation.
The two existing 300-case properties remain active. A new 1,000-case malformed
compound fuzz loop uses seed `385015`; the existing numeric fuzz seed is `385014`.
This is bounded grammar-focused fuzzing, not exhaustive arbitrary-input exploration.

All five compiling semantic mutations were killed: denominator whitespace (12
failed tests), Celsius alias (3), reversed division (17), lost prefix scale (4),
and weakened nesting limit (1). The [mutation evidence](parser-gap-mutations.json)
retains failures and FsCheck counterexamples. This targeted campaign is not an
exhaustive library mutation score. Original bytes were restored before the final
build, full test suite, coverage and differential execution.

## Requirements and remaining work

StrictDoc 0.30.4 validates/exports 157 stable requirement/assertion records.
JP-014-COMPOUND, JP-014-RATIONAL-SPACE and JP-014-ALIAS link implementation,
tests and evidence. `Active` is a lifecycle state; it does not mean verified.
The `46-A5` ledger mapping now includes these cases and original probe resolutions.
Formal totals remain **45 GAP, 50 IMPLEMENTED_UNVERIFIED,
18 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**.

Full Java grammar remains incomplete: numeric unit factors/shifts, roots,
superscript notation and automatic prefixes beyond registered symbols are not
implemented here. General unit identity, arbitrary Number precision/storage,
cursor/locale/appendable format APIs and Java service discovery remain outstanding.
Compound scale arithmetic uses double factors and Math.Pow; arbitrary exact unit
factor arithmetic is not promised. CLR limits can reject inputs Java can retain.

Restore/build the .NET projects and compile the Java reference runner with the
pinned classpath first. Run `tools/Invoke-ParserGapCheckpoint.ps1 50`, then
`tools/Invoke-ParserGapMutations.ps1`, rebuild restored source, and collect
`dotnet test --collect:"XPlat Code Coverage"`. Summarize with
`tools/Measure-ParserCoverage.ps1`; validate requirements with
`tools/Export-Requirements.ps1`. New automation uses C# and PowerShell.
The existing Python runner is used only for its pinned Java TCK workflow, and
StrictDoc is a required Python-based development tool.

Both remote workflows were confirmed `disabled_manually`; source retains manual
dispatch only and macOS stays disabled. Local Windows evidence is not new
Ubuntu/Windows CI or reviewer closure.
