# Java service/catalog gap implementation

Baseline: `2cc3f49`; 962 passing .NET tests. Both GitHub workflows are disabled.

This batch inventories 100 missing implementation obligations: 44 unit definitions
in the Java-style catalog, 33 quantity-type-to-system-unit bindings and 23 service
contracts. Each iteration targets one stable JP-SPI requirement. These are finer
than TCK assertions; 100 iterations do not mean 100 independently closed assertions.

The profile supplies built-in provider discovery, supported SIMPLE formatting,
system lookups, immutable catalogs, typed factory metadata and prefix discovery.
Automatic external assembly discovery, arbitrary Java Number storage and unsupported
format families remain explicit gaps. Existing native registries and APIs keep their
contracts. Compilation and full tests ran every ten items; Java executed independently.

## Implemented profile

`JavaServiceProvider.Current` exposes the built-in provider (priority 10), with an
immutable `Available` list. Its format service supplies the supported SIMPLE
quantity formatter and SIMPLE_DEFAULT unit formatter. Known aliases, optional
variant lookup, missing names and current-locale case conversion follow the
measured profile. Unsupported Java format families are not advertised.

`JavaUnitSystem` supplies the 44 members of pinned Indriya's `Units` system, with
their exact names, labels, SI dimensions and converter coefficients. This includes
the seven SI bases, named derived units, dimensionless alternate units and the
accepted non-SI definitions actually in that system. It is an isolated core
compatibility catalog, not an extension to electrical/circuit/metrology features.
The native nine-entry `UnitRegistry.CreateStandard` is unchanged.

System lookup is case sensitive. Unit text lookup accepts exact formatted labels,
not arbitrary human names or parser aliases. Dimension filtering preserves all
alternate units (for example Hz/Bq and one/rad/sr/%). A fresh registry can be
created without sharing mutable registrations with any other service.

All 33 Java quantity identifiers have system-unit bindings and cached factory
metadata. Temperature maps to K, not Celsius. Factory creation retains the supplied
unit/value/scale rather than converting to the mapped system unit. These are CLR
identifier APIs; they do not implement Java Class/generic identity or provide
compile-time CLR quantity-type admission. Unknown custom type identifiers get a
factory whose system unit is null, as independently observed for a custom Java
quantity interface. Java's raw factory also accepts an unrelated unit; this
adaptation does not invent a runtime dimension guard absent from that raw API.

Metric and binary prefix catalogs expose actual Java symbol/radix/exponent
metadata. Binary prefixes use radix **1024** and exponents **1–8**. Merely exposing
the mathematically equivalent radix 2/exponents 10–80 was caught as an API mismatch.

Example:

```csharp
var provider = JavaServiceProvider.Current;
var units = provider.SystemOfUnitsService.GetSystemOfUnits();
var factory = provider.GetQuantityFactory("Temperature");
var quantity = factory.Create(20, units.GetUnit("℃")!);
var text = provider.FormatService.GetQuantityFormat().FormatJavaQuantity(quantity);
// text is "20 ℃"; factory.SystemUnit is K; the retained quantity remains Celsius.
```

Existing physical-temperature arithmetic and its documented offset-unit policy are
unchanged. Native registries, electrical quantities and symbolic algebra remain
covered by the full regression suite.

## Iterations and executable evidence

Each numbered item has a stable JP-SPI requirement and one missing catalog member,
quantity binding or service contract. The first 44 add catalog definitions, the
next 33 add type mappings, and the last 23 reconcile service contracts. Shared
infrastructure supports multiple items; these are not 100 different public classes,
100 commits or 100 TCK assertion closures. They are not just 100 numerical variants
of the same formatter test.

| Checkpoint | Executed full-suite tests | Matched numbered obligations |
|---|---:|---:|
| 10 | 972 | 10 |
| 20 | 982 | 20 |
| 30 | 992 | 30 |
| 40 | 1002 | 40 |
| 50 | 1012 | 50 |
| 60 | 1022 | 60 |
| 70 | 1032 | 70 |
| 80 | 1042 | 80 |
| 90 | 1130 | 90 |
| 100 | 1140 | 100 |

Later checkpoints include additional edge/property/filter tests. The final
checkpoint and coverage run verify **1,140 executed/passing tests** through TRX
counters, rather than relying only on a console pass summary. An invalid even
FsCheck replay gamma initially prevented its new robustness class from running;
it was corrected to an odd gamma, and checkpoint 90/100 were rerun with execution
count accounting. No result from that incomplete property run is accepted as final
robustness evidence. Existing build warnings remain; there are zero build errors.

The service corpus contains **168** actual Java-versus-CLR observations: 100 scoped
items, 24 edge cases, and 44 dimension-filter membership observations. The earlier
558-case corpus was re-executed and compared separately: **726 observations total,
zero mismatches**. No string, boolean, dimension, prefix metadata or membership
tolerance is used. Converter probes permit explicit relative tolerance 1e-13;
zero is exact. The exact-factor property independently challenges early rounding.

The pinned Java TCK separately passes **112 tests**, using TCK commit
`96da910db2a9aca61580a14125dde94b5f1c0285`, Indriya commit
`07a9aac9a46695ea0a4acf95b74ab7d92766f49a` and JDK 17.0.20.1.
These Java tests do not establish 112 SharpUnits assertion closures. The existing
Fraction oracle passes 200 vectors (seed 385). Eleven comparator tests pass,
including raw UTF-8 versus JSON-escaped labels on Windows without UTF-8 process mode.

## Challenged semantics and testing

Actual Java observations required three API corrections: binary prefix radix and
exponents, Turkish format-name case folding, and null quantity-type lookup. Unlike
its null-rejecting text lookup, pinned system getUnit(Class) returns null for null
because it uses a HashMap. Evidence is retained in `service-gap-corrections.json`.

The existing Python comparator also needed explicit UTF-8 decoding: Windows locale
decoding otherwise invented 18 Unicode string mismatches. Its regression test and
JP-028-UTF8 requirement accompany the correction. New service automation is
C#/PowerShell; existing required Python TCK/Fraction/comparator workflows remain in use.

Two FsCheck properties execute 300 samples each, with replay seeds `385,521` and
`385,523`. They challenge exact 5/18 speed conversion and absent-name behavior.
Seeded malformed-name fuzzing executes 1,000 Unicode/control/surrogate inputs
(seed 385521). Negative tests cover null arguments, unsupported format families,
nonfinite values and invalid scales. Concurrency tests challenge factory caching;
mutation attempts through collection casts and cross-registry registration test
immutable discovery and isolation.

Eight compiled non-equivalent semantic mutants are killed: provider priority,
case-sensitive unit lookup, null type lookup, format-name locale conversion, binary
metadata, temperature system-unit mapping, capacitance dimensions and exact speed
factor. `service-gap-mutations.json` retains failing evidence and hashes. The last
mutant is detected by FsCheck with counterexample **3** (no successful shrinks),
replay `385,521`. This bounded campaign is not an exhaustive mutation score.

## Coverage, requirements and assertion accounting

Coverlet 6.0.4 measures **1408/1507 lines (93.43%)** and **1158/1422 branches
(81.43%)** across the managed library. The new service/catalog source has all
148 measured lines and 18 branches covered. The detailed report includes uncovered
locations in the rest of the library. Java, automation, tests, compiler-generated
record machinery and runtime internals are not represented by these library
metrics. Coverage does not establish semantic correctness.

StrictDoc 0.30.4 validates and exports **267 stable requirements in six documents**.
The 100 JP-SPI requirements and JP-028-UTF8 trace implementation and executable
evidence. [The assertion review](service-assertion-review.md) records exact semantics
for 26 assertions advanced from GAP to PARTIAL_BEHAVIORAL_MAPPING. Ledger and
test-link audit pass with 97 mappings and zero missing mapped methods.

Final ledger: **16 GAP, 50 IMPLEMENTED_UNVERIFIED, 47 PARTIAL_BEHAVIORAL_MAPPING,
0 VERIFIED**. **Zero formally verified closures** are claimed. CLR lists,
identifiers, properties and PascalCase methods do not satisfy Java Set/Class/
reflection signatures. Independent review and current CI evidence remain absent.

## Remaining incompatibilities and reproduction

`service-known-limits.json` preserves two actual differences: Java advertises four
quantity format families and four unit format families; this profile implements
only SIMPLE and SIMPLE_DEFAULT respectively. EBNF, LOCAL, NUMBERDELIMITER,
NUMBERSPACE aliases and ASCII remain unsupported. Mutable labels, unit appendable
formatting, full unit normalization, cursor parsing, arbitrary Number storage and
precision, provider replacement/external loading, and Java type/API identity remain
open. Collection immutability is deliberate; this does not reproduce Java's mutable
format-name key-set view. Previously recorded parsing/formatting limits still apply.

After building the Java reference project, run `tools/Invoke-ServiceGapCheckpoint.ps1 100`.
Historical lower checkpoints require their corresponding source snapshots because
later tests depend on the completed catalog. `tools/ServiceGapReview.cs` prepares,
retains actual Java observations, and compares independently executed CLR results.
Run `tools/Invoke-ServiceGapMutations.ps1`, then rebuild restored source before
normal tests or coverage. Collect `XPlat Code Coverage`, summarize with
`tools/Measure-ParserCoverage.ps1`, and export using `tools/Export-Requirements.ps1`.
The edge/filter JSON files are executable inputs for both reference runners;
the known-limit file intentionally records differences separately from passing cases.

Both GitHub workflows remain `disabled_manually`, their sources retain manual
dispatch only, and macOS stays disabled. Local Windows results are not new GitHub
or Ubuntu CI evidence. Work remains on draft PR #8; no merge or formal closure is claimed.

Follow-up: the [unit-format review](unitformat-gap-hundred-review.md) adds bounded ASCII, mutable labels and appendable formatting; 45-A3/45-A5 now have partial mappings. Earlier counts above are historical.
