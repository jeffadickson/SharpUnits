# Quantity parsing review

Follow-up: the [parser gap review](parser-gap-fifty-review.md) implements three of
the four incompatibilities recorded below. This document preserves the earlier evidence.

Starting commit: `098acf2`. Review date: 2026-10-24. GitHub CI stays disabled.

Scope: add an adapter-returning parser preserving the display unit and absolute
scale, while retaining the existing SI-only parser contract. Compare decimal,
whitespace, rational and malformed inputs with the pinned Indriya
`SimpleQuantityFormat.parse` and `RationalNumberScanner`. Full Java unit grammar,
parse cursors, localization and arbitrary Number retention remain outside this increment.

There will be 50 numbered executable scenario reviews, with compilation and full
regression testing after every ten. These are not 50 commits or formal closures.
Coverage, deliberate mutations, negative cases and seeded randomized tests will
supplement the Java differential evidence. Requirements are maintained in StrictDoc
with stable IDs and implementation/test/evidence links. No verification result is
claimed before execution.

## Implementation and execution

`QuantityTextFormat.ParseJavaQuantity` returns an absolute adapter retaining its
display value and unit. It accepts invariant decimal/scientific literals, or two
contiguous signed integers joined by `÷`. Missing units mean dimensionless.
Rationals round once from the exact fraction to double. Text is limited to 4096
characters and numeric tokens to 1024. The old SI-only `Parse` API is unchanged.

```csharp
var format = new QuantityTextFormat(UnitRegistry.CreateStandard());
var current = format.ParseJavaQuantity("250\tmA"); // Value = 250; Unit = mA
var third = format.ParseJavaQuantity("1÷3 A");
var temperature = format.ParseJavaQuantity("20 °C"); // absolute; display value 20
```

| Reviews | Focus | .NET tests | Shared cases | Mismatches |
|---|---|---:|---:|---:|
| 1–10 | Decimal syntax and display preservation | 647 | 268 | 0 |
| 11–20 | Java whitespace and nonbreaking separators | 657 | 278 | 0 |
| 21–30 | Signed fractions and invalid rationals | 667 | 288 | 0 |
| 31–40 | Unit equality, scale and underflow | 677 | 298 | 0 |
| 41–50 | Malformed numeric/unit admission | 687 | 308 | 0 |

Baseline: 625 tests. All 50 new observations match Java with zero tolerance;
existing cases retain their declared tolerances. Admission probes compare rejection,
not exception class identity. Pinned Java TCK: 112 tests pass, including five
QuantityFormatTest methods. Final restored-source build and coverage execution pass
all 687 tests, with 19 pre-existing XML documentation warnings and zero errors.

Commits: `5bc70f2` (requirements), `beff4f1` (1–20), `2a0d7e6` (21–30),
`394c2c9` (31–40); the final increment contains 41–50 and final evidence.

The Java runner explicitly uses UTF-8 on Windows. Unit checks compare unit equality,
not canonical labels: Java formats Celsius as `℃`, while the registry uses `°C`.
That formatting difference remains open.

## Coverage and adversarial checks

| Instrumented scope | Lines | Branches |
|---|---:|---:|
| Library overall | 1063/1160 (91.64%) | 838/1090 (76.88%) |
| New JavaQuantityTextParser | 30/30 (100%) | 52/52 (100%) |
| QuantityTextFormat | 14/14 (100%) | 7/8 (87.5%) |
| Shared JavaDoubleArithmetic | 46/46 (100%) | 50/54 (92.59%) |

Coverlet 6.0.4 counters/report hashes are retained in the [initial](parser-coverage-10.json)
and [final](parser-coverage-50.json) records. Generated regex/runtime internals,
Java, tests and automation are unmeasured. Coverage does not establish correctness.

Two FsCheck properties run 300 cases each with replay seeds `385,385` and `385,387`:
integer-rational rounding versus independent binary division, and display/unit/scale
preservation. Another 1,000 malformed numeric-token probes use seed `385014`.
This fuzzing is grammar-focused, not exhaustive arbitrary-input fuzzing.

All five compiling manual mutations were killed: NBSP classification, fraction
arithmetic, relative scale, wrong unit and doubled input limit. This targeted
campaign is not an exhaustive mutation score for the library. The
[mutation record](parser-mutations.json) retains test failures and FsCheck
counterexamples, including the wrong-unit property shrinking from 1 to 0.
Source was restored byte-for-byte and rebuilt before final verification.

## Traceability and remaining gaps

StrictDoc 0.30.4 validates/exports three documents with 154 stable IDs: 37 capability
requirements, four detailed parsing requirements and 113 TCK assertion records.
`Active` denotes requirement lifecycle, not verification. Implementation, test and
evidence links are retained in SDoc comments, source relation markers, the ledger
and [scenario record](parser-review-50.json).

`46-A5` advances to PARTIAL_BEHAVIORAL_MAPPING. Its Java assertion checks public
method availability; the shared cases supplement it with behavior. Totals:
**45 GAP, 50 IMPLEMENTED_UNVERIFIED, 18 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**.
No formal closures or new remote CI evidence are claimed.

Four separately executed [incompatibilities](parser-known-differences.json) remain:
compound units (`A/V`), whitespace within fractions (`1÷ 2`), automatic Celsius
alias resolution (`℃`), and arbitrary Java numbers (`1e309`). Java accepts these;
this subset rejects them. A fifth guard confirms both reject `1 A trailing`,
disproving a permissive-suffix assumption. These cases are not counted as matching
conformance cases. Cursor APIs, locales, Number identity and precision in subsequent
arithmetic remain open.

## Reproduction

Use .NET 10 and the pinned Java classpath. Compile the Java reference runner,
then run `tools/Invoke-ParserCheckpoint.ps1 50` from the repository root.
`tools/ParserReview.cs` supplies `prepare`, `record` and nonmutating `check` modes.
Run `tools/Invoke-ParserMutations.ps1`, rebuild restored source, collect
`dotnet test --collect:"XPlat Code Coverage"`, and summarize with
`tools/Measure-ParserCoverage.ps1`. New automation uses C# and PowerShell.
StrictDoc is a required Python-based development tool, pinned in
`requirements/tool-requirements.txt`; export with `tools/Export-Requirements.ps1`.

Both GitHub workflows remain `disabled_manually`; macOS stays disabled. Local
Windows tests do not supply new Ubuntu/Windows GitHub CI proof.
