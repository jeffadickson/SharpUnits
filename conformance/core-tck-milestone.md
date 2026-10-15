# Core TCK milestone verification

Target: JSR 385 core behavioral parity in .NET 10. Upstream TCK pinned at `96da910db2a9aca61580a14125dde94b5f1c0285`.

## Evidence
- Successful cross-platform CI baseline: https://github.com/jeffadickson/SharpUnits/actions/runs/38815267131 (Ubuntu, Windows, macOS).
- This baseline covers previously committed dimensional, converter, rational quantity, and migration tests.
- Generic SI prefix implementation and tests are newer and must be verified separately.

## Core suite work
| Suite | Coverage | Remaining |
|---|---|---|
| UnitDimensionTest | A1-A7 adapted or partially covered | Full rational-root public API semantics and assertion-level reference execution |
| UnitConversionTest | A2-A9 partially covered | A1 provider, A7 Number overload, A10 conversion steps |
| PrefixInterfaceTest | Generic decimal prefixes authored | Method-by-method TCK assertion mapping |
| UnitInterfaceTest | Product/quotient unit behaviors | Full TCK mapping |
| QuantityInterfaceTest | Arithmetic and compatibility | Full TCK mapping |
| QuantityTypesTest | Selected electrical types | Full quantity type catalog |
| FundamentalTypesTest | Partial | Full mapping |
| Formatting and SPI | Not yet ported | All relevant suites |

## Completion rule
Do not call this milestone complete until all core TCK assertion IDs have a documented disposition (verified / gap / not applicable), all supported tests pass on three operating systems, and Java reference results are independently reproduced. A passing .NET test suite alone does not prove full JSR 385 conformance.
