# Core mutation-debt work — 2026-10-30

This continues VER-005. Historical repair reports remain immutable. CI is disabled and no formal TCK assertion is promoted.


Final local verification: **3206 .NET tests passed**, including100 additional test cases since the prior repair. All11 shared Java/.NET corpora match (**2670 cases**), plus16 pinned normalized-chain witnesses. The pinned Java TCK passes112 tests;37 verifier tests pass. Coverlet measures2094/2189 library lines (**95.66%**) and2022/2304 branches (**87.76%**). New helper file apps, Java and workflows are outside that managed coverage scope.

Combined measured mutation outcomes are **954 killed,66 survived,6 uncovered,2 timed out**, with76 compile errors and96 ignored. This is a source-aware combination of retained phases, not a single-run score. Of the remaining survivors,39 have explicitly bounded equivalence/invariant dispositions,26 change observable diagnostic wording, and one changes the unspecified hash algorithm. The6 uncovered entries have individually documented source-invariant/redundant-profile dispositions. Two mutation-induced loop timeouts remain inconclusive. These judgments are not formal proofs, raw kills or full conformance closure.

The final scale primitive has9 deterministic boundary cases plus a500-trial configured FsCheck property; both checked-removal and addition-to-subtraction mutants are killed. Other new seeded properties configure500 primitive-promotion trials,500 projection-comparison trials and100 hash-quality trials. The hash artifact retains all100 actual trials. Full public multi-billion-operation chains were not executed. The final test/source freeze validates132 source/test files and12 fixture hashes.

See `mutation-debt-evidence.json`, `mutation-debt-latest.json`, `mutation-debt-reviewed.json`, individual phase reports/freezes and `mutation-debt-review.md` for provenance, actual classifications and acceptance limits.
## Confirmed implementation gap

The independently authored decimal128 boundary cases exposed six initial mismatches: `BigInteger` values with signed bit length63 can remain BigInteger in pinned Java even while fitting Int64. Positive2^62 is already in this category; negative-2^62 remains Long, while -2^62-1 does not. Decimal multiplication uses a34-significant-digit context for retained BigInteger and exact multiplication for Long. The previous `abs(value)>long.MaxValue` heuristic missed these values.

JavaNumber now retains private multiplication-promotion provenance alongside its normalized public Integer family. Explicit boxed Int64 and integral Double paths retain Long promotion. Exact BigInteger/decimal/rational inputs follow the signed-bit-length narrowing rule. Public integer equality intentionally ignores Java runtime class/provenance, so equal normalized values may select different subsequent decimal rounding. This is an explicit CLR representation adaptation, not Java class identity equivalence. AddExact remains available for mathematical exact addition; default large-integer addition is not routed through the signed64-bit Indriya bug.

`MutationDebtCases.cs` constructs exact products and independently rounds integer coefficients to34 digits, ties to even, before actual Java execution. Capture rejects disagreement. The pinned JVM agreed with28 retained boundary/comparison cases. Those cases and28 public boundary/negative cases raise the full .NET suite from3106 to3162 passing tests at the initial promotion-fix phase. Every one of the11 shared corpora passes (2656 paired cases). These phase counts do not substitute for final-source qualification after chain findings.

## Mutation witnesses

New tests challenge partially malformed dimension vectors, integer API overflow, whitespace accepted by BigInteger parsing,4096-scale parser budgets, null operand contracts,±1024 unit boundaries, graph import/export symbols, native adapter record retention, base-unit names, prefix recovery with misleading symbol/scale/dimension, same-base log/log and exp/exp compositions, bit-preserving identity conversions, and malformed prefix metadata.

The independent agent reviewed the narrowing rule and requested chains. `NumberChainReference.java` records16 chains with explicit `ns.narrow` at every operation boundary. These are normalized public-operation sequences; they do not establish raw Calculator accumulator parity. Java Calculator can retain an unnormalized accumulator until peek(), which is a separate unsupported claim. In particular, BigInteger(+2^62) negates and narrows to Long(-2^62); subsequent multiplication must follow the Long rule. Negating Long.MIN_VALUE widens, including across a second negation.

## Classification policy

Raw Stryker outcomes remain unchanged. A review disposition never turns a survivor or timeout into a test kill.

- Equivalent/unreachable conclusions require the exact statement and invariant. Examples include exponent-zero multiplication by10^0 and checked int exponent arithmetic bounded by the numeric budget.
- Public metadata changes are observable defects in test coverage, even when numeric values match.
- Diagnostic wording and hash algorithm changes are observable variations. If they lie outside the declared behavioral contract, classify that scope limit explicitly rather than calling them identical behavior.
- Timeouts remain inconclusive execution evidence. They are never added to demonstrated kills.
- Source statements removed or revised by an actual bug fix are reported separately from killed mutations; stable semantic matching uses source statement/mutator/replacement, not regenerated numeric IDs.

Detailed final counts, survivor dispositions, frozen source/test hashes, coverage, seeds and independent reviewer acceptance are retained in the mutation-debt manifests. Formal closure remains zero and Windows/Ubuntu CI is not dispatched.
## Scale-budget primitive and hash quality

The remaining checked scale sum was extracted into production-used `JavaNumber.CombineDecimalScales`. Only the test assembly receives internal access through the SDK friend-assembly mechanism; no new public API or fabricated private object state is used. Four unchecked overflow witnesses failed before the checked helper was integrated. Nine boundary examples and a seeded500-trial BigInteger-oracle property verify the primitive directly. This closes the cheap unit-level guard gap; a multi-billion-operation public chain was not executed. Final complete JavaNumber mutation replay qualifies the changed source rather than borrowing the pre-extraction report.

Constant converter hashing preserves functional equality/hash consistency but degrades dictionary use. The separate Draft `VER-HASH-QUALITY` criterion requires at least8 buckets for32 converters confirmed distinct by Equals, across100 seeded FsCheck trials. It allows collisions and promises no exact hash values or general performance bound. Recorded normal runs used32 buckets and32 dictionary equality comparisons; a constant-hash control used1024 comparisons for the same insertion/lookup workload. The constant converter-hash mutation is killed. The alternative numeric hash algorithm remains an allowed variation and was replayed with both equality/hash consistency tests and the quality check.

Two saturation-branch survivors are bounded to .NET10 CoreCLR's specified saturating floating-to-Int64 conversion. See the [Microsoft compatibility documentation](https://learn.microsoft.com/en-us/dotnet/core/compatibility/jit/9.0/fp-to-integer). This is source/runtime-contract reasoning, not executed Ubuntu CI or a promise for older/alternate runtimes.

The interrupted first final replay is retained as aborted diagnostics only and contributes no counts or qualifications. The narrowly filtered typed-method probe likewise is retained separately; final typed qualification uses the complete core test filter, which detects all6 selected mutations. Old report IDs are not stable across source changes. `MutationDebtTransitions.cs` compares exact statement, enclosing declaration, replacement and token span:64 historical survivors and11 historical uncovered mutations were demonstrated killed in the first broad replay; four revised/unmatched statements were not counted as kills. Twelve of those64 survivor kills were already demonstrated by the preceding repair's focused run.
