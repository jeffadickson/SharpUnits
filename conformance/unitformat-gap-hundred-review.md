# Mutable unit formatting and ASCII implementation

Baseline `8cbdf09`: 1,140 passing .NET tests; both GitHub workflows disabled.

One hundred scoped missing obligations are inventoried before implementation:
32 mutable-label/alias/appendable/integration contracts, 44 ASCII catalog bindings,
and 24 ASCII prefixed-ohm bindings. These are finer-grained requirements, not
100 TCK assertions or formal closures. Each has a stable JP-UF requirement and
an independently executed Java observation. Compile/full tests run every ten.

Pinned source review targets UnitFormatTest 45-A3 and 45-A5. Java's collision,
removal, first-character validation, format-family fallback and shared quantity
formatter behavior must be measured rather than replaced with preferred semantics.
General unit identity, arbitrary Number storage, EBNF/LOCAL formats and external
provider discovery remain outside this batch. GitHub CI stays disabled.

## Delivered implementation

`JavaUnitTextFormat` now exposes Label, Alias, RemoveLabel, RemoveAlias,
RemoveAliases, and StringBuilder/TextWriter formatting overloads. Display labels and
parse names are separate maps: relabeling retains former names, collisions replace
the parse owner without erasing another unit's display label, and removing an alias
does not rename the unit. As in pinned Java, RemoveAlias ignores its unit argument
when selecting the binding to remove. Null alias removal is a no-op.

Fresh instances isolate mutable state. Built-in services share their default label
database with quantity formatting/parsing; canonical dimensionless quantity output
still omits the unit. Current shared labels also drive system text lookup without
changing immutable unit metadata or quantity-type bindings. The public CLR service
constructor supplies an isolated scope; this is an extra CLR facility, not Java
DefaultFormatService's shared-global-instance identity.

Supported catalog product graphs render current component labels when their own
macro label is absent. Parsing aliases uses original unit objects and resolves
components before algebra. Thus aliasing `m` to current changes `m/s` to current/time
rather than returning a cached speed unit. Explicit CLR registry entries take
priority over inferred prefixes, independent of dictionary enumeration order.

The service now advertises SIMPLE_ASCII and its ASCII/name-variant aliases. All
44 catalog spellings and 24 metric-prefixed Ohm spellings match actual Java. Eight
binary Ohm aliases/labels also match, retaining exact radix-1024 coefficients.
This is a bounded profile, not complete Java normalization of arbitrary units.

Measured ASCII conventions include `Ohm`, `Celsius`, `m*s^-1` and `m*s^-2`.
Default macro fallbacks deliberately retain `m²` and `m³`. Removing those macros
produces ASCII product forms `m^2` and `m^3`. Foreign base symbols can retain Unicode
too. ASCII flavor is not a blanket character replacement or output-sanitization API.

## Iterations and executable results

| Checkpoint | Full-suite tests executed/passing | Numbered obligations matched |
|---|---:|---:|
| 10 | 1150 | 10 |
| 20 | 1160 | 20 |
| 30 | 1170 | 30 |
| 40 | 1180 | 40 |
| 50 | 1190 | 50 |
| 60 | 1200 | 60 |
| 70 | 1210 | 70 |
| 80 | 1220 | 80 |
| 90 | 1230 | 90 |
| 100 | 1298 | 100 |

Each checkpoint compiled tests and runner and executed the full suite. Later
regression additions increased the final checkpoint from its original 1240 tests.
Final TRX counters confirm **1,298 executed and passing tests**, with no skips or
failures. Existing build warnings remain; there are no build errors. Existing
electrical, symbolic, factory, converter and quantity tests continue passing.

The unit-format corpus has **146** actual Java/CLR observations: 100 scoped items
and 46 integration/edge observations. Earlier quantity/parser/formatting (558) and
service/catalog (168) corpora were separately re-executed: **872 observations total,
zero mismatches**. All new outputs are exact strings/booleans; no output tolerance
or normalization hides differences. Existing numerical corpus tolerances are unchanged.

The pinned Java TCK independently passes **112 tests**, with no failures/errors/
skips. Pins: TCK `96da910db2a9aca61580a14125dde94b5f1c0285`, Indriya
`07a9aac9a46695ea0a4acf95b74ab7d92766f49a`, JDK 17.0.20.1. Java TCK tests
do not establish translated SharpUnits assertion closures.

## Corrections found by challenging the implementation

Four independently observed failures were recorded before correction in
`unitformat-corrections.json`: stale system lookup after shared relabeling and
default speed/area/volume output ignoring changed component labels. The original
failing test output and hash are retained; corrected observations now pass.

The JSONL comparator also incorrectly used Python splitlines, which split a valid
raw NEL character inside a JSON string and invented a JSONDecodeError. It now frames
records only at LF boundaries. The regression test also covers U+2028/U+2029. This
repairs the required existing Python workflow; new automation remains C#/PowerShell.
All twelve comparator infrastructure tests pass. The independent Fraction oracle
still passes 200 vectors (seed 385).

## Negative, generated and mutation verification

Actual Java and CLR validation was compared across all **65,536 UTF-16 code units
per flavor**. Recorded acceptance count and SHA-256 fingerprints of complete
bitmaps match exactly: default accepts 65,128 single-character labels; ASCII accepts
99. This is exhaustive for the initial UTF-16 character, not all possible strings
or Unicode scalar sequences. Default validates only that first character; ASCII
also checks every subsequent character. Some controls/surrogates are accepted by
the pinned rule; the implementation does not silently substitute stricter rules.

Two FsCheck properties run 300 cases each, replay `385,611` and `385,613`, challenging
former-alias retention and arbitrary destination prefixes. Malformed-initial-
character fuzzing runs 1,000 reproducible inputs (seed 385615). Negative tests cover
invalid labels, null destinations/arguments, invalid flavors and writer errors.
Concurrency and scope tests challenge map consistency and registry isolation.

Thirteen compiled non-equivalent semantic mutants are killed: relabel history,
alias-only display behavior, ignored removal owner, first-character-only validation,
ASCII Unicode validation, builder prefix retention, writer exception propagation,
product graphs, alias object identity, dynamic system lookup, ASCII fallback,
Java whitespace and explicit-registration priority. The mutation report retains
failing outputs and hashes. The former-alias property shrinks counterexample **1
to 0** (one shrink), replay `385,611`. This is a targeted campaign, not exhaustive
mutation coverage of the library or verification tools.

## Coverage and traceability

Coverlet 6.0.4 measures **1622/1721 lines (94.25%)** and **1368/1634 branches
(83.72%)** across the managed library. JavaUnitTextFormat has 135/135 measured lines
and 200/202 branches; its emitted binary-prefix iterator has 5/5 lines and 2/2
branches. JavaUnitAsciiData has 74/74 lines (no measured branches); UnitRegistry has
26/26 lines and 9/10 branches. Detailed uncovered/partial locations are retained
in `unitformat-coverage-final.json`. Java, test/automation code, runtime internals
and compiler-generated portions omitted by Coverlet are unmeasured. High coverage
does not prove semantic equivalence.

StrictDoc 0.30.4 validates/exports **374 stable requirements in seven documents**.
One hundred JP-UF items trace the scoped contracts/bindings; additional stable
requirements cover shared lookup, component rendering/parsing, BMP validation,
binary aliases, explicit registrations and JSONL framing. Requirement lifecycle
Active is not a verification claim.

45-A3 checks public Java `Appendable format(Unit, Appendable)` presence; 45-A5 checks
public label-method presence. The CLR adaptations now have executed behavioral
evidence, but not Java signature/object identity. Both advance from GAP to
PARTIAL_BEHAVIORAL_MAPPING. Ledger/test-link audits pass with 99 mappings and zero
missing mapped methods. Final ledger: **14 GAP, 50 IMPLEMENTED_UNVERIFIED,
49 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**. **Zero formal closures** are claimed.

## Remaining incompatibilities

`unitformat-known-limits.json` retains actual differences separately from passing
fixtures. Java permits Label(null, validName), while the CLR API enforces non-null
unit arguments. Default and ASCII Java formatters render the constructed Fahrenheit
unit as `((K*5)/9)+459.67`; this bounded CLR profile retains `°F`. General transformed
and arbitrary product normalization, affine prefix syntax, Java unit/class/generic
identity, full parsing/cursors, EBNF/LOCAL/NUMBERDELIMITER, arbitrary Number precision
and external provider discovery remain incomplete. Previously recorded bounds still
apply. Native temperature arithmetic and its documented offset-unit policy are unchanged.

## Reproduction and GitHub state

Build the Java reference project, then run `tools/Invoke-UnitFormatCheckpoint.ps1 100`
and `tools/Invoke-UnitFormatSupplement.ps1`. Historical lower checkpoints correspond
to earlier source snapshots; later tests require completed bindings. Run
`tools/Invoke-UnitFormatMutations.ps1`, then rebuild restored source before normal
tests/coverage. Collect XPlat Code Coverage and summarize with
`tools/Measure-ParserCoverage.ps1`; export requirements with
`tools/Export-Requirements.ps1`. Known-limit JSON is executable input for both runners
and intentionally records disagreements.

Both GitHub workflows remain disabled_manually, sources retain manual dispatch only,
and macOS remains disabled. Local Windows evidence is not a new GitHub/Ubuntu CI
pass. Work remains on draft PR #8; no merge or independent reviewer closure is claimed.
