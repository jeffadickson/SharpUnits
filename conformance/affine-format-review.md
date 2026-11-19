# Bounded Fahrenheit format reconciliation (2026-10-27)

The default and ASCII Java unit-format adapters now render the recognized standard Fahrenheit definition as `((K*5)/9)+459.67`, matching the pinned Indriya expression. Previously both returned the native symbol `°F`. This is one behavioral gap addressed with ten independently executed scenarios, not ten TCK assertion closures.

## Scope and independent reference

`UnitFormatReferenceRunner` constructs `Units.KELVIN.multiply(5).divide(9).shift(459.67)` using pinned Indriya 2.2.5-SNAPSHOT (`07a9aac9a46695ea0a4acf95b74ab7d92766f49a`). The ten observations in `unitformat-affine-cases.json` cover both format flavors, explicit Fahrenheit labels, parse-only aliases, removal of labels, and local kelvin relabeling. Java outputs were obtained before changing implementation. Eight cases failed on the old implementation; the explicit-label cases already passed.

Upstream `SimpleUnitFormat.DefaultFormat.nameFor` checks labels before transformed-unit handling and obtains the parent text using `baseUnit.toString()`. Consequently changing a *local* kelvin label does not change this expression. Shared global parent-label mutation and arbitrary converter graphs remain outside this bounded mapping.

Recognition checks the symbol, temperature dimension, exact standard converter record and absence of an additional exact-linear scale. A different converter or dimension sharing the symbol cannot acquire Fahrenheit semantics. Native registry symbols, converter values and native `QuantityTextFormat.Format` remain unchanged. The service-created Java quantity formatter uses the Java unit formatter; standalone quantity-format construction retains its existing symbol contract.

The native registry's `°F` parse alias remains an extension. The formatter does **not** gain affine-expression parsing or a general format/parse round trip. No Java unit identity or reflection compatibility is implied. Explicit `Label` overrides the expression and `RemoveLabel` restores it.

## Executed verification

- Baseline: 1,298 passing xUnit tests.
- New differential fixtures: ten actual Java/CLR observations, retained expected Java output and exact JSONL comparison.
- Final full regression: 1,312 passed, zero failed/skipped.
- FsCheck: 300 nearby-converter cases, replay `385,617`; deterministic dimension/symbol spoof and alias-removal negative checks.
- Pinned Java TCK: 112 passed, zero failures/errors/skips.
- Stryker.NET 5.0.0: all 14 executable mutants in the four changed spans killed; zero survivors, timeouts or uncovered mutants. This is a scoped score, not a whole-library mutation result.
- Comparator infrastructure: 12 Python tests passed.
- StrictDoc 0.30.4: 375 stable records validated/exported across seven documents, including `JP-UF-FAHRENHEIT`.
- Coverlet 6.0.4: 1,628/1,727 library lines (94.27%), 1,381/1,646 branches (83.90%). `JavaUnitTextFormat`: 141/141 lines and 213/214 branches. Java, automation and test-source coverage are unmeasured. Coverage does not prove semantic equivalence.

Mutation execution and source/log hashes are retained in `affine-format-verification.json`. The Stryker configuration is deliberately limited to four changed implementation spans. The launcher recalculates character offsets after newline conversion, checks selection is nonempty, and rejects survivors/uncovered mutants. [Stryker's configuration documentation](https://stryker-mutator.io/docs/stryker-net/configuration/) defines these spans as character offsets, not line numbers. An initial line-number configuration selected no executable mutants and is not counted as mutation evidence.

## Reproduction

From the existing repository directory after verifying it exists:

```powershell
$repo=(Get-Location).Path
if(!(Test-Path -LiteralPath $repo -PathType Container)){throw 'Select the existing SharpUnits checkout'}
Set-Location -LiteralPath $repo
$dotnet=Join-Path $env:USERPROFILE '.dotnet/dotnet.exe'
& $dotnet test tests/SharpUnits.Tests -c Release --collect:'XPlat Code Coverage'
./tools/Invoke-AffineFormatReference.ps1
./tools/Invoke-AffineFormatMutations.ps1
```

The differential launcher requires the existing compiled pinned Java runner/classpath and local runtimes documented in the handoff. Install/reuse Stryker.NET 5.0.0 at the launcher's configured tool path. Outputs remain outside the checkout. No GitHub workflow is enabled or dispatched.

## Traceability and open work

`45-A2` and `45-A5` retain partial behavioral status with the additional test reference. The ledger is unchanged at 14 GAP, 50 IMPLEMENTED_UNVERIFIED, 49 PARTIAL_BEHAVIORAL_MAPPING and zero VERIFIED. Two historical known-limit observations are now resolved for this recognized definition; their old CLR output is preserved. This does not close two full TCK assertions.

Remaining gaps include arbitrary transformed/product graphs and identity, affine unit-expression parsing and prefixes, global parent-label propagation for reconstructed transforms, full parser cursors and format families, Java Number precision, external service discovery, null-unit labels, and independent review/CI closure. Both GitHub workflows remain `disabled_manually` by user instruction. No current Windows/Ubuntu CI pass is claimed.
