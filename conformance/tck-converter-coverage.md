# JSR 385 UnitConversionTest — assertion mapping

Pinned source: `unit-tck@96da910db2a9aca61580a14125dde94b5f1c0285`, `src/main/java/tech/units/tck/tests/unit/UnitConversionTest.java`.

| TCK assertion | C# mapping | State |
|---|---|---|
| 423-A1 provider available | UnitRegistry standard factory | Partial: no SPI provider |
| 423-A2 equals | Tck423A2_ConverterValueEquality | Authored |
| 423-A3 hashCode | Tck423A3_ConverterHashCode | Authored |
| 423-A4 inverse | Tck423A4_Inverse | Authored |
| 423-A5 isIdentity | Tck423A5_Identity | Authored |
| 423-A6 isLinear | Tck423A6_Linearity | Authored |
| 423-A7 convert(Number) | double-only numeric API | Gap JP-016 |
| 423-A8 convert(double) | Tck423A7A8_Convert | Authored |
| 423-A9 concatenate | Tck423A9_Composition | Authored |
| 423-A10 getConversionSteps | no step metadata | Gap JP-010 |

Tests are behavioral equivalents, not Java API-signature clones. All authored cases require passing CI evidence. No assertions are marked verified by source inspection.
