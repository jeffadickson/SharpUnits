# QuantityInterfaceTest assertion review

Reviewed 2026-10-20 against TCK `96da910db2a9aca61580a14125dde94b5f1c0285`,
including `TestUtils` and the registered quantity classes. Reference implementation:
Indriya `07a9aac9a46695ea0a4acf95b74ab7d92766f49a` (`2.2.5-SNAPSHOT`).

All 16 assertions inspect structure. They do **not** specify the arithmetic outcomes
tested by our differential fixtures. C# properties, semantic kinds and `IConvertible`
are explicitly CLR adaptations. No entry is promoted to VERIFIED by this review.

The [2026-10-21 fifty-iteration review](quantity-fifty-review-2026-10-21.md)
adds executable magnitude, product representation, Fahrenheit, scalar, cast and
zero-division cases. It supersedes earlier descriptions of products always using
coherent display magnitudes. Compound unit identity remains partial.

The [second fifty-iteration review](quantity-second-fifty-review.md) adds numeric
input restrictions, supported scalar boundaries, rational converter evaluation and
decimal additive checks. Its evidence is local because GitHub CI is disabled.
Two tiny Fahrenheit cases retain explicitly bounded rather than bit-exact agreement.

The [third fifty-scenario review](quantity-third-fifty-review.md) adds direct
inverse/scalar decimal evaluation, exact standard Fahrenheit coefficients, limited
unit-identity normalization and direct scalar-division argument probes. Its local
evidence does not advance formal VERIFIED status.

## Assertion matrix

The xUnit identifiers below are in `QuantityParitySurfaceTests`. The tests now check
return types, declaring type and public access as well as parameter types. Properties
must have public getters and no setters. The Java helper walks superclasses; the
simple getter/inverse checks check public name and zero arguments, while
`isEquivalentTo` checks public name without requiring a specific parameter signature.
The explicit-signature checks require the exact erased Java return type, including
compiler bridge methods. Our CLR tests do not imply that those Java signatures exist.

| ID | Exact upstream method | Java check / CLR mapping | Remaining limitation |
| --- | --- | --- | --- |
| 431-A1 | testQuantityCastAsType | `Quantity asType(Class)` / I01 AsType(kind, registry) | No Java Class or typed quantity hierarchy; dimension check only |
| 431-A2 | testQuantityEquals | `boolean equals(Object)` / I02 Equals(object) | Java Number representation identity is not preserved by double storage |
| 431-A3 | testQuantityGetScale | public getScale() / I03 Scale | Enum/property adaptation; scale behavior tested separately |
| 431-A4 | testQuantityGetUnit | public getUnit() / I04 Unit | UnitDefinition is not javax.measure.Unit; compound identity remains partial |
| 431-A5 | testQuantityGetValue | public getValue() / I05 Value | double is not arbitrary Java Number |
| 431-A6 | testQuantityHashCode | `int hashCode()` / I06 GetHashCode() | Equal objects hash equally; Java numeric hash algorithm/identity not reproduced |
| 431-A7 | testQuantityIsEquivalentTo | public isEquivalentTo / I07 IsEquivalentTo(adapter) | Null/incompatible dimensions return false in CLR; numeric precision limited |
| 431-A8 | testQuantityOp0Inverse | public inverse() / I08 Inverse() | Linear inverse reconciled; CLR rejects absolute affine inverse early and approximates relative affine unit representation; unit identity remains partial |
| 431-A9 | testQuantityOp0To | `Quantity to(Unit)` / I15 To(UnitDefinition) | Affine double conversions supported; Number precision and non-affine converters absent |
| 431-A10 | testQuantityIsComparable | Comparable assignability / I16 IComparable | CLR null sorts first; Java null comparison throws; double precision only |
| 4311-A1 | testQuantityOp1Add | `Quantity add(Quantity)` / I09 Add | Affine and mixed-scale behavior corrected; arbitrary precision absent |
| 4311-A4 | testQuantityOp1MultiplyByNumber | `Quantity multiply(Number)` / I10 Multiply(IConvertible) | Runtime restricts input to supported numeric types; signature/type hierarchy and arbitrary precision remain different |
| 4311-A5 | testQuantityOp1DivideByNumber | `Quantity divide(Number)` / I11 Divide(IConvertible) | Same numeric surface limitation; division by zero exception adaptation |
| 4311-A6 | testQuantityOp1Subtract | `Quantity subtract(Quantity)` / I12 Subtract | Absolute subtraction now remains absolute; arbitrary precision absent |
| 4312-A1 | testQuantityOp2Divide | `Quantity divide(Quantity)` / I13 Divide(adapter) | Result scale corrected to absolute; display unit identity remains partial |
| 4312-A2 | testQuantityOp2Multiply | `Quantity multiply(Quantity)` / I14 Multiply(adapter) | Result scale corrected to absolute; display unit identity remains partial |

## Behavioral correction

`NumberQuantity` delegates arithmetic to `ScaleHelper`. Addition/subtraction use
system magnitudes, ignoring offsets for relative operands. Only two relative
operands yield relative scale. Scalar arithmetic converts absolute affine values
to system units before operating and converts back. Quantity products/quotients
yield absolute scale. `AbstractQuantity.compareTo` compares in the left unit,
converting the right quantity according to its own scale. `isEquivalentTo` delegates
to comparison, rather than comparing scale enums.

Linear inverse was subsequently challenged with six cases: `250 mA` yields
`0.004 (1/mA)` and preserves absolute/relative scale. Five failures were corrected;
see [inverse evidence](evidence/quantity-inverse-results.json).

These semantics can be surprising for temperatures. Absolute `20 °C + 10 °C` is
`303.15 °C`; absolute `100 °C - 20 °C` is `-193.15 °C`, still absolute. The Java
adapter now matches these behaviors. The dedicated temperature and core Quantity
APIs retain their existing behavior. This is an intentional behavioral correction
to the adapter; applications relying on its previous exceptions should review it.

## Evidence and reproduction

See [execution evidence](evidence/quantity-2026-10-20.md). The shared fixture runner
now invokes `Quantities.getQuantity` and real quantity methods, including for older
electrical cases. It no longer synthesizes Java quantity results with unit arithmetic.
The .NET runner invokes `JavaQuantityAdapter` rather than core `Quantity` operators.
The same quantity fixtures execute as xUnit theories on Windows and Ubuntu.

Before any further closure: challenge affine inverse and compound
unit identity, numeric precision/equality, typed casts and nonnumeric scalar inputs.
Retain partial status until the exact remaining API adaptations have an accepted
disposition and independent review. Formatting/factory/provider work follows this gate.

## Affine inverse and multiplication follow-up

Four more permanent shared cases cover absolute and relative Celsius products and
quotients, including system conversion before multiplication. All 58 fixtures match.
A separate Java probe constructs the inverse of `20 °C` with magnitude `0.05`;
asking its unit converter to convert to its system unit throws
`UnsupportedOperationException` from `ProductUnit.getSystemConverter`, because its
converter is not linear. SharpUnits rejects absolute affine inversion immediately; relative affine inverse currently uses a linear reciprocal factor, which does not preserve Java's non-linear unit representation. This is
an unresolved representational/API distinction, not a verified equivalence.
