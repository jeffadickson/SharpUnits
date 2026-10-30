# Verification audit repairs — 2026-10-29

This repair supersedes the historical audit's unsafe tool/observation behavior. It does **not** declare SharpUnits JSR 385 conformant. The assertion ledger remains **113 occurrences: 14 GAP, 99 PARTIAL_BEHAVIORAL_MAPPING, zero VERIFIED**. All16 QuantityInterfaceTest occurrences now link both exact-core surface counterparts and action-specific behavior cases; the CLR API remains an adaptation.

## Findings and dispositions

| Finding | Implemented correction | Executable challenge |
|---|---|---|
| VA-01 wrong composition oracle | Retain independent literal expectations17 for concatenate and13 for transformed unit; preserve the original audit | REPAIR-CONCAT, REPAIR-TRANSFORM; CoreRobustnessTests noncommuting order tests |
| VA-02 incomplete quantities | Joint recursive value, scale, structural unit (including dimensions), converter identity/availability and zero/one observations; normalize before observing | Four independently corrupted-field cases; actual runner distinguishes equal magnitudes in different units;1000mA normalizes to1A |
| VA-03 collapsed number/error evidence and unsafe quirk | Retain raw Java runtime class/text/decimal scale alongside declared CLR normalization; capture failure stage, actual Java exception and approved CLR mapping; make lossy arithmetic explicit | RAW-DECIMAL/DOUBLE/RATIONAL/INTEGER; malformed decimal input; both operand orders and independent rational arithmetic |
| VA-04 comparator false greens | Typed exact comparison without binary64 integer narrowing; authoritative expected inventory/tolerances; reject nonfinite values and malformed outcomes | Large adjacent integer, bool/number, empty, duplicate-key, unauthorized tolerance and Unicode framing tests |
| VA-05 fixture false greens | Shared strict contract; omitted tolerances mean zero; legacy cases now explicitly declare zero; reject contradictory/duplicate/missing outcomes | Tiny-value loss, duplicate fixtures, result-plus-error and seeded malformed outcome tests |
| VA-06 fabricated closure | Require pinned occurrence/method, actual test references at tested commit, complete source proof, hashed execution/review records and matching GitHub workflow/job/platform/artifact digests | Isolated fabricated ledger and synthetic positive/negative CI-envelope tests; synthetic responses never count as real CI |
| VA-07 legacy-only mappings | Link all16 quantity assertions to ExactQuantitySurfaceTests, semantic facts and exact-core case IDs | validate_tck_ledger.py validates source occurrences, methods, test/case references |
| VA-08 unfinished mutation evidence | Completed broad frozen-source/test campaign; retain individual outstanding mutations, add survivor-driven tests and a separate focused replay | CoreMutationReview manifests; MutationQualificationTests; Invoke-RepairMutations.ps1 |

VER-001 through VER-006 are accepted Active StrictDoc requirements. Active describes lifecycle, not verified closure. Their implementation/evidence links are here and in the retained manifests. StrictDoc0.30.4 parsed/exported386 IDs across9 documents.

## Arithmetic and shifted units

`JavaNumber.Add` and `Subtract` now preserve arbitrary integer magnitude under the existing numeric promotion/rounding profile. `AddPinnedIndriya` and `SubtractPinnedIndriya` deliberately reproduce the observed signed64-bit narrowing bug: `2^64 + 0.1` becomes `0.1` only through those explicitly named methods. `AddExact` also bypasses decimal128 promotion/rounding and returns exact rational addition. Default arithmetic may still round under its documented decimal128 rules; it is not arbitrary precision decimal arithmetic for every operation.

Pinned Java absolute Celsius arithmetic is separately retained as a measured implementation quirk. For absolute20°C + absolute10°C, Indriya computes303.15 and returns it in°C. This is pinned measured behavior, not a physical interpretation of adding two absolute temperatures. Under native policy, absolute20°C plus a relative10°C increment yields30°C (303.15K); adding two absolute temperatures is a different, unsupported operation. Native absolute-temperature safety policies remain separate. Complete observations now prevent a value-only comparison from concealing the returned unit.

## Provenance and qualification boundary

Core expectations were captured from actual pinned JVM execution, never generated from CLR results. Four supplemental literals were authored independently before execution and capture rejects disagreement. Per-case Java row hashes, Indriya commit, raw representation mapping and explicit quirk classification accompany the core corpus. Legacy corpora retain their original normalized projections and now state that limitation; their Java row hashes do not imply full structural/runtime representation conformance.

`ReferenceLock.ps1` defaults to Validate and checks clean pinned upstream checkouts, source/test/corpus/verifier hashes, compiled Java observer bytes and exact dependency JAR bytes. Capture is an explicit authoring action, never automatic acceptance. Its local compiled-runtime lock is not an Ubuntu execution result. Build the observer first; differing runtime artifacts require review. Do not recapture merely to turn a failed validation green.

Formal closure additionally requires three GitHub-attested bundles: Windows and Ubuntu .NET TRX, and pinned Java execution. `QualificationBundle.cs` binds exact bytes of **every tracked Git blob**, including binary resources, dependency/build inputs and tools. CI requires an unchanged tracked checkout under Git's clean-filter rules. Bundles retain payload hashes and raw Surefire XML. The gate checks intended workflow paths, successful jobs, runner labels, exact tested SHA, matching GitHub artifact digest, complete source map and assertion-specific passed methods. It only reads GitHub; it never enables or dispatches CI. A source commit is tested first; a later ledger commit records its qualification, avoiding self-referential source hashes.

The trusted boundary is the reviewed CI producer plus GitHub artifact provenance and a reviewed ledger mapping. The gate cannot cryptographically authenticate a human reviewer, prove the semantic adequacy of arbitrary tests, or treat a mocked GitHub response as real execution. Local producer rehearsals are labeled LOCAL_UNTRUSTED and cannot establish formal closure. Expired remote artifacts may be retained locally only when their archived ZIP matches the published digest. A maliciously modified trusted workflow/producer is outside the stated boundary.

## Executed verification

Final full suite:3106 passed, zero failed/skipped. Managed-library Coverlet:2087/2185 lines (95.51%),1997/2294 branches (87.05%). Five Python verifier modules:261/272 statements (95.96%),106/124 branches (85.48%);37 tests passed. These are different measurement scopes.

- Pinned Java TCK:112 passed, zero failed/errors/skipped, JDK17.0.20.1+1, Maven3.9.9, pinned Indriya07a9aac and TCK96da910.
- All ten Java/.NET corpora:2628 paired cases, zero expectation/differential mismatches. This includes1738 core cases and5 supplemental repair cases.
- Repair diagnostic witnesses:14/14 pass; original unsafe acceptances are now rejected and the actual quantity runner distinguishes returned units. Historical audit files are preserved.
- Bounded Hypothesis6.170.12 campaigns: three seeds385001/385002/385003,1000 attempted examples each, database disabled, covering exact neighboring integers within±2^512, Unicode framing and contradictory outcomes. The enclosing test result is authoritative; counters alone are not success evidence.
- Library FsCheck properties retain replay seeds385,701/703/705/707 and1400 configured trials. Finite randomized campaigns are not proofs.

Exact final counts, coverage, mutation dispositions, source/test/log hashes and commands are retained in `verification-repair-evidence.json` and the linked JSON reports. Existing compiler XMLDoc/analyzer warnings are not claimed resolved. Coverage measures execution, not conformance. Java observers, C# helper file apps and workflows are not covered by managed-library Coverlet; the five Python verification modules have separate branch coverage. C# helper applications execute as managed .NET file apps (PublishAot=false); no Native AOT claim is made for them.

## Mutation limits

The completed broad campaign used one worker, disable-mix-mutants and the core/TCK/exact-surface filter across8 library files. It killed843 mutations, with145 survivors,22 uncovered and2 timeouts;76 compile errors and96 ignored in that scope are not kills. The larger whole-project report also contains excluded-file mutations; its initial4629 created total must not be used as the scoped execution denominator. Stryker's score treats timeout handling differently from a demonstrated test kill; the raw counts are the evidence.

Every surviving/uncovered/timeout entry is retained with exact source excerpt and an open or inconclusive classification. Text mutations may alter public metadata as well as diagnostic strings; none is automatically called equivalent. Guard-removal survivors may duplicate downstream validation; they remain review debt rather than assumed defects or kills. New tests challenge both-order BigInteger arithmetic, nonlinear relative conversion, null/scaled/incompatible equivalence, exact prefix bridge identity, native affine projection without graph round trips, nonbinary exact linear factors and maximum product-factor depth. The final focused replay killed36/43 executable mutants, with7 survivors and2 additional uncovered mutations. Its preceding replay killed33/43; newly strengthened witnesses killed three more. The focused replay has its own test hashes and does not retroactively change the broad campaign's frozen-test counts.

Remaining mutation debt is explicitly bounded and prevents a claim of exhaustive qualification. General factory/provider/formatting compatibility gaps also remain outside this verification-repair slice. Windows/Ubuntu/live Java CI qualification and external human acceptance remain outstanding because CI is disabled by user instruction. No platform evidence is fabricated and no formal assertion is closed.

## Reproduction

Run from an existing SharpUnits checkout after checking its directory exists. Reuse installed SDK/runtime paths; the local commands below use the primary .NET SDK10.0.402. Stryker's own project build resolved SDK10.0.403; these are distinct recorded toolchains.

```powershell
$root = Get-Location
if (!(Test-Path -LiteralPath $root -PathType Container)) { throw 'Missing checkout' }
$dotnet = Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
& $dotnet test tests/SharpUnits.Tests/SharpUnits.Tests.csproj -c Release --collect:'XPlat Code Coverage' --results-directory ../repair-final-coverage --logger 'trx;LogFileName=repair-final.trx'
& ./tools/ReferenceLock.ps1 -Mode Validate
# Supply a built runner and existing Java/Python runtime paths where installations differ.
& ./tools/Invoke-AllVerificationCorpora.ps1 -Runner src/SharpUnits.ConformanceRunner/bin/Release/net10.0/SharpUnits.ConformanceRunner.dll
& ./tools/Invoke-RepairMutations.ps1
python -m unittest discover -s tools -p 'test_*.py'
python tools/validate_tck_ledger.py
```

Install pinned development-only verification tools from `tools/verification-requirements.txt` into a suitable existing environment. Fraction uses Python's exact rational standard library; coverage.py and Hypothesis extend the established Python verifier test infrastructure, while application and new helper logic remain C#/.NET10. Primary references: [Fraction](https://docs.python.org/3.13/library/fractions.html), [coverage.py](https://coverage.readthedocs.io/en/7.17.0/), [Hypothesis](https://pypi.org/project/hypothesis/6.170.12/).
