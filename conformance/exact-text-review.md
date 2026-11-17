# Exact graph text implementation and qualification

2026-11-16. Implemented bounded native exact graph/quantity text and typed expected failures. This closes the demonstrated decimal-factor and emitted native Fahrenheit parser gaps, adds lossless supported graph/value/scale roundtrips, and preserves legacy successful grammar and native temperature safety. It does not establish complete JSR semantics, Java binary compatibility or formal TCK closure.

## Delivered behavior

ExactTextFormat reads/writes the existing JavaUnit/JavaNumber/JavaNumberQuantity models directly. Public methods return TextResult for parsing and TryFormat, with Match/Bind composition, stable TextErrorCode, parameter and UTF16 location. Throwing format wrappers remain available. The codec retains public magnitude fraction/family/decimal scale, display graph, absolute/relative quantity scale, rational powers, nested transforms, atomic converter order and prefix metadata. It does not evaluate unsupported root/product converters to obtain display text.

Native syntax has conventional association/full consumption, explicit n÷d/binary literals, @absolute/@relative, transform operation lists, unit(JSON-string) bindings and alias-independent unit(). Unsupported custom atoms require explicit bindings. Log/exp bases must preserve the existing floating backend parameter representation; high-precision literal projection is rejected rather than hidden. This is a native invariant codec, not a general calculator, locale/EBNF implementation or hidden input-promotion provenance serializer.

Legacy parsers preserve exact registrations, labels and previously successful association/sentinel rules. Previously failing decimal/affine forms use the exact graph path where a finite native projection exists. The recognized standard Fahrenheit expression returns its native record, preserving the existing formatter's symbol/metadata roundtrip. Legacy quantity values remain doubles; lossless quantities use the new codec.

## Verification

Final full Windows suite:3294 passed,0 failed/skipped, including all3240 baseline tests. Coverlet:2431/2543 lines95.60%,2460/2794 branches88.05%, managed SharpUnits library scope only. Scripts, Java and demo are outside these percentages. No like-for-like performance or coverage-improvement claim is made.

FsCheck executes200 exact-transform roundtrips (seed385,1611) and300 malformed-input trials (seed385,1613). A deterministic sweep exercises65536 UTF16 escapes. Independent witnesses cover noncommuting affine coefficients, values beyond binary64/decimal128 precision, quantity numeric family/decimal scale and relative flags, converter/prefix metadata, aliases, unknown bindings, failure positions, cancellation, subsequent valid parsing, output/input budgets, shared DAGs, emitted nesting and culture independence.

New Java/native corpus:16 declared contracts,3 intentional acceptance differences,32 paired numeric observations for eight commonly accepted expressions. Raw Java values/classes are retained. Tolerance is1e-12 absolute/relative for projected samples; separate xUnit tests assert exact stored fractions and graph metadata. Native accepts `(A*0.25)` and `K-10` which pinned Java rejects; native rejects incomplete `m/` which pinned Java accepts as metre. These are explicit differences, not parity passes. Pinned Java TCK execution exercises Indriya, never the C# implementation directly.

Final all-corpus replay, Java TCK, StrictDoc and example outcomes are retained in conformance/exact-text-evidence.json. No outcome is inferred from a test merely authored or a compiler exit without completed results.

## Mutation scope and failed attempts

The initial current-source Stryker5.1 profile failed CS8652 on the unrelated released C#15 collection-constructor arguments. An equivalent explicit ordinal HashSet construction removes that syntax obstacle without changing .NET11/C#15 or frozen/ordinal semantics; PLAT-FROZEN acceptance now describes behavior and qualified-tool compatibility rather than a particular spelling. Historical migration/failed compiler evidence remains unchanged.

The first broader compatible profile was stopped after over13minutes without a completed report. It reached .NET11 VSTest hosts but has no qualified mutation outcomes. No partial or stopped campaign is a mutation pass. The final typed-result profile completed on .NET11:20 killed,3 diagnostic-wording survivors,5 ignored,0 selected compile errors/timeouts/uncovered mutants. Three substantive guard-test gaps in the first narrow run were addressed. The remaining survivors change only English exception messages; structured errors and exception categories are the contract, so these are not counted as kills. Full Reader/Writer Stryker qualification remains open.

Eight isolated snapshot semantic controls initially detected seven faults and left ProjectedAliasEquality alive. The original witness used a terminating fraction whose decimal128 representation differed from the perturbed fraction, so it did not challenge that fault. The strengthened witness uses two distinct repeating fractions sharing a decimal128 projection. The final alias replay completed with54 passing baseline tests and one failing test under the deliberate fault. The first replay detected the fault but failed metadata retention; the repaired helper was rerun successfully. Final status and hashes are retained explicitly. These finite controls are not Stryker or an exhaustive mutation score.

Retained failed attempts also include one test typo corrected before valid RED, an invalid even FsCheck replay gamma which aborted a run (corrected to1613), deliberate RED suites, initial diagnostic Java fixture assumptions corrected from actual observations, and successful pre-final profiles. They are not included in final pass counts.

## Independent review and dispositions

A separate read-only agent found alias interception, unbounded graph comparison traversal, power/division ambiguity, malformed UTF16 binding/decode behavior and emitted syntax depth beyond the reader budget. Each was repaired with a concrete regression witness; the nesting witness failed before redundant factor grouping was removed. Log-base projection and culture-sensitive powers have separate failing-before-fix witnesses. The reviewer subsequently found no remaining blocking defect within its static scope. This is agent review, not external certification or an independent rerun of every workload.

Graph matching uses reference-pair memoization and65536 comparison attempts; rendering has8192 visits/output units and the reader64 expression levels. These are source-reviewed bounded mechanisms with finite test evidence, not formal complexity or Dafny proofs. Source/runtime/tool versions, raw hashes and unsuccessful outcomes stay in the evidence manifest and artifact directory.

## Traceability and remaining work

Stable requirements:TXT-EXACT-GRAPH/NUMBER/ERROR/BOUNDS/BRIDGE/GRAMMAR. The broader REL-COMPAT-004 conversion/factory acceptance remains wider than this codec. Existing TCK-format entries gain an increment reference without changing GAP/PARTIAL/VERIFIED status. Ledger remains14 GAP,99 PARTIAL,0 VERIFIED; owner accounting is unchanged.

Still open: full normative/API semantic coverage, Java locale/EBNF families and structural API identity, broader typed conversion/factory boundaries, any mutation survivors/tool limitations recorded in final evidence, Linux execution, Native AOT and revision-bound CI qualification. Both workflows stay disabled_manually; no dispatch/publication occurred.

Reproduce full tests/corpora/Java/requirements through tools/Invoke-TypedErrorVerification.ps1 and exact Reference/Mutation/Example through tools/Invoke-ExactTextVerification.ps1 using fresh output directories under the verified artifact root. Invoke-ExactTextFaults.ps1 requires the same process-tree watchdog and operates only in a fresh artifact snapshot. See docs/exact-text-format.md and examples/ExactText.cs for the public API and native grammar.
