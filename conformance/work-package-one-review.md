# Work package 1: core Java reconciliation

**2026-10-29 repair continuation:** [Verification repairs](verification-repair.md) supersede the old observation and default narrowing behavior described below. Historical execution numbers retain their original scope.

**2026-10-29 audit qualification:** [verification audit](verification-audit.md) reopens verification acceptance. Historical executions below remain evidence of their stated observations; they do not establish complete quantity results, raw Java representation preservation or requirement closure. The separate reviewer concurs only with the audit findings and limits; see its retained decision. No production behavior is changed by that audit.

The new core separates structural units and finite Java numeric representations from the existing double/SI projection. This permits exact-number quantities, normalized unit products and lazy affine inverse units without changing native electrical/symbolic or temperature safety policies.

## Assertion scope

The source audit covers all 66 annotation occurrences in these pinned suites:

| Suite | Occurrences | Evidence |
|---|---:|---|
| UnitDimensionTest | 7 | Signed roots, equality/hash, nullable base decomposition |
| UnitConversionTest | 10 | Exact numeric conversion, ordered composition, reduction, steps, inverse |
| PrefixInterfaceTest | 6 | Java names, radix versus factor, all 24 metric and eight binary prefixes |
| UnitInterfaceTest | 27 | Metadata, atom identity, alternates, transforms, products, conversion |
| QuantityInterfaceTest | 16 | Exact values, scale, arithmetic, conversion, equality/comparison, inverse |

`work-package-one-assertion-audit.json` retains the actual Java source methods, method-presence/reflection calls, source hashes and CLR surface tests. Most occurrences check API structure; they are not numerical acceptance tests. The separate differential corpus challenges numerical and unit behavior beyond those structural checks.

All 66 retain partial behavioral status because CLR signatures and runtime identities are adaptations. The ledger now has 14 GAP, 99 PARTIAL_BEHAVIORAL_MAPPING, zero IMPLEMENTED_UNVERIFIED and zero VERIFIED. No GitHub CI or independent-review gate has been waived.

## Public implementation

- `JavaNumber` retains Integer, Rational, Decimal and Binary64 values using BigInteger/Rational storage. Decimal/decimal and decimal/binary operations use the pinned decimal128 context where required; rational/decimal promotion and decimal scale affect results and representation equality.
- `JavaUnitConverterAdapter` implements Java's argument-first `Concatenate`, exact scale/offset operations, normalization, inverse/equality, immutable steps and finite logarithm/exponential conversion. `Then` preserves the previous native operation order.
- `JavaUnit` represents base, alternate, transformed and normalized product units. Alternate identity is distinct from dimensional or converter equivalence. All 44 built-in catalog parent graphs are modeled. Named metadata does not replace structural parent identity.
- `JavaUnitAdapter` uses this graph. Additive `MultiplyUnit`, `DivideUnit`, `PowerUnit`, `RootUnit`, `InverseUnit`, `CoherentUnit` and `GetBaseUnits` preserve structure. Existing expression-returning APIs remain native projections.
- `JavaNumberQuantity` and `IExactJavaQuantityFactory` preserve numeric representations, unit graphs and absolute/relative scale. Typed factories also accept exact values and units. Legacy adapters expose `ToExact` and `InverseExact`; precision already lost before promotion cannot be recovered.
- `JavaDimensionAdapter` adds signed roots and Java's nullable fundamental-dimension map without changing native positive-root behavior.
- `SiPrefix.GetValue()` returns the Java radix, ten; `ExactValue` is the powered factor. `GetName()` returns Java's enum spelling. Native `Value` and `Name` properties keep their established contracts. Native prefixing now retains exact unit factors and its affine safety guard.

See the runnable `examples/ExactJavaCore.cs` and XML source documentation. Migration: Java adapter `Concatenate` and `Transform` now follow Java order; use `Then` for native left-to-right converter composition. Unit `Name`/`Symbol` are nullable intrinsic metadata rather than generated symbol fallbacks. Native expression-returning unit operations and double/SI quantity APIs remain available; select the new graph/exact operations explicitly.

## Behavioral corrections and compatibility decisions

Four historical tests were asserting incorrect Java semantics: concatenation order, affine step count, rejection of affine prefixes, and accepting an alternate of a scaled unit. Their expectations were corrected only after independent pinned-Java observations.

A pinned Java arithmetic quirk is deliberately confined to the explicit AddPinnedIndriya/SubtractPinnedIndriya APIs: Decimal/BigInteger addition uses signed 64-bit narrowing. Default Add/Subtract preserve the integer magnitude. For example, adding `0.1` to `2^64` yields `0.1` in that Java path. `AddExact` performs mathematical rational addition instead. Native arithmetic is not routed through this quirk.

Pinned RationalNumber equality compares decimal128 projections, while exact comparison can distinguish the underlying fractions. The core matches that equality behavior. CLR hashes remain consistent with CLR equality; Java's conflicting exact-rational hash implementation is intentionally not reproduced. Numeric hash values are not Java interoperability identifiers.

Unit products can exist even when pinned Java cannot supply their system converter. Affine inverse products and fractional roots demonstrate this distinction. Construction preserves identity/dimensions; requesting an unsupported converter raises an exception. No identity scale or fabricated coherent value is substituted.

Absolute `20 °C + 10 °C` yields `303.15 °C` in the pinned Indriya quantity path. The returned display unit is Celsius, so this must not be relabeled as kelvin. It is a compatibility quirk caused by absolute affine arithmetic, not an engineering rule for adding temperatures. The exact Java core reproduces the measured result; native absolute/delta-temperature APIs retain their physical safety conventions. Use a relative temperature difference for physical temperature increments.

`shift(double)` and `shift(Number)` have different promotion paths in Java. The corpus explicitly distinguishes those overloads and the imported standard Fahrenheit definition. Nonlinear comparisons declare tolerances; they do not claim bitwise agreement between JVM and CLR transcendental implementations.

## Verification

The final verification manifest records exact source/log hashes, tool versions, commands, test counts, per-class line/branch coverage, seeds and mutation outcomes. The source review findings and their dispositions are retained separately. A source audit, an executed Java reference and a local self-review are not an independent human review.

Core corpus generation uses seed `385701`. FsCheck runs product cancellation/order, exact affine inverse, malformed decimal suffix and rational arithmetic campaigns with replays `385,701`, `385,703`, `385,705` and `385,707`. No minimized counterexample is discarded; mutation failures are retained in the mutation review.

Reproduction from the existing checkout:

```powershell
$repo=(Get-Location).Path
if(!(Test-Path -LiteralPath $repo -PathType Container)){throw 'Select the SharpUnits checkout'}
$dotnet=Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
& $dotnet test tests/SharpUnits.Tests -c Release --collect:'XPlat Code Coverage'
./tools/Invoke-CoreReference.ps1
& $dotnet run --file tools/CoreReview.cs -- audit
```

Prefix converter equality retains radix/exponent identity: `10^3` differs structurally from rational `1000`, even though both convert identically; Java has an exponent-one shortcut. Primitive unit double overloads also bypass the Number long-narrowing path at ±2^63. These distinctions are executable differential cases.

Java TCK reproduction uses the existing pinned JDK/Maven/toolchains and `tools/run_pinned_tck.py` documented in the handoff. Stryker.NET 5.0.0 uses `tools/stryker-core.json`; raw reports remain outside the checkout. The reflection-based corpus/audit file app explicitly disables Native AOT; no helper native compilation is implied by running it.

## Compatibility envelope and later packages

The profile is fixed to the pinned default Java number system and decimal128 context. Numeric representations have a 16,384-bit budget; decimal tokens/scales and unit graph depth/factor/exponent bounds are explicit in XML documentation. Nonfinite construction and unsupported nonlinear domains are rejected. CLR integer widths, Java Class/generic identities and concrete runtime Number classes are not interchangeable.

The legacy `JavaQuantityAdapter` remains a finite double/SI projection, including its native affine-inverse restriction. The exact quantity core supplies the structural inverse without forcing that projection. Existing native dimension maps, expression roots and physical temperature rules keep their contracts.

Package 2 owns general graph text, parsing, cursors and format families. Package 3 owns external provider discovery and Java type/reflection adaptation. Package 4 owns independent review and Windows/Ubuntu qualification. GitHub workflows stay disabled; no remote CI pass or formal TCK closure is claimed.

## Final local results

- .NET 10.0.402: 3,073 passing xUnit tests, zero failures/skips, including 1,734 pinned core fixtures and 27 adversarial/property tests. FsCheck executes 1,400 generated trials across the four stated replays; these are trials within tests, not 1,400 additional xUnit tests.
- Differential corpus: 793 numeric, 215 converter, 416 unit, 205 quantity, 73 dimension, 24 metric-prefix and eight binary-prefix cases. Zero mismatches. One separately retained rational-hash-law difference is intentional and actually observed in both runtimes.
- Pinned Java TCK: 112 passing Surefire tests. The comparator's own 12 negative/tolerance tests pass. StrictDoc validates 380 requirement IDs in eight documents.
- Coverlet managed-library scope: 2,081/2,179 lines (95.50%) and 1,985/2,290 branches (86.68%). Per-class gaps are retained in work-package-one-coverage.json. Java, helper tooling, native code and runner code have no coverage claim.
- Initial broad mutation run: 646 killed, 292 survived, 27 uncovered, five timed out, 68 compile errors and 91 ignored in eight scoped files. This is historical source, not a final-source score. It drove isolated catalog construction and additional semantic/budget tests. A broad rerun aborted after VSTest infrastructure failures; no completed result is inferred.
- Final focused Stryker 5.0.0 run: 18 killed, two surviving but reviewed as equivalent, zero timeouts/uncovered among 20 executable mutants. Reported tool score is 90%, not a claimed universal 100%. Two meaningful boundary survivors from the prior focused run were killed after adding endpoint witnesses. Broad survivor disposition/requalification remains package-4 testing debt; it is not evidence of correctness.

Use tools/Invoke-CoreMutations.ps1 to regenerate character spans for the focused campaign. tools/stryker-core.json describes the broader campaign; single-worker execution is preferred after the recorded multi-worker infrastructure failure. Source review was performed by the implementing agent, not an external reviewer. All raw logs/reports remain outside the checkout; the verification manifest retains hashes and reproduction commands.
