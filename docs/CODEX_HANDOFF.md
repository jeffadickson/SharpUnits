# Codex engineering handoff — SharpUnits

Repository: https://github.com/jeffadickson/SharpUnits
Baseline before handoff: `dde0daa1acd250498f7f4376e2a1d48068e70573`
Target framework: .NET 10. Primary development language: C#.
Upstream Java TCK pin: `unitsofmeasurement/unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`.

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
