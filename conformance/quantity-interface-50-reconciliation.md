# QuantityInterfaceTest reconciliation — 50 local iterations

Pinned source: `unitsofmeasurement/unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`, `src/main/java/tech/units/tck/tests/quantity/QuantityInterfaceTest.java`.

Fifty local iterations produced a Java-inspired `JavaQuantityAdapter`, 16 assertion-specific CLR reflection checks and 34 quantity behavior tests. The code was integrated after the local development cycle; the original local ZIP and Git bundle are separate development artifacts.

| Java assertions | CLR counterpart | Status |
| --- | --- | --- |
| 431-A1 through 431-A10 | AsType, Equals, Scale, Unit, Value, GetHashCode, IsEquivalentTo, Inverse, To, IComparable | Implemented; requires semantic review |
| 4311-A1, A4, A5, A6 | Add, Multiply(IConvertible), Divide(IConvertible), Subtract | Implemented; Number precision and temperature semantics partial |
| 4312-A1, A2 | Divide(JavaQuantityAdapter), Multiply(JavaQuantityAdapter) | Implemented; derived-unit identity partial |

`QuantityParitySurfaceTests` contains 16 individual tests. `QuantityParityBehaviorTests` contains 34 additional tests. All require .NET 10 CI execution before being counted as passed.

## Remaining semantic differences

- Java `Number` is not equivalent to CLR `IConvertible`; the adapter currently converts through `double`.
- Java `asType(Class)` and quantity type hierarchy are represented only by dimension validation against a registered semantic kind.
- Java's absolute/relative quantity `getScale` semantics and affine arithmetic require upstream behavioral reconciliation.
- `equals` compares representation, while `IsEquivalentTo` compares coherent SI values and scale; exact Indriya equality semantics remain to be established.
- Derived unit symbols are currently placeholders, not Java compound-unit identities.
- The Java TCK's 16 assertions primarily check method availability through reflection, not these 34 behavioral scenarios.

No official TCK assertion is marked `VERIFIED` by this work. The .NET CI matrix now targets Windows and Ubuntu, not macOS.

## Subsequent pinned review

The [assertion review](quantity-interface-review.md) supersedes the provisional arithmetic assumptions above. Real Indriya quantities exposed 12 mismatches, now corrected and covered by 25 shared quantity cases. The [execution record](evidence/quantity-2026-10-20.md) distinguishes these results from formal assertion closure.
