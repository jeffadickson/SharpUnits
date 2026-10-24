# Second fifty-iteration quantity review

Starting implementation: `cf417d4`. The review was committed before implementation
as `8fcacd2`. GitHub CI remains disabled at the user's request; all new evidence is
local. These are 50 numbered scenario reviews with executable checks, not 50 commits
or formally closed TCK assertions.

The [review record](quantity-review-second-50.json) preserves source references,
hypotheses, Java results, .NET before/after results, precision dispositions and
checkpoint counts. New fixtures are `Q100-01` through `Q100-50` in
`java-reference/cases.json`. Earlier `Q50-*` evidence remains historical.

| Iterations | Scope | .NET tests passed at compile checkpoint | Differential cases |
| --- | --- | ---: | ---: |
| 1–10 | Reject nonnumeric scalar coercions | 481 | 118 |
| 11–20 | Supported numeric types and signed/unsigned boundaries | 491 | 128 |
| 21–30 | Equivalent converters and subnormal conversion | 501 | 138 |
| 31–40 | Shifted coordinates and decimal additive results | 511 | 148 |
| 41–50 | Extreme converter ratios and final rational rounding | 521 | 158 |

Library/tests and the conformance runner compiled after **10, 20, 30, 40 and 50**.
Final local verification passed: **521 .NET tests**, **112 pinned TCK tests**,
**158 differential fixtures within declared tolerances**, and **10 Python harness
tests**. An independent Python `Fraction` oracle also passed 100 conversion and
100 addition vectors. That oracle checks the CLR round-trip-decimal model, not
additional Java conformance assertions or another 200 review iterations.

## Corrections and evidence scope

The review resolved **35 pre-correction failing case outcomes**. Of the 50 new
scenarios, **48 final outputs match Java exactly** and two agree within explicit
bounds. Formal TCK closure remains zero.

The first ten Java checks inspect `Number.class.isInstance`; the CLR checks invoke
public scalar multiplication. This verifies argument admissibility, not Java
arithmetic with invalid arguments. The next ten call actual Java scalar multiplication.
Unsigned CLR inputs map to wider Java Number types. Double output does not prove
preservation of large integer or decimal storage precision.

Scalar methods retain `IConvertible` signatures but now require `sbyte`, `byte`,
`short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double` or `decimal` at
runtime. Previously accepted boolean, enum and numeric-text coercions now throw
`ArgumentException`. Null handling and finite-scalar validation remain. Custom Java
Number subclasses still have no general CLR equivalent here.

Conversion evaluates the complete affine expression from the stored display value
before rounding the final result. It avoids a rounded SI intermediate or a double
scale ratio that can overflow/underflow even when the final result is finite.
Addition/subtraction use rational evaluation with the existing absolute/relative
result rules. Round-trip decimal representations supply input coefficients; final
outputs remain doubles. The existing `Rational` type supplies internal fractions;
the final fraction is rounded to binary64 with ties to even.

Copy-unit fixtures resolve to original units in Java and distinct equivalent CLR
converters. They challenge converter behavior, not canonical unit-object identity.
Coherent `SiQuantity` still uses double precision and can round or underflow.
Finite coherent storage limits, Java numeric type identity, Java Double formatting
boundaries, exact unit rational metadata and MathContext rounding remain incomplete.

### Explicit Fahrenheit precision limitation

For `Q100-35` and `Q100-36`, Java returns `1.0000000000036774e-20` while SharpUnits
returns `1e-20`. Each declares an absolute tolerance of **`1e-30`**. This is bounded
agreement, not bit-exact Java rounding compatibility. Both original CLR results
were zero and failed that bound. Every other new fixture uses zero absolute
tolerance. These differences are documented and do not advance VERIFIED status.

Fixture expectation validation now honors per-case tolerances, including zero.
The comparator rejects boolean/numeric substitutions and records containing neither
a result nor an error. Before extreme-converter correction, unexpected runner exits
were captured one case at a time as diagnostic `UNHANDLED_RUNNER_FAILURE` records;
they were not normalized into Java exceptions. Minimum-subnormal inputs refined
iterations 24–26 so they actually exercised intermediate underflow.

## Reproduction and remaining work

Use the existing [pinned Java reproduction commands](evidence/quantity-2026-10-20.md)
with the current corpus. After building the conformance runner:

```sh
python tools/check_quantity_rounding.py
python -m unittest discover -s tools -p 'test_compare_*.py'
```

Use `--dotnet /path/to/dotnet` when .NET 10 is not on PATH. Source fingerprints,
source pins and the pinned TCK report summary are in the review record. No remote
CI run is claimed for this revision. Existing XML documentation warnings remain;
the build has no errors. Dedicated physical temperature arithmetic and unrelated
electrical/symbolic functionality retain their existing behavior.

Ledger totals remain **47 GAP, 50 IMPLEMENTED_UNVERIFIED, 16 PARTIAL_BEHAVIORAL_MAPPING,
0 VERIFIED**. Number precision/type identity, typed Java API surfaces, compound-unit
identity, affine inverse differences, formatting, factories and providers remain open.
