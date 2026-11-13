# .NET11 migration — 2026-11-13

All four projects target net11.0 with stable SDK11.0.100 and C#15 defaults; no preview language features are enabled. The installed SDK/runtime are reused. Current runner paths and SDK selection are updated, including the independent rounding helper. Workflow source remains manual-only Windows/Ubuntu; GitHub CI remains disabled. Runtime consumers now need .NET11; no .NET10 multitarget package is claimed.

## Modernization and behavioral evidence

The fixed metric membership uses C#15 collection-expression constructor arguments to preserve StringComparer.Ordinal, then BCL FrozenSet. Prefix lookups use FrozenDictionary; an OrderedDictionary builder and ImmutableArray snapshot preserve the prior public MetricPrefixes order. Frozen collections and ordered/immutable collections are established BCL facilities, not new .NET11 inventions or evidence of measured speedups.

The first negative witness showed the shared Dictionary was mutable. After freezing, independent review identified public enumeration-order drift: the new sequence began a,c,d rather than Q,R,Y. A new public-order witness failed before the ordered immutable snapshot and passed afterward. No process-global prefix data was corrupted to obtain the witness. Stable cases test Q/q/µ identity; FsCheck seed385,1113 configures300 exact factor trials.

Final .NET11 suite:3,240 passed, zero failures/skips. Java/.NET replay:2,670 matching cases across11 corpora, with independently retained Java expectations. The existing Python Fraction oracle passes200 cases with seed385 against the net11 runner. The demo builds/runs, and the managed exact-core example retains its expected values/converter order/temperature outputs. API and native electrical/symbolic behavior are covered by the full regression suite; this is not exhaustive equivalence proof.

Coverlet measures2,147/2,243 library lines (95.72%) and2,032/2,311 branches (87.93%, calculated from counts). Tools, demo and Java are outside these percentages. Compiler/runtime and denominator changed, so this is not a like-for-like .NET10 coverage comparison. Existing XMLDoc/analyzer warnings remain; no warning-free claim is made. Examples run managed; Native AOT and Linux execution were not run.

## Mutation tooling limitation

Local tool manifest pins Stryker5.1.0. The guarded attempt cannot build the released C#15 collection-expression arguments: its embedded compiler reports CS8652 requiring preview. The stable SDK builds/tests the same feature without preview. No unsupported compiler patch or production preview switch is introduced to hide that mismatch.

The failed engine prints score0 and can return exit0. Assert-MutationReport.ps1 now rejects missing/no-execution reports; native process completion alone is not mutation qualification. That guard only checks execution exists, not intended scope, which must still be assessed separately. No .NET11 mutation kills/closures are claimed, and older .NET10 mutation reports remain historical. WP-COMPLETE-5 explicitly owns the C#15-aware mutation-toolchain qualification gap.

## Requirements and completion plan

StrictDoc validates397 IDs in10 documents. PLAT-NET11 and PLAT-FROZEN describe the implementation; JP-030 is updated to .NET11 while retaining its stable ID and unfulfilled CI-proof distinction. Draft WP-COMPLETE-1 through5 define the proposed delivery packages.

[Five-package plan](../docs/package-completion-five.md): core semantics60 assertions; text/formats11; catalogs/prefixes/factories20; providers/types22; qualification/release covers all113 without duplicate ownership. The JSON ownership map is validated against every ledger ID, source suite, current status and StrictDoc ID by Validate-CompletionPlan.ps1. This is planning coverage, not completed requirements. The ledger remains14 GAP,99 PARTIAL,0 VERIFIED.

The [reuse assessment](../docs/net11-migration-and-library-assessment.md) records language/BCL choices, current library candidates and why working test dependencies were retained. Native union/result-state, JSONL, span/Unicode and process APIs are candidates for bounded trials in the work packages, not silently adopted dependencies.

## Review and reproduction

A separate read-only agent found the stale rounding-helper path and the public prefix-order risk. Both were repaired, with independent Fraction checks and an order RED-to-GREEN witness. It found the five-package plan coherent and noted the mutation-report guard's intentionally limited scope. This is agent review, not human certification or formal proof.

Run current modes through tools/Invoke-TypedErrorVerification.ps1 with fresh directories under a verified CODEX_ARTIFACT_ROOT (default E:/Artifacts). Modes Tests, Reference, Requirements and Demo reproduce the main checks. Net11Mutation restores the local tool and reproduces the known compiler limitation; it must fail qualification. Existing Windows commit-limit watchdogs, shared admission and native read-back guard each workload. Raw outputs and exact source/tool hashes are retained by tools/Retain-Net11Evidence.ps1 in net11-migration-evidence.json. Historical evidence files are not overwritten.

Windows/Ubuntu/Java CI qualification is still blocked by the user's CI-off policy. No workflow was enabled/dispatched, release published or formal TCK occurrence promoted. The migration is a tested local foundation for the five proposed packages.
