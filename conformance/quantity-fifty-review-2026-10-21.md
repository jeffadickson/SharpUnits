# Fifty quantity compatibility review iterations

Starting commit: `630faff` (the temperature policy documentation was committed
before this review). Completed 2026-10-21. Each iteration challenges one numbered
scenario against actual pinned Indriya calls, with a permanent fixture and xUnit
theory. These are 50 scenario review iterations, not 50 implementation commits or
50 formally closed TCK assertions.

The [machine-readable review record](quantity-review-50.json) contains each
hypothesis, exact Java result, .NET result before correction, corrected result,
and mismatch disposition. Fixtures `Q50-01` through `Q50-50` live in
`java-reference/cases.json`. Explicit zero tolerances challenge magnitude
preservation; the xUnit runner now honors each fixture's absolute tolerance.

| Iterations | Review and correction | Tests passed at compile checkpoint | Java/.NET cases |
| --- | --- | ---: | ---: |
| 1–10 | Preserve supplied display magnitudes, including tiny Celsius readings and subnormal prefixed readings; equality/hash use display representation | 431 | 68 |
| 11–20 | Preserve linear factors and display magnitudes in products/quotients; convert affine operands to system units before composing | 441 | 78 |
| 21–30 | Verify Fahrenheit offsets, relative conversion factors, mixed scales and absolute arithmetic | 451 | 88 |
| 31–40 | Preserve scalar identity and relative/linear scalar magnitudes; challenge inverse representation | 461 | 98 |
| 41–50 | Execute matching and incompatible casts; normalize observed zero-division exceptions; distinguish zero absolute Celsius from zero interval | 471 | 108 |

The library/tests and conformance runner compiled after iterations **10, 20, 30,
40, and 50**. All final checkpoint tests and comparisons passed. The checkpoint at
20 first exposed an obsolete test expectation: `.25 A × 4 kΩ` has display magnitude
`1` in a unit with factor `1000`, and converts to `1000 V`. The test now asserts both
representation and physical value. Core electrical quantity operators were not changed.

Across these 50 scenarios, **22 failing case outcomes** were corrected (9 in the
first group, 9 in the second, 4 in the fourth). The other scenarios confirmed
behavior or expanded executable coverage. Cast/error instrumentation in the final
group required no production correction; its before/after records refer to the
same production implementation with the new fixture instrumentation.

## Implementation details and practical limits

`JavaQuantityAdapter.Value` retains the supplied double magnitude instead of
reconstructing it from a rounded coherent value. Same-unit conversion and scalar
identity preserve that magnitude. Representation equality and hashing use the
stored display value, unit and scale. Relative and linear scalar operations act
on display magnitudes before creating their result.

Products and quotients retain linear operand factors; affine operands are first
represented in coherent units. Display magnitudes may therefore change from older
SharpUnits adapter results. For example, `2 mA × 3 Ω` has magnitude `6` and unit
factor `.001`, and converts to `.006 V`. Use `To(target).Value` when a specific
output unit is required. Symbols are descriptive expressions; canonical Java unit
identity and normalization remain incomplete.

Preserving a display double does **not** provide Java arbitrary Number precision.
The coherent `SiQuantity` remains double precision and can lose small components
beside an offset, or round/underflow when scaling. Different-unit round trips and
general affine arithmetic retain those limitations. Preserving one-unit values
must not be interpreted as an arbitrary-precision implementation.

The fixture runner invokes Java `asType(Class)` and the CLR semantic-kind adapter
separately. Matching dimensions and incompatible casts are tested; the CLR API
still does not implement the Java type hierarchy. Exception categories normalize
CLR `DivideByZeroException` and the pinned Java zero-divisor exceptions, including
`RationalNumber.of`'s `IllegalArgumentException`. Unrelated Java arithmetic and
argument exceptions are rethrown, rather than silently classified as zero division.

The [physical temperature policy](../docs/temperature-semantic-envelope.md) is
unchanged: dedicated absolute/difference types enforce their operation rules;
the compatibility adapter follows Indriya conventions.

## Verification and remaining work

The full pinned TCK is executed in addition to the differential fixtures. Local
build logs and results were used at each checkpoint. Implementation commit
`ba3f53452906cc1e3fb9ce8753544b16c387872a` passed
[Windows and Ubuntu CI](https://github.com/jeffadickson/SharpUnits/actions/runs/39614054395) and
[pinned Java TCK plus differential CI](https://github.com/jeffadickson/SharpUnits/actions/runs/39614054406). macOS CI remains disabled.
Existing unrelated XML documentation warnings remain.

No assertion is promoted to VERIFIED. Remaining issues include Java Number
precision/type identity, typed API surfaces, compound-unit identity, nonnumeric
`IConvertible` inputs, and affine inverse construction/conversion differences.
Ledger totals remain 47 GAP, 50 IMPLEMENTED_UNVERIFIED, 16 PARTIAL_BEHAVIORAL_MAPPING,
0 VERIFIED. Formatting/factory/provider closure remains open.
