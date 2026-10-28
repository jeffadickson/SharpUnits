# Codex engineering handoff — SharpUnits

Repository: https://github.com/jeffadickson/SharpUnits
Baseline before handoff: `dde0daa1acd250498f7f4376e2a1d48068e70573`
Target framework: .NET 10. Primary development language: C#.
Upstream Java TCK pin: `unitsofmeasurement/unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`.

## Current continuation (2026-10-27, work package 1)

The [core reconciliation review](../conformance/work-package-one-review.md) and [verification manifest](../conformance/work-package-one-verification.json) complete the local finite-profile implementation of work package 1. JavaNumber, JavaUnit, JavaNumberQuantity and JavaDimensionAdapter provide exact numeric/structural behavior, including converter order, decimal128 promotion, signed roots, prefix identity and exact factory bridges. Native engineering/temperature policies remain independent. The review documents pinned Java affine-temperature and Number quirks and the intentional CLR rational-hash consistency difference.

Final local results: 3,073 .NET tests; 1,734 matching core Java/.NET cases; 112 passing pinned Java TCK tests; 380 StrictDoc IDs in eight documents; 95.50% line / 86.68% branch managed-library coverage. The example was published as Native AOT and executed. Four FsCheck replays run 1,400 trials. Final focused Stryker: 18 killed and two equivalent survivors among 20 executable mutants. Initial broad survivors and the aborted VSTest rerun remain explicit qualification debt; do not quote an initial score as final-source evidence.

All 66 core TCK occurrences, including 16 quantity occurrences, are source-audited and remain PARTIAL CLR mappings. Ledger: 14 GAP, 99 PARTIAL_BEHAVIORAL_MAPPING, zero IMPLEMENTED_UNVERIFIED, zero VERIFIED. GitHub .NET and Java workflows remain disabled_manually; no CI run was dispatched. Package 2: general graph text/parsing/formats. Package 3: provider/type/reflection adaptation. Package 4: broad mutation requalification, independent review and Windows/Ubuntu qualification. Do not enable CI without a new user instruction.

Reproduce with tools/Invoke-CoreReference.ps1, tools/Invoke-CoreMutations.ps1, the full test/coverage command in the manifest, and the existing pinned Java/StrictDoc toolchains. Paths are relative to this checkout; reuse installed tools in ../tools-runtime and the user's .dotnet SDK before installing. Raw evidence stays outside Git with retained SHA-256 hashes. No formal assertion closure is authorized by this local implementation milestone.

## Current continuation (2026-10-27, Fahrenheit formatting)

See [Fahrenheit formatting review](../conformance/affine-format-review.md). The Java unit-format adapter now renders the recognized native Fahrenheit definition with its pinned converter expression, with explicit label precedence. Ten additional executed Java observations and guarded converter regression/property tests cover this bounded mapping. General transformed-unit graphs and affine-expression parsing remain gaps; native temperature conventions are preserved. GitHub CI stays disabled; no formal TCK assertion is closed.

## Current continuation (2026-10-25, unit-format state)

See [mutable unit-format and ASCII review](../conformance/unitformat-gap-hundred-review.md): 100 scoped items with compilation every ten; final 1,298 passing .NET tests, 872 matching Java/CLR observations and 112 passing pinned Java TCK tests. Thirteen semantic mutants are killed. Coverage is 94.25% lines / 83.72% branches; StrictDoc validates 374 records in seven documents. Ledger: 14 gaps, 50 implemented/unverified, 49 partial mappings, zero verified closures. Both GitHub workflows remain disabled. Remaining work includes general unit normalization/identity, null-unit label differences, full format families/cursors, Number precision and external discovery.

## Current continuation (2026-10-25)

The [service/catalog review](../conformance/service-gap-hundred-review.md) implements 100 scoped missing obligations: 44 unit definitions, 33 quantity bindings and 23 service contracts. Final verification: 1,140 .NET tests; 726 Java/CLR observations across separate corpora; 112 pinned Java TCK tests; eight new semantic mutants killed. StrictDoc validates 267 records in six documents. Ledger: 16 gaps, 50 implemented/unverified, 47 partial mappings, zero verified closures. GitHub CI stays disabled. Next gaps include full format families, mutable unit labels/appendable unit formatting, external discovery, Java API identity and Number precision.

## Current continuation (2026-10-24)

See [formatting review](../conformance/format-hundred-review.md): 100 numbered formatting scenarios, compile/full tests every ten; final 962 .NET tests, 558 shared cases and 512 seeded binary64 comparisons pass. Pinned Java TCK: 112 passing tests. Ledger: 42 gaps, 50 implemented/unverified, 21 partial mappings, zero verified closures. Both GitHub workflows remain disabled by current user instruction, superseding older CI directions below. Next gaps include arbitrary Number precision, unit-label normalization, formatting registration and provider/service APIs.

## Objective

Close the JSR 385 / Indriya compatibility gaps in a disciplined, auditable fashion. Do not divert into additional AC circuit, metrology, or symbolic DSL features. The existing optional features must not regress.

## Verified baseline

- Latest .NET CI and Java reference TCK workflows passed on the baseline commit.
- .NET 10 CI runs on Ubuntu and Windows. **Do not re-enable macOS** unless explicitly requested.
- `conformance/tck-closure-ledger.json` tracks 113 inventoried assertion occurrences: 47 GAP, 50 IMPLEMENTED_UNVERIFIED, 16 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED as of this handoff.
- `JavaQuantityAdapter` and 50 quantity-focused tests were integrated. The 16 `QuantityInterfaceTest` assertions are mapped to CLR analogues, not fully proven Java equivalents.

## Prioritized tasks

1. Fetch current `main`, check working-tree cleanliness and CI. Read this file, `conformance/java-parity-closure-plan.md`, `conformance/quantity-interface-50-reconciliation.md`, `conformance/tck-closure-ledger.json`, and the pinned upstream Java TCK source.
2. Audit each of the 16 `QuantityInterfaceTest` assertions against its exact Java method and expected public API signature. Distinguish structural reflection checks from semantic behavior checks. Add direct Java-versus-.NET differential fixtures where appropriate.
3. Investigate `JavaQuantityAdapter` semantics: `asType(Class)` versus kind/dimension checking; absolute versus relative `getScale`; `getUnit` and `getValue`; `equals` versus `isEquivalentTo`; `compareTo`; inverse; `to(Unit)`; arithmetic and Java `Number` precision. Document intentional .NET differences and needed adapter APIs.
4. Review correctness of absolute and relative temperature arithmetic and converter composition against pinned Indriya, especially offset units. Never mark mismatches as compliant without source evidence.
5. Add focused xUnit tests, source-level XML docs, and executable Java differential cases. Keep regression tests deterministic. Run `dotnet test` for the full suite and Java TCK/differential harness. Reconcile CI failures before completion.
6. Update the ledger **per assertion** with exact Java source method, .NET test name, expected behavior, CI evidence, and any remaining limitation. Mark VERIFIED only when evidence supports full equivalence; otherwise retain partial/unverified status.
7. After quantity closure, continue to the remaining formatting, factory, provider, and service suites in the TCK inventory.

## Engineering rules

- Preserve public APIs and backwards compatibility unless a reviewed breaking change is unavoidable.
- Prioritize exact numeric semantics and unit identity over superficial reflection-test count.
- Follow the existing requirements and traceability workflow; keep source documentation and tests current.
- Avoid introducing heavy dependencies for straightforward adapters.
- Do not claim the whole TCK is closed just because aggregate Java and .NET CI are green.
- Keep CI to Ubuntu and Windows; maintain the Java reference workflow.
- Commit coherent, tested increments with descriptive messages. Push only after checks pass or clearly document blockers.
- Summarize evidence: commit hashes, tests run, CI URLs, ledger status counts, and unresolved gaps.

## Suggested first deliverable

An assertion-by-assertion `QuantityInterfaceTest` parity matrix and a small, verified code change resolving at least one genuine semantic mismatch. Include a Java reference test, matching .NET test, and ledger update.
