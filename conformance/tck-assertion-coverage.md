# JSR 385 TCK assertion-level port status

Reference: unit-tck commit `96da910db2a9aca61580a14125dde94b5f1c0285`.

## Section 4.2.5 — UnitDimensionTest

| Assertion | Upstream Java test | .NET behavioral test | Status |
|---|---|---|---|
| 425-A1 | testEquals | Tck425A1_DimensionEquality | Authored, not CI-verified |
| 425-A2 | testHashcode | Tck425A2_EqualDimensionsHaveEqualHashes | Authored, not CI-verified |
| 425-A3 | testMultiply | Tck425A3_DimensionMultiplication | Authored, not CI-verified |
| 425-A4 | testDivide | Tck425A4_DimensionDivision | Authored, not CI-verified |
| 425-A5 | testRoot | none | **GAP** JP-003: rational dimension roots |
| 425-A6 | testPow | Tck425A6_Power_RepresentableIntegerPowers | Partial: no public Dimension.Pow |
| 425-A7 | testGetBaseDimensions | Tck425A7_BaseDimensionComponents | Partial: record properties, not dictionary API |

Java tests often check method availability through reflection; C# parity requires semantic equivalents rather than identical Java method names. The partial tests do not demonstrate full parity. They must not be counted as completed assertions until API-level acceptance and CI pass.

## Remaining suites

The other TCK classes listed in `upstream-test-inventory.md` are not yet mapped at assertion level. Indriya's 77 test classes are not yet mapped at method level. Issue #7 remains open.

## Closure

A TCK assertion requires a source path and ID, translated acceptance condition, implemented API, passing .NET test, and independent Java reference evidence. Missing cases are explicit gaps, not skipped tests.
