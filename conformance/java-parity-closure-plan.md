# JSR 385 / Indriya parity — closure-first milestone

**Scope freeze:** Until core conformance closure advances, do not expand AC circuits, metrology, semantic DSLs or other optional APIs except to repair regressions.

Pinned upstream TCK: `unitsofmeasurement/unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`.

## Evidence accounting

The inventory contains **113 annotation occurrences** in 16 Java classes. Two setup annotations both have upstream ID `Setup` and are internally distinguished as `Setup-1` and `Setup-2`. These are not 113 independently verified .NET assertions.

The closure ledger currently records 42 GAP, 21 PARTIAL_BEHAVIORAL_MAPPING, and 50 IMPLEMENTED_UNVERIFIED; **0 VERIFIED**.

The pinned Java TCK run has succeeded (112 Surefire tests); the separate cross-language fixture runner has succeeded. Neither establishes full SharpUnits conformance: normalized differential fixtures are not a one-to-one substitute for the official assertion corpus.

## Required next actions, in order

The handoff supersedes the historical order below: QuantityInterfaceTest is first. See [the assertion review](quantity-interface-review.md).

1. **UnitDimensionTest (7 annotations):** Review each pinned Java method and its fixture; record exact behavior, source method and corresponding xUnit test. Execute Java reference and C# equivalent, attach CI run and mark VERIFIED only when all expected behavior matches.
2. **UnitConversionTest (10):** Repeat with affine conversion, inverse, identity, linearity, concatenation and conversion steps. Do not close `convert(Number)` using only a `double` test.
3. **PrefixInterfaceTest (6):** Confirm prefix definitions, decimal factors, symbols and supported API behavior; compare Java and C# reference outputs.
4. **UnitInterfaceTest (27):** Prioritize products, quotients, roots, powers, compatibility, identity and offset units. Record any intentional .NET API deviations separately.
5. **QuantityInterfaceTest (16):** Verify arithmetic, conversion, equality and numeric semantics.
6. **Provider, service, formatting and factory suites (remaining):** Classify each as equivalent, adapter-needed, not applicable (with justification), or gap.

## Verification gate

A ledger entry can be `VERIFIED` only when it has: exact upstream assertion ID and source method, complete expected semantics, a runnable C# test, a passing .NET CI URL, a Java reference execution record, and reviewer/date. Do not infer closure from green aggregate CI.

## Exit criteria

- Every inventory occurrence has a disposition and an auditable rationale.
- No unreviewed gaps are represented as complete.
- Core supported equivalents pass .NET 10 on Ubuntu/Windows and match pinned Java reference cases.
- Remaining intentional incompatibilities are documented as explicit adapter requirements.

See `conformance/tck-closure-ledger.json` and `conformance/verification-protocol.md`.

Latest formatting evidence: [100-scenario formatting review](format-hundred-review.md). GitHub workflows remain disabled at the user’s request; local execution is not current CI evidence.
