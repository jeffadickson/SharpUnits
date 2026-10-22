# Third fifty-scenario quantity review

Starting implementation: `9065fe8`. Review initialization was committed as
`7fdd759`. GitHub CI stays disabled at the user's request. These are fifty numbered
source/behavior reviews with local executable evidence, not fifty commits or
formally closed TCK assertions.

Fixtures `Q150-01` through `Q150-50` are recorded in `java-reference/cases.json`.
The [machine-readable evidence](quantity-review-third-50.json) contains each
hypothesis, exact Java result, CLR before/after result, disposition, source pin,
source fingerprint and compile/test checkpoint.

| Iterations | Review and correction | .NET tests passed | Differential cases |
| --- | --- | ---: | ---: |
| 1–10 | Compute inverse from displayed decimal magnitude, not a rounded SI reciprocal | 531 | 168 |
| 11–20 | Round scalar division and absolute affine scaling after evaluating the full expression | 541 | 178 |
| 21–30 | Evaluate standard Fahrenheit using its exact scale/shift definition | 551 | 188 |
| 31–40 | Check actual unit equality; normalize equal-unit cancellation and known squares | 561 | 198 |
| 41–50 | Directly exercise nonnumeric scalar-division rejection | 571 | 208 |

The library/tests and conformance runner compiled at iterations **10, 20, 30, 40,
and 50**. All final checkpoint tests passed. The review corrected **27 failing
case outcomes** (5 inverse, 6 scalar, 7 Fahrenheit, 9 unit identity). All fifty new
outputs match the Java reference with zero absolute tolerance.

Final local verification also passed all **112 pinned TCK tests**, including the
16 QuantityInterfaceTest methods; **10 Python harness tests**; and **200 independent
Python Fraction conversion/addition checks**. The full 208-case corpus agrees within
its declared tolerances. Earlier `Q100-35` and `Q100-36` retain their explicitly
documented bounded Fahrenheit agreement; no new claim of bit-exact parity is made
for those earlier cases.

## Behavioral details and compatibility boundaries

Inverse now evaluates the reciprocal of `Value`, as Indriya's `NumberQuantity`
does, while preserving scale and reciprocal display-unit factor. Scalar division
evaluates decimal input representations through the existing internal rational
arithmetic before rounding to double. Absolute affine scalar operations evaluate
their complete system-unit expression before converting back. Relative/linear
scalar multiplication keeps its existing floating-point behavior, matching the
corresponding Java path rather than applying decimal promotion indiscriminately.

The adapter recognizes the standard `UnitConversion.Fahrenheit` coefficient pair
by converter equality. It evaluates that standard definition as scale **5/9** and
offset **45967/180 K** (a shift of 459.67 °F). Generic user-defined coefficients
still use their supplied values. A custom converter with the same coefficient pair
is treated as the same standard Fahrenheit definition; the converter API carries
no separate rational-definition identity. Coherent storage is now computed through
the same conversion helper and remains a finite double. Construction directly
retains the supplied magnitude instead of reconstructing it from that cache.

Unit normalization is deliberately limited. Identical operands cancel to the
dimensionless representation. Identical A, V and ohm operands expose the known
square representation. Other compound expressions remain descriptive; general
factorization, identity and alternate-unit behavior remain incomplete. In
particular, actual Java reports `A·Ω` **not equal** to the named volt unit, despite
conversion compatibility. `Q150-40` guards against equating units solely by dimension.

The last ten Java checks inspect `Number.class.isInstance`; CLR checks invoke
`Divide(IConvertible)` directly. They establish nonnumeric parameter rejection,
including boolean false before zero-division handling. They do not execute invalid
Java arithmetic. These cases required fixture instrumentation but no production
input-guard change, and provide the direct divide coverage absent from the preceding
review's multiplication probes.

## Reproduction and remaining work

Use the [pinned Java reproduction commands](evidence/quantity-2026-10-20.md) with
the current corpus, then run `tools/check_quantity_rounding.py` and the Python
`test_compare_*.py` suite. The Fraction oracle checks the CLR round-trip-decimal
model; it does not establish arbitrary Java Number equivalence. Source pins,
fingerprints and the pinned TCK summary are stored in the review record.

No remote CI run is claimed. Builds retain existing XML documentation warnings and
have no errors. Dedicated physical temperature types and unrelated electrical and
symbolic features retain their behavior under the full regression suite.

Ledger status remains **47 GAP, 50 IMPLEMENTED_UNVERIFIED,
16 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**. Open differences include public Number
precision/type identity, Java formatting/MathContext boundaries, general rational
unit metadata, compound-unit identity, affine inverses, finite coherent-storage
limits, typed API surfaces, formatting, factories and providers.
