# First reviewed local behavioral closure — 51-A2

Verified scope: `QuantityFactoryTest.testQuantityFactoryScale`, pinned TCK
`96da910db2a9aca61580a14125dde94b5f1c0285`, on SharpUnits commit
`521dc82d5d0597ff2af9d20398f7baa766485700`. Qualification provenance is
`LOCAL_REVIEWED` under VER-LOCAL-001, not GitHub attestation or JSR certification.

The pinned method iterates available providers, obtains each Length factory,
gets the Length unit from the **current** provider's default system, creates
`10d` for every scale value, and asserts scale equality. The direct xUnit witness
`TckLocalClosureTests.FactoryScaleMatchesEveryAvailableProvider` follows that
control flow, additionally rejecting empty discovery and checking display value
and unit. Java Class-based Length lookup maps explicitly to the CLR `Length`
identifier; discovery covers the built-in immutable provider list. This does
not close external-provider discovery, Java generic/API identity, other factory
contracts or full specification compliance.

Retained byte-preserving artifacts are in
`conformance/qualification/521dc82d5d05/`: Windows and Ubuntu TRX bundles, pinned
Java Surefire bundle, and the independent AI-agent's structured approval record.
The ledger binds their SHA-256 digests, all471 tracked Git blobs at the tested
revision, pins, exact source/test methods, semantics and API adaptation. Review
approval is inside the hashed review artifact itself; editing ledger declarations
cannot reuse stale approval for replacement bundles. `.gitattributes` preserves
these raw artifact bytes across Windows/Linux checkouts.

Executed on the same physical machine: Windows/.NET11.0.0 and Ubuntu24.04.5
under WSL2/.NET11.0.0 each pass **3,301 tests, zero failed/skipped**. Each library
Coverlet report covers **2,432/2,544 lines (95.60%)** and **2,462/2,796 branches
(88.05%)**. The official Linux SDK11.0.100 archive matched Microsoft's SHA-512
release metadata. Native WSL storage holds Linux intermediates. Pinned Java
executes **112 passing methods**, including the exact scale method, on
OpenJDK17.0.20.1/Indriya `07a9aac9a46695ea0a4acf95b74ab7d92766f49a`.
JVM properties are retained and checked against raw Surefire XML. OS/runtime and
WSL context are measured by the producer, but local hashes cannot authenticate
an independent operator/physical host.

The verification-tool suite passes39 tests, including Hypothesis seeded checks
and synthetic local/CI contract controls. Existing library FsCheck, malformed
input and finite UTF16 checks run in both platform suites. This increment did
not run a coverage-guided fuzzer or broad Stryker campaign. Finite curated
mutation controls detect removal of the review-artifact digest binding and full
Java method identity. A separately compiled factory fault (`AlwaysAbsolute`)
passes its one-test baseline and fails the exact factory witness when relative
scale is discarded. These are three killed semantic fault controls, not a
Stryker score or broad mutation qualification. Only mutation work used the
runaway guard; ordinary tests, Java and documentation ran without memory caps.
Coverage for the C# capture helper itself was not measured; the Python gate has
separate statement/branch results in the retained evidence manifest. Coverage
does not substitute for the assertion-specific witnesses and artifact review.

An earlier candidate `7996298` passed execution but was **not qualified**:
the initial stricter TRX reader mistook legitimate theory rows sharing a test ID
for duplicate executions. Independent artifact review blocked approval. The
correction requires unique execution IDs and adds positive theory-row and
negative duplicate/missing-execution regressions. All three platforms were
recaptured at521dc82. Failed-gate artifacts remain separately retained on E:.

Reproduction: run `Invoke-TypedErrorVerification.ps1` modes Tests and JavaTck
against a clean checkout of the tested commit, execute Tests with native .NET11
on Ubuntu, and use `QualificationBundle.cs --local-capture` on each raw report.
Use fresh checked directories below CODEX_ARTIFACT_ROOT (default E:/Artifacts)
for Windows bulk output and native WSL paths for Linux intermediates. The
versioned ZIPs permit validation without rerunning or enabling CI:

```powershell
$python = Join-Path $env:LOCALAPPDATA 'Programs/Python/Python313/python.exe'
if (!(Test-Path -LiteralPath $python -PathType Leaf)) { throw 'Use an installed Python with the existing validation dependencies' }
if (!(Test-Path -LiteralPath tools -PathType Container)) { throw 'Run at repository root' }
& $python tools/validate_tck_ledger.py
```

The ledger is now **14 GAP,98 PARTIAL,1 VERIFIED**. Assertions552-A1/561-A4
remain open: their new catalog tests are preparatory; they still need the
actual Java unit-toString/symbol-eligibility bridge. The113 annotation inventory
does not exhaust normative behavior. Both GitHub workflows remain manually
disabled, with no dispatch. Further graph mutation, provider/type/API coverage
and release/package qualification remain separate work.
