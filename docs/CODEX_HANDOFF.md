# Codex engineering handoff — SharpUnits

Repository: https://github.com/jeffadickson/SharpUnits
Baseline before handoff: `dde0daa1acd250498f7f4376e2a1d48068e70573`
Target framework: .NET 11. Primary development language: C#15.
Upstream Java TCK pin: `unitsofmeasurement/unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`.

## First reviewed local closure (2026-11-19)

VER-LOCAL-001 permits explicitly reviewed local evidence alongside the unchanged CI-attestation path. CI stays disabled. Assertion51-A2 is VERIFIED for the documented built-in CLR semantic adaptation at commit521dc82d5d0597ff2af9d20398f7baa766485700: Windows and native Ubuntu24.04/WSL2 each3301 passed; pinned Java112 passed; independent AI-agent review approves exact archives,471 Git blobs, methods, semantics and runtime/pins. Artifacts and structured approval are retained in conformance/qualification/521dc82d5d05; see conformance/local-closure-51-A2.md. Current ledger14 GAP,98 PARTIAL,1 VERIFIED supersedes historical zero-closure summaries below. This does not claim Java API/binary identity, external provider discovery, remote attestation or certification.

The first candidate7996298 was not qualified because the validator rejected legitimate theory rows sharing testId. The corrected gate validates unique executionId, with positive/negative regressions, and all captures were rerun at521dc82. Three curated semantic controls are killed; broad Stryker and coverage-guided fuzzing were not rerun. Assertions552-A1/561-A4 remain gaps pending actual Java toString/symbol-eligibility catalog bridge; their new tests are preparatory only. Continue with narrowly reviewed assertions and preserve full raw artifacts, platform/WSL provenance and structured review-digest binding. Ordinary jobs remain uncapped; mutation guards only.

## Current memory policy (2026-11-17)

The owner superseded universal memory budgets: ordinary builds, tests, benchmarks and Java/reference/documentation jobs must run without process-tree, aggregate or managed-heap caps. Do not gate or terminate work based on free RAM. The active verification runners now dispatch ordinary workloads directly; only mutation/fault-control modes use the existing runaway guard. ManagedHeapMiB is rejected for ordinary modes. Historical capped-run reports below remain historical evidence, not the current policy. Concurrency and task timeouts remain appropriate. See VER-RAM-TELEMETRY and VER-RUN-BUDGET.

The native text parser now preserves scaled/shifted parents under an explicit identity transform, including nested transforms and Celsius. The Java adapter shortcut remains unchanged. Four regression witnesses fail on the old implementation; an isolated IdentityParentMapping fault control reproduces those four failures. See conformance/exact-text-identity-review.md and its evidence manifest for fresh results and scope. Normative/API completeness, typed conversion/factory boundaries and broad graph mutation qualification remain open. No TCK assertion is promoted.

## Exact graph text continuation (2026-11-16)

User-requested exact formatting/parsing and typed errors add ExactTextFormat/TextResult/TextError over the immutable exact graph model. See docs/exact-text-format.md, requirements/exact-text.sdoc and conformance/exact-text-review.md/evidence. Native text preserves public numeric fraction/family/decimal scale, display unit, quantity scale and graph/converter/prefix structure; it is not full Java locale/EBNF or hidden number-class provenance persistence. Legacy successful grammar and aliases remain first; decimal/affine gaps have an exact fallback, with canonical native Fahrenheit record roundtrip. New graph format/parse does not force converter evaluation for fractional/nonlinear products. Native point/interval temperature policies remain unchanged.

Review fixed alias capture, rational-power division ambiguity, UTF16 binding/decode failures, DAG comparison work, emitted nesting limits and silent log-base projection. Native unit() denotes dimensionless independently of bindings; unit(JSON-string) resolves quoted names. Three Java/native acceptance differences remain explicit in the16-case reference corpus. All full-suite/coverage/mutation details and failed attempts are revision-bound in the final evidence report; do not quote a partially completed mutation run as qualified. CI stays off; ledger statuses remain14 GAP,99 PARTIAL,0 VERIFIED. Broad conversion/factory typed boundaries and normative/API completeness remain later work.

The only C#15-syntax-specific initializer was changed to an equivalent explicit ordinal HashSet constructor to test current Stryker5.1 compatibility; .NET11/C#15 targets and frozen/ordinal behavior stay intact. Historical migration evidence still describes the original syntax and its compiler failure. See the new evidence for the actual latest tool outcome; do not infer full tool/platform support from version availability.

## Compliance-led release audit (2026-11-16)

The owner reaffirmed full useful JSR semantics and requested a modern .NET domain rather than mechanical Java interface imitation. See conformance/release-compliance-audit.md/json and docs/modern-dotnet-compatibility-design.md. All113 TCK occurrences have exact source/method/implementation/test dispositions:81 API-shape,9 configuration,21 behavior,2 mixed;58 bounded mappings and55 API adaptations are not closures. The ledger remains14 GAP,99 PARTIAL,0 VERIFIED, now with per-entry releaseAuditRef. The five-package ownership snapshot hash is updated for those additive links only. Fresh runtime evidence and source/raw hashes are in conformance/release-audit-evidence.json; stock Java tests exercise Indriya, not C#. Affine-expression and decimal-factor parse gaps are reproduced; a large-factor Java text anomaly must not become a native default. The additional12-type/93-member API inventory is not complete normative coverage. docs/first-release-plan.md now preserves the full semantic release objective and marks its earlier reduced-scope proposal superseded.

Current user runaway policy treats free RAM as telemetry only. The Windows guard removes free-RAM admission gating, retaining explicit commit budgets/descendant termination/read-back. CI stays off; no formal assertion promotion, publication or native domain API change is part of the audit. Next implementation is the normative contract inventory and a bounded exact graph/text vertical slice, not a broad interface rewrite.

## .NET11 continuation (2026-11-13)

The user requested .NET11 migration and five work packages to complete the library. All four projects now target net11.0 with stable SDK11.0.100/no prereleases. Active runner commands resolve installed stable dotnet; workflows retain manual-only triggers and must stay disabled. C#15 comparer arguments and BCL frozen collections protect shared prefix/metric lookup metadata. See docs/net11-migration-and-library-assessment.md, docs/package-completion-five.md and conformance/completion-work-packages.json. The ownership map assigns all113 TCK occurrences exactly once to packages1–4 (60/11/20/22); package5 qualifies all113. Validate with tools/Validate-CompletionPlan.ps1. Historical .NET10 evidence is historical and does not qualify the new runtime. See conformance/net11-migration.md for executed migration evidence, versions and limits; no formal assertion is promoted by retargeting. CI stays off.

## Typed numeric failure continuation (2026-11-12)

See [typed errors and survivor follow-up](../conformance/typed-errors.md), the approved design and final evidence manifest. ParseDecimal/ConvertNumeric add typed recovery codes, parameters and optional UTF-16 positions; Decimal/FromNumeric preserve throwing behavior through shared validation. NumericResult supports Match and failure-short-circuiting Bind without swallowing unexpected faults or cancellation. Unit/quantity and arithmetic result boundaries remain outside this increment. The full .NET suite has3236 passing tests; all2670 paired Java cases and112 pinned TCK tests pass. Coverlet measures95.71% line/88.11% branch library coverage. StrictDoc validates390 IDs in nine documents. The selected Standard mutation profile has103 killed,15 survivors (nine diagnostic wording changes and six bounded invariants), three uncovered defensive arms,22 compile errors and250 ignored entries. Other JavaNumber arithmetic and historical timeouts are outside this profile; broad guarded qualification remains open. See the retained manifest for exact scope, hashes and failed attempts.

Use tools/Invoke-TypedErrorVerification.ps1 with fresh E: outputs and installed runtimes in ../tools-runtime. Windows verification now uses native read-back of job limits, shared same-user admission, descendant termination and a conservative queried watermark. Never assign a nested value-type field directly through PowerShell: the initial Basic.Flags write modified a copy, and the initial mutation run was therefore not memory-enforced. Failed/OOM/terminated runs are retained separately. Stryker's own worker count does not bound compiler/GC pools; the focused profile uses two .NET processors, workstation GC and an explicit managed-heap cap. StrictDoc imports bind BLAS pools to one thread; Java TCK uses bounded heap/processor settings. Read conformance/typed-errors.md for exact qualification limits. CI stays disabled and no formal TCK assertion is promoted.

## Historical mutation debt continuation (2026-10-30)

See [mutation debt reduction](../conformance/mutation-debt.md) and its retained source/test/report manifests. New public boundary and metadata witnesses exposed and corrected signed-bit-length integer promotion and normalized negation provenance. Separate pinned Java cases distinguish typed conversion from getConverterToAny, and normalized chains from Calculator accumulators. The checked decimal-scale sum is now a production-used internal primitive with direct overflow/property tests. Converter hash quality has a separately labeled Draft engineering criterion; no exact hash values or universal performance claims are imposed. GitHub CI remains disabled and formal TCK closures remain zero. Historical audit/repair reports stay immutable; combined mutation outcomes are explicitly multi-phase, with equivalent/invariant reasoning and diagnostic variations separate from kills.

## Verification repair continuation (2026-10-29)

See [verification repairs](../conformance/verification-repair.md) for current behavior, evidence and open gates. The historical audit below remains unchanged as a record of the defects it found. The repaired harness jointly observes quantity value/scale/unit/converter, retains raw Java numeric metadata, uses exact typed comparisons and requires revision-bound CI artifacts for VERIFIED ledger status. Default JavaNumber.Add/Subtract preserve large integer magnitude; named AddPinnedIndriya/SubtractPinnedIndriya opt into the measured Indriya narrowing bug. CI remains disabled; no formal assertion is promoted.

## Current continuation (2026-10-29, verification audit)

The [verification audit](../conformance/verification-audit.md) supersedes unrestricted completion interpretations of work package 1. Verdict: NOT QUALIFIED FOR REQUIREMENT OR TCK CLOSURE. Existing source and raw-log hashes match; fresh .NET and normalized differential replays pass, but observer and verification-tool blind spots remain. The quantity corpus contains 186 value/scale-only observations, four unit-only observations and 15 predicate/order cases. The separate reviewer corrected an initial count error; the correction and final agent concurrence are retained in the audit review.

Eight findings cover historical oracle mistakes, incomplete quantity/number/error observations, comparator and fixture false greens, weak ledger qualification, legacy-only quantity mappings and narrow mutation scope. The audit changes documentation, draft remediation criteria and reproducible diagnostic tooling; it does not silently repair verifier or arithmetic behavior. The prior implementation/coverage results remain historical scoped evidence. Six new draft VER requirements describe repair acceptance; none is a closed conformance requirement.

Current ordering tests detect deliberately reversed concatenation and transform faults in isolated snapshots. The historical tools/VerificationAudit.cs targets the pre-repair harness and is retained as source evidence. Use tools/VerificationRepair.cs for current negative witnesses; tools/Invoke-VerificationOrderAudit.ps1 replays directed faults on its explicit historical snapshot. The audit helper is a findings collector, not a universal qualification gate. All actual ledger entries remain non-VERIFIED; GitHub .NET and Java CI remain disabled. Next engineering work is verification repair against the draft criteria before further closure claims. Reviewer concurrence is with the audit's findings/limits, not certification or external human approval.

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
