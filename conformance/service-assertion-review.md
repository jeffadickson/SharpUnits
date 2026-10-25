# Service assertion reconciliation

Pinned TCK: `96da910db2a9aca61580a14125dde94b5f1c0285`. These 26 inventory
occurrences advance from GAP to PARTIAL_BEHAVIORAL_MAPPING. None is VERIFIED.
Exact source method names are retained in `tck-closure-ledger.json`.

| Assertion | Exact Java expectation | CLR evidence and remaining distinction |
|---|---|---|
| 53-A1 | `ServiceProvider.available()` is non-null | Immutable built-in provider list; no external ServiceLoader |
| 53-A2 | Available providers are nonempty | Built-in provider is present; does not establish all-provider discovery |
| 53-A3 | Current provider is non-null | Stable `Current` singleton; replacement/loading unsupported |
| 53-A4 | Current provider priority is an int within int bounds | Actual default priority 10 compared; property rather than Java method |
| 54-A01 | Each provider exposes non-null FormatService | Built-in supported format service; external providers unsupported |
| 54-A02 | Quantity format names are non-null/nonempty | Advertises implemented SIMPLE only; Java has four families |
| 54-A03 | Default **unit** format exists and quantity names are nonempty | Matches the actual test body; its description misleadingly says quantity format |
| 54-A04 | Unit format names are non-null/nonempty | Advertises SIMPLE_DEFAULT only; Java has four families |
| 54-A05 | Default unit format exists and unit names are nonempty | Bounded invariant unit formatter; full normalization unsupported |
| 54-A06 | Each provider exposes SystemOfUnitsService | Built-in service only |
| 54-A07 | Default system exists | `Units` system; CLR API shape differs |
| 54-A08 | Available systems are non-null/nonempty | One actual built-in system; external discovery unsupported |
| 54-A09 | Binary prefixes are non-null/nonempty with pinned count | Eight entries; full metadata compared, including radix 1024 and exponents 1–8; CLR records rather than Java enum instances |
| 54-A10 | Metric prefixes are non-null/nonempty with pinned count | All 24 metadata entries compared; CLR records rather than Java enum instances |
| 52-A1 | Each provider has a service with available systems | Built-in service/system; no Java TCK configuration or external loading |
| 52-A2 | Public `getName` method exists | `Name` property returns exact `Units`; Java reflection signature differs |
| 52-A3 | Public `getUnit(Class)` returns Unit | 33 identifier-based mappings match Java; strings do not establish Class identity |
| 52-A4 | Public `getUnit(String)` returns Unit | Exact label lookup, absent returns null; named API is PascalCase |
| 52-A5 | Public `getUnits` exists | Immutable complete catalog snapshot; CLR list rather than Java Set |
| 52-A6 | Public `getUnits(Dimension)` returns Set | Full membership compared for every catalog entry's dimension; CLR rational struct/list API differs |
| 51-A1 | Quantity factories exist for configured Java quantity types | Cached factories and 33 system-unit bindings compared; CLR identifiers/doubles rather than Java generic types/Number |
| 46-A1 | Configured quantity formatter list is nonempty | SIMPLE discoverable through supported service; official Java configuration list is not implemented |
| 45-A1 | Configured unit formatter list is nonempty | SIMPLE_DEFAULT discoverable; Java configuration identity remains absent |
| 45-A2 | Public `String format(Unit)` exists | Bounded string formatter behavior compared, including Celsius; general normalization and Java signature identity remain incomplete |
| 45-A4 | Public `isLocaleSensitive` exists | Actual false compared; CLR property rather than Java method |
| 45-A6 | Public `parse` method exists | Uses existing bounded grammar; does not imply complete Java grammar/cursor behavior |

`ServiceReferenceFixtureTests.MatchesPinnedServiceObservation` executes the 100
scoped implementation obligations plus 68 additional edge/filter observations.
Expectations are actual Java observations, never CLR-produced values. The official
pinned Java TCK separately passes 112 tests. Neither result establishes exact Java
API identity or current Windows/Ubuntu CI acceptance.

The 100 stable JP-SPI child requirements describe 44 catalog membership/definition
items, 33 quantity bindings and 23 service contracts. They are finer-grained than
the TCK inventory and are not counted as 100 assertion closures. The shared service
foundation supports multiple items; not every item required a separate public class.

Remaining GAP entries include mutable unit labels and unit appendable formatting
(45-A5/45-A3), fundamental Java types, Java annotation/configuration setup and other
unmapped structural requirements. Arbitrary Number precision/storage, full format
families, assembly/service discovery and independent review remain open even where
an assertion now has a partial mapping. The ledger policy still requires independent
review and CI evidence for formal closure; GitHub CI remains disabled by user request.
