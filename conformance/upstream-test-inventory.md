# Java upstream test-port inventory

Source repositories:
- https://github.com/unitsofmeasurement/unit-tck (16 main test classes + 1 runner test class, master at inventory time)
- https://github.com/unitsofmeasurement/indriya (77 test classes, master at inventory time)

These are upstream **class counts**, not numbers of test cases. Pin upstream commit SHAs before importing exact test vectors.

## TCK suites
FundamentalTypesTest, TCKSetupTest, QuantityFormatTest, UnitFormatTest, QuantityInterfaceTest, QuantityTypesTest, ObtainingQuantiesTest, ObtainingUnitsTest, QuantityFactoryTest, ServiceProviderTest, ServicesTest, SystemOfUnitsTest, PrefixInterfaceTest, UnitConversionTest, UnitDimensionTest, UnitInterfaceTest.

## Porting order
1. UnitDimensionTest, UnitConversionTest, UnitInterfaceTest, PrefixInterfaceTest.
2. QuantityInterfaceTest, QuantityTypesTest, FundamentalTypesTest.
3. UnitFormatTest, QuantityFormatTest.
4. SPI and service provider suites.
5. Indriya regressions: converters, formatting, composite units, precision, mixed quantities, locale, edge cases.

## Porting rules
- Translate semantics to xUnit, preserving original upstream test path, case name and expected result in a traceability manifest.
- Reproduce negative cases and numerical tolerances; classify unsupported cases as explicit gaps, not skips hidden from CI.
- Do not copy Java TestNG/JUnit scaffolding.
- Retain upstream BSD copyright and attribution for adapted tests.
- Run upstream Java TCK independently; SharpUnits is not a Java JSR 385 implementation and cannot directly pass the Java TCK without an adapter.
- Cross-language fixture results must be pinned and compared by CI before requirement closure.

## Status
Upstream suite inventory completed. Initial idiomatic xUnit behavioral ports below are authored; execution and exhaustive porting remain open.
