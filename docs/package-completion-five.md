# Five work packages to complete SharpUnits

Planning baseline:2026-11-13. The .NET11 migration is the foundation, not a declaration of package completion. The ledger has113 assertion occurrences:14 GAP,99 PARTIAL mappings,0 VERIFIED. These five proposed work packages retain stable requirement IDs and explicit acceptance gates. No iteration quota substitutes for verified outcomes.

| Package | Primary TCK ownership | Main deliverable | Depends on |
| --- | ---: | --- | --- |
| 1. Core semantics and recoverable operations |60| Exact quantity/unit/converter/dimension behavior and usable typed failures | .NET11 foundation |
| 2. Formats and parsers |11| General graph/text and cursor behavior |1|
| 3. Catalogs, prefixes and factories |20| Exact creation, metadata, identity and registry isolation |1; coordinates with2|
| 4. Providers and structural interoperability |22| Qualified CLR/JVM mappings and deterministic discovery/selection |1,3|
| 5. Qualification and release | All113, cross-cutting; no duplicate owner | Reproducible release evidence and supported consumer package | Starts now; finishes after1–4|

The executable ownership map is [completion-work-packages.json](../conformance/completion-work-packages.json). Every ledger ID has exactly one primary owner in1–4 and a qualification obligation in5. Requirement records WP-COMPLETE-1 through5 remain Draft planning criteria; implementation and formal closure are separate.

## 1. Core semantics and recoverable operations — WP-COMPLETE-1

Own QuantityInterfaceTest16, UnitInterfaceTest27, UnitConversionTest10 and UnitDimensionTest7. Reconcile exact values, numeric promotion, unit equality/identity, scales, dimensional casts and noncommuting converter/transform composition. Preserve native electrical/symbolic and absolute-temperature safety. Keep measured Indriya quirks behind named compatibility paths; never make unsafe Java behavior the default engineering policy.

Extend typed expected failures to user-facing conversion/factory boundaries where useful, preserving legacy throwing APIs and state on failure. Compare C#15 unions/closed hierarchies with the current NumericResult representation before changing it: test empty/default state, allocation cost, cancellation, serialization and error forwarding. Prefer BCL/existing exact arithmetic to a new numeric framework. A bounded Dafny candidate is loop/cursor progress or checked scale arithmetic; select a precise requirement and execute verification/generated C# before claiming proof.

Exit: each owned occurrence has exact API and behavioral disposition, complete differential observations and independent mathematical/Java witnesses; all non-equivalent defects found are repaired. Historical rounding/product-loop timeouts and resource bounds have explicit executed or unresolved dispositions. New typed boundaries have malformed-input and short-circuit evidence. Local completion cannot bypass WP5's platform gates.

## 2. Formats and parsers — WP-COMPLETE-2

Own QuantityFormatTest5 and UnitFormatTest6. Complete supported graph text, transformed/affine expressions, powers/roots, labels/aliases, format families and parse cursor/consumption rules. Challenge locale, whitespace, malformed UTF-16, duplicate labels and round trips against actual pinned Java behavior; distinguish display text from unit/value/scale identity.

Before growing the grammar, compare maintained parser libraries/generated parsers, existing combinators and BCL span parsers/SearchValues/Unicode validation on representative valid and malformed inputs. Record exact versions, licenses, cursor semantics and recovery behavior at selection. Evaluate native System.Text.Json11 JSONL facilities for the observer harness without changing its schema or numeric fidelity. Use explicit typed parser failures; unexpected faults/cancellation propagate.

Exit: each supported graph round-trips structurally and numerically, malformed inputs have deterministic bounded diagnostics, format state is isolated and every remaining unsupported syntax is documented. Keep independently authored Java expectations and minimized randomized/fuzz counterexamples.

## 3. Catalogs, prefixes and factories — WP-COMPLETE-3

Own PrefixInterfaceTest6, SystemOfUnitsTest6, ObtainingQuantiesTest4, ObtainingUnitsTest2 and QuantityFactoryTest2. Audit every catalog entry, quantity binding, exact factory overload and prefix path, including boxed Long/BigInteger provenance, absolute/relative scale, affine restrictions, symbol identity and Unicode distinctions.

Use BCL frozen collections for fixed data and preserve intentionally mutable registries with atomic validation before publication. Validate duplicates/unknown metadata without replacing absent engineering values by zero. Reuse established registry/collection facilities instead of building another catalog engine. Provide actual package-consumer examples for exact inputs and recoverable factory failures.

Exit: full catalog/binding inventory has independent expected values and executable factory/import/export/identity evidence; rejected registrations leave valid prior state; all20 assertions have traceable dispositions.

## 4. Providers and structural interoperability — WP-COMPLETE-4

Own FundamentalTypesTest4, QuantityTypesTest2, ServiceProviderTest4, ServicesTest10 and TCKSetupTest2. Separate Java class/reflection/API-surface assertions from behavioral service contracts. Define supported .NET interfaces/types explicitly; a CLR analogue is not literal Java class identity. Where literal JVM interoperability is an actual acceptance requirement, evaluate an existing bridge and a bounded executable adapter before custom work. Record accepted deviations without reclassifying them as verified equivalence.

Evaluate Microsoft.Extensions.DependencyInjection and established registration/discovery mechanisms before extending custom service infrastructure. Keep explicit registration deterministic, provider selection isolated, diagnostics typed and automation free of interactive dialogs. Test empty/multiple providers, invalid types, failed activation, concurrency and restoration of process-global state.

Exit: all22 assertions have precise executable mappings or documented unresolved incompatibilities; provider failures preserve usable state and external discovery has isolated reproducible evidence. Unsupported reflection claims remain open rather than being counted as passes.

## 5. Qualification, documentation and release — WP-COMPLETE-5

Cover all113 occurrences and the requirements outside the TCK. Qualify .NET11/C#15-aware mutation tooling, coverage and malformed-input/property/fuzz campaigns. Retain seeds, minimized counterexamples, tested source/tool versions and raw outcome classifications. Historical reports remain immutable; ignored/uncompiled mutants and timeouts never become demonstrated kills. Plan memory budgets before dispatch and verify effective limits.

Run Windows, Ubuntu and pinned Java qualification when authorized, plus representative NuGet-consumer, examples, XML/API and engineering documentation checks. Compare new .NET11 Process/streaming APIs or maintained process libraries against the custom verification glue before expanding it. Scope Native AOT and performance claims to actual executed platforms/workloads. Publish only after explicit release authorization.

Exit: requirement-to-code-to-test/evidence links validate; each VERIFIED ledger entry satisfies its exact gates; packaging/examples are reproducible; independent review findings are resolved or explicitly accepted. CI is currently disabled by user instruction. Do not enable/dispatch it: formal platform-dependent closure and release readiness remain blocked until the user changes that policy. Local success is reported as local success.

## Delivery cadence

Choose one independently reviewable compatibility gap, write a failing requirement-based or reference-based witness, repair it, run appropriate bounded verification, update StrictDoc/ledger/evidence and commit. Use coherent vertical slices. Revisit reuse decisions when foundational gaps recur; passing tests or mutation counts do not justify a growing custom subsystem by themselves.
