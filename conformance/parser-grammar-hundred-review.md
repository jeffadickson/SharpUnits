# Remaining Java grammar implementation review

Baseline: `e982406`. GitHub CI remains disabled by user request.

This batch targets four concrete parser gaps: superscript/double-star powers,
rational powers/roots, integer numeric unit factors, and automatic decimal prefixes.
It comprises 100 numbered executable scenario reviews, with compilation and full
regression tests every ten, coherent commits, coverage, seeded generated/fuzz tests
and targeted mutations. Review counts are not commit counts or formal TCK closures.

Pinned SimpleUnitFormat source must guide implementation. It treats zero exponent
and root literals as default-one sentinels, and handles immediate integer factors
differently from recursively parsed unit operands. Those conventions must be
tested, rather than inferred from conventional algebra. Native unit APIs and the
legacy CLR parser retain their existing contracts.

Arbitrary Java Number storage, full locale/cursor/appendable formatting and service
discovery remain separate outstanding requirements. No new behavior is claimed
before actual Java and .NET execution.

## Implemented and observed behavior

Four baseline probes that Java accepted and CLR rejected now agree in both runners:
`1 A²`, `1 A^1:2`, `1 A*2`, and `1 µA`. The
[baseline record](parser-grammar-baseline.json) retains their before/after results.
Superscript digits 1/2/3, `**` powers, colon roots and all 24 decimal SI prefixes
are supported within explicit bounds. Greek `μ` normalizes to micro `µ`; exact
registrations still override inference and parsing does not mutate registries.

Pinned Java treats zero **or missing** exponent/root digits as default-one
sentinels. `A^0` and `A^` therefore retain the dimension of A. `A^-0` means A^-1.
These are parser compatibility conventions; native Pow(0) and the legacy parser
retain conventional algebra. A separately observed rejection of bare `A^` was
corrected in this increment, with its before/after result retained among the
[limit probes](parser-grammar-known-limits.json).

Immediate integer unit factors use Java's operator loop before unit-operand
recursion. For example, `A/2/Ω` means `(A/2)/Ω`, whereas `A/A/A` remains `A/(A/A)`.
Nonterminating factors retain exact internal linear coefficients, so `3 A/3`
converts to exactly `1 A`. Conversion, addition and derived-unit arithmetic use
that coefficient. Public magnitudes and legacy converter APIs remain doubles.
Unit equality distinguishes exact factors from different rounded coefficients.
Exact metadata is not a general Java Number model or a new interchange format.

```csharp
var registry = UnitRegistry.CreateStandard();
var format = new QuantityTextFormat(registry);
var current = format.ParseJavaQuantity("3 A/3").To(registry.Resolve("A")); // 1 A
var rooted = format.ParseJavaQuantity("7 (mA²)^1:2").To(registry.Resolve("A")); // 0.007 A
var small = format.ParseJavaQuantity("4 nA").To(registry.Resolve("A")); // 4e-9 A
```

## Ten compile/full-test checkpoints

| Review | Passing .NET tests | Shared cases | Mismatches |
|---|---:|---:|---:|
| 10 | 754 | 368 | 0 |
| 20 | 764 | 378 | 0 |
| 30 | 778 | 388 | 0 |
| 40 | 788 | 398 | 0 |
| 50 | 801 | 408 | 0 |
| 60 | 811 | 418 | 0 |
| 70 | 824 | 428 | 0 |
| 80 | 834 | 438 | 0 |
| 90 | 844 | 448 | 0 |
| 100 | 854 | 458 | 0 |

Baseline: 744 tests. Every new observation matches actual pinned Java with zero
tolerance; pre-existing cases retain their documented tolerances. Admission probes
compare acceptance rather than Java/CLR exception identity. The final restored-source
build, coverage run and differential execution pass all 854 tests and 458 cases.
Pinned Java TCK: 112 tests, zero failures/errors/skips. Existing documentation and
unused-theory-ID warnings remain; no build errors.

Commits: `fbbcdbf` (scope/baselines), `48ed091` (1–20), `aa2eb5f` (21–40),
`590da37` (41–60), `135c65b` (61–90), plus the final 91–100/evidence increment.
[The scenario record](parser-grammar-review-100.json) retains observations,
checkpoint counts, requirements and source fingerprints. Per-case baseline
values were not invented for scenarios outside the separately executed probes.

## Coverage and adversarial evidence

| Instrumented scope | Lines | Branches |
|---|---:|---:|
| Managed library | 1187/1285 (92.37%) | 1016/1274 (79.75%) |
| JavaQuantityTextParser | 35/35 (100%) | 58/58 (100%) |
| JavaUnitTextParser including helper types | 104/105 (99.05%) | 159/166 (95.78%) |
| JavaQuantityAdapter | 82/84 (97.62%) | 105/112 (93.75%) |
| JavaDoubleArithmetic | 49/49 (100%) | 54/58 (93.10%) |
| UnitDefinition | 20/20 (100%) | 5/6 (83.33%) |

Coverage records at [40](parser-grammar-coverage-40.json),
[60](parser-grammar-coverage-60.json) and [100](parser-grammar-coverage-100.json)
retain counters, report hashes and uncovered line/branch locations. Remaining
unhit paths include exact-factor resource/error branches and some adapter guards.
Java, tests, tooling and generated/runtime internals are unmeasured. Coverage is
not proof of equivalence or correctness.

Four new FsCheck properties execute 300 cases each with replay seeds `385,393`,
`385,395`, `385,397`, and `385,399`: superscript dimensions, matching powers/roots,
integer-divisor rounding and prefix conversion versus independent decimal parsing.
Two new 1,000-case fuzz loops use seeds `385016` and `385017`. Earlier properties
and numeric/compound fuzzing remain active. Fuzzing is bounded and grammar-focused.
The independent Fraction oracle also passes 200 conversion/addition cases with
seed 385; it verifies the stated decimal-input rounding model, not arbitrary Java Number identity.

All eight compiling manual mutations were killed: superscript digit (21 failing
tests), zero sentinel (8), negative root (1), integer continuation (6), exact-scale
metadata (3), prefix sign (36), registration precedence (1), and precision budget (1).
[Mutation evidence](parser-grammar-mutations.json) retains failures and minimized
FsCheck counterexamples. This is a targeted campaign, not an exhaustive mutation
score. Original source bytes were restored before final build/testing/coverage.

## Requirements, limits and reproduction

StrictDoc 0.30.4 validates/exports 161 stable records in four documents. New
JP-014-POWERS, JP-014-ROOTS, JP-014-INTEGER-FACTOR and JP-014-PREFIXES requirements
link implementation, tests and execution evidence. Requirement lifecycle `Active`
does not mean verified. The TCK ledger remains **45 GAP, 50 IMPLEMENTED_UNVERIFIED,
18 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**. No formal assertion closure is claimed.

Six separately executed differences remain: integer factors outside signed 32-bit
bounds, fractional numeric factors, affine prefixes, root orders above 1024,
arbitrary Number magnitudes such as `1e309`, and exact-factor growth above 16384 bits.
Java accepts the probes while this CLR profile rejects them. Java's integer scanner
was independently observed to wrap `2147483648` to a unit factor of `-2147483648`;
the CLR parser intentionally rejects that range. Other unresolved areas include
numeric shifts, complete unit identity/normalization, repeated/permissive grammar
forms, general Number precision/storage, legacy double converter projection,
serialization of exact metadata, locale/cursor/appendable APIs and service discovery.

After restoring/building .NET projects and compiling the pinned Java runner, use
`tools/Invoke-ParserGrammarCheckpoint.ps1 100`. Scenario preparation/checking is a
C# file-based app (`tools/ParserGrammarReview.cs`). Run
`tools/Invoke-ParserGrammarMutations.ps1`, rebuild restored source, collect
`dotnet test --collect:"XPlat Code Coverage"`, then summarize with
`tools/Measure-ParserCoverage.ps1`. Export requirements using
`tools/Export-Requirements.ps1`; StrictDoc is pinned in its development requirements.
New automation is C#/PowerShell. Existing Python tools are used for the required
pinned TCK and independent Fraction workflows only.

Both GitHub workflows remain `disabled_manually`, source retains manual dispatch,
and macOS stays disabled. Local Windows results are not new Ubuntu/Windows CI evidence.
