# JSR compatibility closure audit — 2026-11-16

Verdict: substantial bounded implementation, incomplete semantic qualification; no full JSR compliance/certification claim. All113 inventoried pinned TCK occurrences were source-audited against C# implementations and actual test/evidence references. This is an assertion audit, not a complete review of every normative specification/API clause. Three separate read-only agents reviewed independent subsets; their judgments are agent review, not external certification.

## What the counts actually mean

| Exact test category | Occurrences |
| --- | ---: |
| Public API shape/reflection |81|
| Configuration/registration |9|
| Behavioral assertions |21|
| Mixed behavior/discovery |2|

The16 QuantityInterfaceTest occurrences and all27 UnitInterfaceTest occurrences check public API shape rather than executing their named arithmetic operations. The helper overload with hasParameters=true checks only a public method name, not parameter count/types. The113 count measures annotation occurrences, not113 independent behavioral requirements or a compliance percentage.

The source audit assesses58 occurrences as bounded domain mappings and55 as API adaptations. These are implementation dispositions, not runtime passes or VERIFIED closures. Recommendations are56 preserve-semantic contracts,50 native .NET adaptations and7 defer-Java-mechanism cases. Separate residual behavior fields keep genuine domain gaps visible even when a reflection check has a native analogue.

The formal ledger remains14 GAP,99 PARTIAL,0 VERIFIED. Every occurrence now links to its audit record; stable IDs and ownership are unchanged. Most of the14 GAP records do not mean missing arithmetic: six concern Java configuration/class registration; two concern all-base/all-supported-type coverage; six concern obtaining quantities/units with exact configuration/type/catalog evidence still absent. Existing C# functionality is not automatically qualified by this reassessment.

## Exact suite inventory

| Suite | Total | API shape | Configuration | Behavior | Mixed |
| --- | ---: | ---: | ---: | ---: | ---: |
| FundamentalTypesTest |4|0|4|0|0|
| ObtainingQuantiesTest |4|2|0|1|1|
| ObtainingUnitsTest |2|0|0|2|0|
| PrefixInterfaceTest |6|6|0|0|0|
| QuantityFactoryTest |2|0|0|1|1|
| QuantityFormatTest |5|4|1|0|0|
| QuantityInterfaceTest |16|16|0|0|0|
| QuantityTypesTest |2|0|0|2|0|
| ServiceProviderTest |4|0|0|4|0|
| ServicesTest |10|0|0|10|0|
| SystemOfUnitsTest |6|5|0|1|0|
| TCKSetupTest |2|0|2|0|0|
| UnitConversionTest |10|9|1|0|0|
| UnitDimensionTest |7|7|0|0|0|
| UnitFormatTest |6|5|1|0|0|
| UnitInterfaceTest |27|27|0|0|0|

Records: conformance/release-compliance-audit.json, with exact Java source methods/lines, implementation and test anchors, residuals and modern-domain decisions. Separate subset files retain reviewer outputs. Their qualificationGap strings describe source-review execution scope at review time; the fresh aggregate runs below do not establish complete per-assertion qualification. Validate accounting/reference existence with tools/Validate-ReleaseAudit.ps1; its PASS does not validate numerical semantics.

## Important corrections to the old picture

- The exact JavaNumber/JavaUnit/JavaNumberQuantity path already handles representation, graph and affine-inverse cases that the older double adapters cannot. Do not report legacy projection limits as absent core functionality.
- QuantityFormat A2/A3 duplicate the same Appendable signature check. Unit/quantity parse and label checks using the name-only helper do not exercise parsing behavior.
- FundamentalTypes41-A3 counts registered Prefix classes, not24 metric or8 binary prefix values. Copying Java test registries into the domain would not improve numerical correctness.
- Services54-A03 calls getUnitFormat(), despite its quantity-format description. The audit follows executed code rather than that description.
- Stock Java TCK execution exercises pinned Indriya. It is reference/toolchain evidence, never a claim that the TCK directly exercised the C# implementation.
- The older Markdown capability inventory contains stale pre-exact-core descriptions. Stable StrictDoc IDs are retained; this assertion audit supersedes old blanket absence statements, not the underlying normative requirements.

## Fresh execution and retained limitations

Fresh Windows runs:3240 .NET tests passed,0 failed/skipped;2670 paired observations across11 corpora matched, with independent Java expectation checks;112 pinned Java TCK tests passed,0 failures/errors/skips. Coverlet library scope:2147/2243 lines95.72%,2032/2311 branches87.93%. Demo/tools/Java and PowerShell guard/validator branches are outside those metrics. FsCheck cases in the full suite execute their retained replay settings, including the new prefix seed385,1113; no new coverage-guided fuzz campaign or Stryker campaign ran in this audit. The known C#15 Stryker compiler limitation remains unresolved, not counted as a kill or success.

Six additional public formatter/parser diagnostic observations were independently captured and replayed:

| ID | Java observation | C# observation | Disposition |
| --- | --- | --- | --- |
| RA-F-01 | ((K*5)/9)+459.67 | same text | Formatting control |
| RA-F-02 | Parses that expression equal to the constructed Fahrenheit unit | INVALID | Confirmed affine-expression parse gap |
| RA-F-03 | m*0.5 parses/formats as m/2 | INVALID | Confirmed decimal-factor parse gap |
| RA-F-04 | m*2147483648 formats as m*-2147483648 | INVALID | Pinned Java text anomaly and .NET range boundary; do not copy apparent narrowing into native defaults |
| RA-F-05 | m*2 | m*2 | Integer-factor control |
| RA-F-06 | m | m | Base-unit control |

Three matching controls and three disagreements are retained as observations, not six conformance passes. The large-factor text alone does not establish the complete converter/value semantics; investigate separately before choosing adapter behavior. Initial diagnostic attempts used the wrong harness catalog identifier and factor ordering; they are retained as failed/unsupported probes, not production defects. Final expectations come from independently executed Java and C# observations, not regenerated parity fixtures.

The audit also inventories93 declared public members across12 central interfaces/classes from the actual unit-api2.2 jar using javap, retaining its SHA256. This is not all inherited members, quantity subinterfaces, format variants or normative prose. The pinned TCK source is2.2.1-SNAPSHOT and reference Indriya2.2.5-SNAPSHOT; exact commits remain in upstream-pins.json. Full normative completeness remains a separate work item even if all113 annotations eventually pass.

The user updated runaway-budget policy: system free RAM is telemetry, not an admission gate. tools/Invoke-Budgeted.ps1 now follows that rule while preserving explicit process/job commit limits, descendant cleanup and active native read-back. ReserveMiB is informational for compatibility. This change does not qualify every watchdog failure branch or simulate system memory pressure.

## What remains to implement or qualify

| Priority | Concrete work | Native acceptance |
| --- | --- | --- |
| P0 | Normative/API coverage beyond annotation inventory | Each targeted semantic contract has native acceptance, exact reference/spec source and tests; identify genuinely optional Indriya extras |
| P0 | Exact graph/text seam | Supported affine/transformed/product graphs and decimal factors round-trip value/unit/scale/converter; exact numbers survive the public path |
| P0 | Typed recoverable parse/conversion/factory outcomes | Stable codes/locations, failure short-circuiting, preserved state; cancellation/faults propagate |
| P0 | Whole-catalog and supported-kind/provider coverage | All bases/bindings and declared provider extensions are executable through deterministic native contracts |
| P0 | Verification qualification | Resolve mutation tooling; inspect survivors/timeouts; actual Windows/Ubuntu evidence and independent review; CI gates remain blocked while CI is off |
| P1 | Package adoption | License/metadata, package-only consumers, documentation and reproducible release artifacts |

Modern .NET design/reuse recommendations: docs/modern-dotnet-compatibility-design.md. Preserve safe domain semantics and existing compatibility adapters. Do not require Java ServiceLoader, reflection signatures or broken narrowing/hash behavior in the native API merely to mimic the reference implementation. Such adaptations/deviations must remain explicit; they do not justify saying literal Java compatibility or certification is complete.

## Reproduction and claim boundary

Run guarded modes Tests, Reference, JavaTck and Requirements through tools/Invoke-TypedErrorVerification.ps1 in fresh E: directories. Run tools/Invoke-ReleaseAuditProbes.ps1 and tools/Export-PinnedJavaApi.ps1 under the same guard. Run tools/Validate-ReleaseAudit.ps1 and tools/Validate-CompletionPlan.ps1 for trace/accounting checks. The retained evidence manifest records exact hashes, versions, commands, outputs and limitations. Large raw artifacts remain outside Git.

No production domain behavior was changed by this audit. No assertion was promoted. CI was not enabled/dispatched. Full semantic compatibility remains the release objective; this audit does not authorize publishing a reduced-scope1.0 or provide a calendar estimate. A schedule requires the normative-contract inventory and bounded graph/text implementation assessment, not a percentage derived from113.
