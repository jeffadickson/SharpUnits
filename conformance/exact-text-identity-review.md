# Identity transforms and verification policy — 2026-11-17

`transform(m*2;scale(1))` formerly parsed as an unscaled metre. The reader
delegated to `JavaUnit.Transform(identity)`, whose compatibility shortcut selects
`SystemUnit`. The native grammar composes a local converter with the parent, so
an identity must retain that parent. The reader now returns it directly. The
same correction preserves shifted origins, Celsius and nested transforms.
The Java adapter and legacy successful parsing remain unchanged. This is a
native contract correction, not a newly established Indriya deviation.

Traceability: `TXT-EXACT-GRAPH` → `ExactTextFormat.Reader.Transform` →
`IdentityTextTransformPreservesParentMapping` (four theory cases). Before the
fix, all four cases failed. After the fix the full suite passes 3,298 tests,
with zero failures/skips. Library Coverlet coverage is 2,432/2,544 lines
(95.60%) and 2,462/2,796 branches (88.05%). Existing FsCheck rational roundtrip
and malformed-input properties run with seeds 385,1611 and 385,1613, together
with the finite UTF16 escape sweep. No coverage-guided fuzz campaign ran.

The isolated curated `IdentityParentMapping` control compiles both versions:
58/58 text tests pass at baseline; restoring system-unit selection yields
54 passing and four failing tests. This is one killed semantic fault control,
not a Stryker score or broad mutation qualification. Stryker was not rerun in
this increment. The mutation guard completed without a runaway intervention;
its observations are retained. An initial guard invocation rejected a missing
output directory before launching work; creating the checked directory allowed
the recorded run to proceed.

The exact Java comparison retains 16 contracts, 32 numeric pairs, three
intentional grammar acceptance differences and zero unexpected failures.
StrictDoc 0.30.4 validates 409 stable IDs across 12 documents. The evidence
manifest retains source-lock and raw-artifact hashes; it distinguishes the
pre-fix baseline from the corrected source. Historical evidence stays intact.

`VER-RAM-TELEMETRY` and `VER-RUN-BUDGET` now reflect the owner's November17
correction. Ordinary runner modes bypass Windows job limits and Java heap/
metaspace caps; explicit managed-heap limits are rejected for ordinary modes.
Only mutation modes, including curated faults, retain the runaway guard.
`Test-OrdinaryVerificationMemory.ps1` executes both real example launchers with
one-MiB mutation-budget arguments (ignored by ordinary dispatch), rejects a
managed-heap limit before creating output, and checks the ordinary runs do not
produce watchdog reports. This verifies runner behavior, not absence of limits
imposed by an external parent or operating system. PowerShell branch coverage
was not measured. JavaTck, Demo and all mutation-profile dispatch variants were
not individually rerun in this increment.

Reproduce from the repository root using a fresh directory below the configured
artifact root (existing dependencies and cache are required):

```powershell
$root = if ($env:CODEX_ARTIFACT_ROOT) { $env:CODEX_ARTIFACT_ROOT } else { 'E:/Artifacts' }
$parent = Join-Path $root 'SharpUnits'
if (!(Test-Path -LiteralPath $parent -PathType Container)) { throw 'Missing artifact parent' }
$run = Join-Path $parent ('identity-' + [guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $run) { throw 'Use a fresh run' }
New-Item -ItemType Directory -Path $run | Out-Null
./tools/Test-OrdinaryVerificationMemory.ps1 -OutputDirectory (Join-Path $run 'launchers')
./tools/Invoke-TypedErrorVerification.ps1 -Mode Tests -OutputDirectory (Join-Path $run 'tests')
./tools/Invoke-ExactTextVerification.ps1 -Mode Reference -OutputDirectory (Join-Path $run 'java-text')
./tools/Invoke-TypedErrorVerification.ps1 -Mode Requirements -OutputDirectory (Join-Path $run 'requirements')
```

GitHub's .NET and Java workflows remain `disabled_manually`; no CI was
dispatched. Windows local execution is fresh evidence; Ubuntu, NativeAOT,
full normative/API completeness, typed conversion/factory boundaries and broad
graph mutation qualification remain open. The 113-occurrence ledger remains
14 GAP, 99 PARTIAL, zero VERIFIED. One native defect was repaired; no formal
TCK assertion was closed.
