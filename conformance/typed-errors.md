# Typed numeric failures and survivor follow-up — 2026-11-12

This implements the approved additive CLR recovery boundary. It does not claim Java API identity or promote a TCK assertion. GitHub CI remains off.

## Implemented behavior

JavaNumber.ParseDecimal and ConvertNumeric return a sealed NumericResult with a stable NumericErrorCode, input parameter and optional UTF-16 offset. Match handles either outcome; Bind short-circuits failure and preserves the error. Unexpected faults and cancellation propagate. Decimal and FromNumeric share validation and preserve legacy exception types and parameter names. The legacy decimal bit-budget parameter remains value, while the result boundary identifies the supplied text.

Ten error categories distinguish null, token length, invalid digit, missing digits, exponent syntax, scale budget, numeric budget, nonfinite input, unsupported type and zero denominator. Exact numeric values and boxed Long multiplication provenance remain intact. Unit/quantity and arithmetic result APIs were not added.

Implementation: src/SharpUnits/JavaNumber.cs and NumericResult.cs. Tests: TypedNumericResultTests. User documentation and runnable example: docs/typed-numeric-errors.md and examples/TypedNumericErrors.cs. Requirements: VER-RAILWAY-ERRORS, VER-SURVIVOR-REPLAY and VER-RUN-BUDGET.

## Executed evidence

Fresh guarded full suite: 3,236 passed, zero failures/skips, including30 additional xUnit cases. Two FsCheck properties configure300 trials each, with replay seeds385,1101 (malformed suffix/offset and short-circuit) and385,1103 (independent exact Long products). The all-input-family comparison is consistency coverage because both public APIs share code; independent exact-value tests and the executed Java corpora provide separate reference evidence. The malformed-input property is a bounded seeded campaign, not broad coverage-guided fuzzing.

Fresh guarded Coverlet library coverage: 2,143/2,239 lines (95.71%) and2,084/2,365 branches (88.11%). NumericResult has full measured line/branch coverage; NumericError has a defensive unreachable mapping arm. Java, PowerShell, C# embedded interop, file-app helpers and workflows are outside these percentages. Existing XMLDoc/analyzer warnings remain.

All11 Java/.NET corpora replay successfully: 2,670 paired cases, zero fixture-expectation or differential errors. The typed error codes are CLR acceptance requirements; legacy Java observations exercise the unchanged success/error compatibility profile.

The final guarded selected mutation campaign completes with103 killed,15 surviving and3 uncovered mutants, with22 compile errors and250 ignored entries in the two source-file inventories. The118 executed mutants have no timeouts in this profile. Nine survivors change diagnostic wording; six have explicitly bounded source-invariant dispositions. The three uncovered entries are defensive normalization/error-code arms. These classifications are not kills or formal proofs. Compile errors/ignored entries may include statements outside the selected spans and confer no qualification.

Pinned Java TCK:112 passed, zero failures/errors/skips. StrictDoc0.30.4 parses, builds the graph and exports all390 IDs in nine documents. The managed example prints5/4 for1.25 and actionable InvalidDigit/ScaleBudget/UnsupportedNumericType outcomes. These outcomes, source/tool hashes and raw artifact hashes are retained in typed-errors-evidence.json; individual mutation dispositions are in typed-errors-mutations.json. No formal TCK closure is added: the113-occurrence ledger remains14 GAP and99 PARTIAL mappings. Windows local execution is measured; Ubuntu and GitHub execution remain unavailable under the CI-off policy.

## Resource guard repair and acceptance limits

The first mutation run completed, but its guard was misconfigured: PowerShell assigned a nested Basic.Flags value-type copy rather than writing it back. Its declared512MiB aggregate was not enforced; the observed native peak exceeded that declaration. It is retained as diagnostic execution and is not the final guarded qualification run. The initial missing-API build is a compile-time RED witness, not a runtime test failure. A later128MiB managed-heap attempt failed in Roslyn with OutOfMemoryException and contributes no mutation outcomes. Broad and Basic attempts subsequently reached configured aggregate watermarks and were terminated; their partial execution contributes no final counts. Initial documentation and Java launches also hit undersized budgets. The successful documentation retry bounds BLAS imports to one thread; the Java retry specifies a small initial heap and Serial GC. Every failed attempt remains separate from successful evidence.

The repaired Windows wrapper assigns the complete Basic struct, reads native flags and both limits back before releasing work, uses a same-user global admission mutex, and fails on a recorded breach independently of root exit status. It applies an explicit85-percent fail-fast commit watermark and reconciles retained peaks every250ms. Windows allocation-limit notifications are best-effort, so the query fallback is intentional ([Microsoft notification contract](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_associate_completion_port)). Kernel limits govern commit, not RSS; pre-assignment allocations and native accounting are not a physical-memory reservation against other applications.

Real regression checks pass for a child memory-limit event with descendant cleanup, root exit0 followed by a child watermark with wrapper failure, and separate-process admission refusal before any workload executes. Earlier checks failed for unenforced limits, the root-exit path and missing admission. These are finite Windows checks; abrupt watchdog crashes, every notification/race ordering and Linux cgroup operation are not exhaustively qualified. No unrelated application was terminated.

Bulk outputs, packages, coverage and raw reports are on an isolated E:/Artifacts run. Bounded existing Windows bin/obj and Maven target paths remain on NTFS. The reusable Invoke-TypedErrorVerification.ps1 records disk checks and effective resource limits; Invoke-Budgeted.ps1 rejects inadequate free RAM rather than starting the workload. Guard and runtime failures remain separate from conformance results.

## Mutation accounting

Historical mutation-debt reports remain immutable. The final selected profile uses Standard mutations over the full NumericResult file and JavaNumber's Decimal/ParseDecimal, FromNumeric/ConvertNumeric and Check/WithinBudget spans. Other JavaNumber arithmetic, rounding, hashes and primitive-double conversion methods are excluded from this profile; their historical survivors and timeout remain unresolved by this replay. The failed broader runs do not become completed qualification. Stryker uses one worker, two reported .NET processors, workstation GC and a256MiB managed-heap limit.

A fresh report is reviewed by exact source statement and replacement; raw statuses are preserved. Diagnostic English messages are observable variations outside the typed recovery contract, not identical behavior. Internal unused override branches and exact zero-power identities retain explicitly bounded invariant dispositions. Timeout is inconclusive, not a kill. No revised statement is silently counted as a killed historical mutant and no global score is inferred from a different universe. Standard includes equality, assignment and string mutations in addition to Basic control/arithmetic operators ([Stryker configuration](https://stryker-mutator.io/docs/stryker-net/configuration/)).

## Independent review

A read-only independent agent reviewed the numeric APIs, tests and guard. It found no confirmed numeric or legacy-exception regression by inspection. It required unconditional failure after child memory events and enforceable shared admission; both were corrected and challenged by actual checks. The real allocation test additionally exposed the nested-struct flags defect. The reviewer noted the consistency-only nature of comparing two shared implementations; separate mathematical and Java references are retained. This is agent review, not external human approval or formal proof.

## Reproduction

From this checkout in PowerShell7, verify paths and use a fresh output per stage:

```powershell
$artifactRoot = if ($env:CODEX_ARTIFACT_ROOT) { $env:CODEX_ARTIFACT_ROOT } else { 'E:/Artifacts' }
if (!(Test-Path -LiteralPath $artifactRoot -PathType Container)) { throw 'Missing artifact root' }
if (!(Test-Path -LiteralPath './tools' -PathType Container)) { throw 'Run from the SharpUnits checkout' }
$parent = Join-Path $artifactRoot 'SharpUnits'
if (!(Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent | Out-Null }
$output = Join-Path $parent ('typed-errors-' + [Guid]::NewGuid().ToString('N'))
./tools/Invoke-TypedErrorVerification.ps1 -Mode Tests -OutputDirectory $output
```

Other supported modes are FocusedMutation, Mutation, Reference, JavaTck, Requirements and Example. FocusedMutation regenerates character offsets for current source line endings; Mutation requests both entire files and is not claimed qualified by the focused replay. Execute them serially with new outputs. Defaults reserve512MiB for other work; the manifest records explicit run-specific limits. The successful selected mutation run used768MiB/process,1152MiB/job and128MiB reserve, with85-percent fail-fast watermarks and256MiB managed heaps. Its native peaks were559,841,280 and960,872,448 bytes. This records the constrained machine run, not a recommended universal RAM sizing. A refused launch is not a passing check. SDK10.0.402 is selected with latest-patch roll-forward and prereleases disabled. Existing installed StrictDoc, pinned JDK17, Maven3.9.9 and reference artifacts are reused.
