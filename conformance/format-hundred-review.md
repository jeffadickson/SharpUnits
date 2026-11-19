# Java quantity formatting review

Baseline: `e9dcd8e`. GitHub CI remains disabled.

This increment targets retained display-value formatting, pinned Java double text,
canonical supported unit labels, appendable output and locale-sensitivity reporting.
The existing SI-only Format API is preserved. One hundred numbered scenarios were
checked against actual pinned Indriya, with compilation/full tests every ten,
coverage, mutations and seeded generated/fuzz verification. These counts are not
commits or formal assertion closures.

Source review: SimpleQuantityFormat.format writes the retained Number text, omits
the unit/delimiter for AbstractUnit.ONE, otherwise appends a delimiter and the
SimpleUnitFormat label. It returns its original Appendable. QuantityFormat's
default isLocaleSensitive method returns false (confirmed from the pinned API
bytecode). NumberQuantity narrows integral Double inputs before formatting.

The supported numeric profile is constructed finite doubles. Java Number identity,
parsed BigDecimal lexical scale and arbitrary unit-expression labels remain gaps.

## Delivered behavior and evidence

`FormatJavaQuantity` accepts retained adapter values, returns a string or appends
to an existing StringBuilder/TextWriter, and returns the original destination.
Writer exceptions propagate; the formatter does not flush or dispose the writer.
`IsLocaleSensitive` is false. Supported primitive Celsius/micro labels match Java;
custom definitions are not canonicalized by their symbol alone. Relative scale is
not printed, and parsing the formatted result loses that scale, as in pinned Java.

`JavaNumberText` uses exact binary rounding intervals rather than CLR's decimal
spelling. This handles Java 17's `1e23` spelling, subnormals, signed-zero narrowing,
scientific boundaries and integral Java Number narrowing. Four actual baseline
probes are retained in `format-baseline.json`; they compare the legacy API with
Java and do not claim its old contract was changed.

| Checkpoint | Full xUnit tests | Shared Java/CLR cases | Mismatches |
|---|---:|---:|---:|
| 10 | 864 | 468 | 0 |
| 20 | 874 | 478 | 0 |
| 30 | 884 | 488 | 0 |
| 40 | 894 | 498 | 0 |
| 50 | 904 | 508 | 0 |
| 60 | 914 | 518 | 0 |
| 70 | 924 | 528 | 0 |
| 80 | 934 | 538 | 0 |
| 90 | 948 | 548 | 0 |
| 100 | 962 | 558 | 0 |

All checkpoints compiled the tests and runner. The final checkpoint includes additional custom-definition and
writer-provider regression tests, bringing the final full suite to **962 passing**.
The baseline had 854 tests. Existing electrical, symbolic and converter tests pass.
Builds retain existing warnings; there are no build errors.

`format-review-100.json` records each input, actual Java/CLR result and checkpoint.
Pinned JDK 17.0.20.1, Indriya commit
`07a9aac9a46695ea0a4acf95b74ab7d92766f49a` and TCK commit
`96da910db2a9aca61580a14125dde94b5f1c0285` were executed locally: **112 TCK
tests pass**. These are Java tests, not 112 translated SharpUnits assertions.
The existing independent Fraction oracle passes 200 vectors (seed 385), and ten
differential-comparator infrastructure tests pass.

Seeded binary64 differential sampling (seed 385415) produces **512 exact text
matches**, with input/output SHA256 hashes in `format-fuzz.json`. The generator
includes finite values throughout the exponent range; sampling is not exhaustive.
FsCheck runs 300 arbitrary prefix/integer cases (replay `385,415`); a separate
256-case prefix fuzz loop (seed 385415) includes malformed UTF-16. Negative tests
cover null arguments and writer I/O failures. No unresolved counterexample arose
in these passing campaigns.

Eight non-equivalent compiled semantic mutants were killed: retained-value loss,
Celsius/dimensionless labels, locale flag, prefix loss, writer identity, wrong
long clamp and scientific boundary. `format-mutations.json` retains failing test
evidence. An initial removed-clamp probe survived because .NET 10's cast itself
saturates; it is recorded as equivalent and excluded from the eight. This bounded
manual campaign is not an exhaustive mutation score.

## Coverage and traceability

Coverlet 6.0.4 measures **1260/1359 lines (92.72%)** and **1140/1404 branches
(81.20%)** across the library. JavaNumberText has 55/56 lines and 77/84 branches;
QuantityTextFormat has 32/32 lines and 53/54 branches. The complete affected-class
breakdown and uncovered locations are in `format-coverage-final.json`. Java,
automation, test code and generated/runtime internals are not covered by these
managed-library metrics. Coverage is not semantic proof.

StrictDoc 0.30.4 validates and exports **166 stable requirements in five documents**.
The five JP-015 child requirements trace display magnitude, number text, labels,
append destinations and locale behavior to code/tests/evidence. Ledger validation
and test-link audit pass (71 mappings, no missing mapped test methods).

46-A2 and 46-A3 both check Java's exact public
`Appendable format(Quantity, Appendable)` signature. CLR overload behavior is
supported, but Java signature identity is not. 46-A4 checks the public
`isLocaleSensitive` method; a CLR property supplies equivalent reporting rather
than that Java method. These three move from GAP to PARTIAL_BEHAVIORAL_MAPPING.
46-A1 (registered formatter discovery) remains GAP; 46-A5 remains partial.
Ledger totals are **42 GAP, 50 IMPLEMENTED_UNVERIFIED, 21 PARTIAL_BEHAVIORAL_MAPPING,
0 VERIFIED**. **Zero assertions are formally closed by this batch.**

## Explicit remaining incompatibilities

Three separately executed probes in `format-known-limits.json` retain actual
differences rather than weakening expected strings:

| Input | Java output | CLR output |
|---|---|---|
| Parsed `1.2300 A` | `1.2300 A` | `1.23 A` |
| Parsed `1÷3 A` | `0.3333333333333333333333333333333333 A` | `0.3333333333333333 A` |
| Constructed 20 Fahrenheit | `20 ((K*5)/9)+459.67` | `20 °F` |

Arbitrary Number storage/precision, general compound labels, locale-dependent
format services, cursor parsing, provider/service discovery, exact Java API
identity and independent closure review remain outstanding. The finite-double
profile does not establish arbitrary Number compatibility. Temperature arithmetic
and its documented offset-unit policy are unchanged by this formatter increment.

## Reproduction and GitHub status

After restoring .NET projects and compiling the Java reference runner, run
`tools/Invoke-FormatCheckpoint.ps1 100`. `tools/FormatReview.cs` prepares/checks
the numbered corpus; it rejects missing rows or mismatches. Run
`tools/Invoke-FormatFuzz.ps1` for the binary64 sample and
`tools/Invoke-FormatMutations.ps1` for the bounded mutation campaign. Rebuild
restored source before normal testing/coverage after mutations. Known-limit JSON
can be passed directly to both reference runners and compared by ID.

Collect coverage with `dotnet test --collect:"XPlat Code Coverage"`, summarize
using `tools/Measure-ParserCoverage.ps1`, and export requirements with
`tools/Export-Requirements.ps1`. New automation is C#/PowerShell. Existing Python
TCK/Fraction/audit workflows and the required StrictDoc CLI remain in use.

GitHub `.NET CI` and `Java reference TCK` were read back as `disabled_manually`.
Both sources retain manual dispatch only; the OS matrix is Ubuntu/Windows and
macOS remains disabled. Local Windows evidence is not a new Ubuntu or GitHub CI
pass. Work is on the existing draft PR #8; no merge is claimed.
