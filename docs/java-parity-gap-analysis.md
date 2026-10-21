# Java JSR 385 / Indriya compatibility gap analysis

Baseline: JSR 385 (javax.measure API), Indriya reference implementation, and UnitsNet as an optional .NET interoperability target. Target runtime: .NET 10.

**Assessment scope:** reviewed currently imported SharpUnits GitHub source, not historical local archives. A feature is NOT VERIFIED until its CI tests pass. Java behavioral compatibility does not imply Java binary/API compatibility.

| ID | Java capability | Current GitHub status | Priority | Acceptance tests |
|---|---|---|---|---|
| JP-001 | Base SI dimensions | Partial: integer exponent vector | P0 | all seven dimensions |
| JP-002 | Product and quotient units | Partial: quantity arithmetic | P0 | derived dimensions, associativity |
| JP-003 | Rational powers/roots | Missing from imported core | P0 | square root, rational exponent |
| JP-004 | Dimensional equality and compatibility | Partial | P0 | positive/negative |
| JP-005 | Named derived and alternate units | Missing | P0 | V, Ω, W, Hz, N, Pa, J |
| JP-006 | Prefixes (SI decimal, binary where applicable) | Partial factories only | P0 | mA*kΩ, µV, kHz |
| JP-007 | Arbitrary user-defined units | Partial UnitDefinition | P0 | registration, collision, identity |
| JP-008 | Linear converters | Partial | P0 | round-trip, overflow, zero scale |
| JP-009 | Affine converters | Partial: Java adapter affine arithmetic reconciled; see quantity assertion review | P0 | °C/°F/K, temperature differences |
| JP-010 | Converter composition/inverse | Missing | P0 | identity, composition, inverse |
| JP-011 | Nonlinear converters | Missing | P1 | domain restrictions, invertibility |
| JP-012 | Quantity conversion to compatible units | Partial | P0 | arbitrary compatible units |
| JP-013 | Unit parsing | Missing | P0 | parentheses, symbols, prefixes |
| JP-014 | Quantity parsing | Missing | P0 | numeric and unit locale cases |
| JP-015 | Unit and quantity formatting | Missing | P0 | round-trip, locale, Unicode |
| JP-016 | Precision strategies | double only | P1 | decimal, rational, large/small |
| JP-017 | Quantity arithmetic and comparison | Partial: 25 real quantity differential fixtures; Number precision/inverse/unit identity remain open | P0 | mixed scale, incompatible, NaN |
| JP-018 | Typed quantity interfaces | Partial electrical types | P0 | generic typed operations |
| JP-019 | Dimension vs semantic kind | Missing systematic kind model | P0 | torque vs energy |
| JP-020 | Unit systems and discovery | Missing | P1 | SI and custom registries |
| JP-021 | SPI/providers/extensibility | Missing | P1 | custom provider, isolation |
| JP-022 | Annotation and metadata | Missing | P2 | symbols, aliases, provenance |
| JP-023 | Compound unit normalization | Partial | P0 | A*Ω -> V, V/A -> Ω |
| JP-024 | Unit scale in symbolic algebra | Missing | P0 | mA*kΩ numeric preservation |
| JP-025 | Offset unit algebra safeguards | Partial warning only | P0 | reject °C multiplication |
| JP-026 | Serialization interoperability | Missing | P1 | JSON versioned round-trip |
| JP-027 | UnitsNet adapters | Missing | P0 | bidirectional loss accounting |
| JP-028 | Java reference conformance corpus | Implemented, partial coverage: 48 real Java/.NET fixtures and exact pinned TCK runner | P0 | independent fixtures |
| JP-029 | Documentation and examples | Partial XML docs | P1 | DocFX and examples |
| JP-030 | .NET 10 CI proof | Workflow present, unverified | P0 | passing Ubuntu/Windows |

## Electrical engineering extensions beyond Java parity

| ID | Capability | Status | Priority |
|---|---|---|---|
| EE-001 | Ohm's law typed operations | Partial | P0 |
| EE-002 | Power, charge, capacitance, inductance relationships | Partial power only | P0 |
| EE-003 | AC complex impedance/admittance | Missing | P1 |
| EE-004 | RMS/peak/phasor semantics | Missing | P1 |
| EE-005 | Measurement uncertainty and covariance | Missing | P1 |
| EE-006 | Symbolic equation solving with domain constraints | Historical prototype, not imported | P1 |
| EE-007 | Roslyn compile-time diagnostics | Missing | P1 |

## Verification policy

Each requirement closes only after: implementation; documented acceptance criteria; positive, negative, boundary and round-trip tests where applicable; cross-platform CI evidence; XML API documentation; and reviewer confirmation. STRIDE is a separate security-threat model, not the functional requirements tracker.

## References

- JSR 385: https://jcp.org/en/jsr/detail?id=385
- Units of Measurement API: https://github.com/unitsofmeasurement/unit-api
- Indriya: https://github.com/unitsofmeasurement/indriya
- UnitsNet: https://github.com/angularsen/UnitsNet

This is a functional capability inventory, not a claim that every Java method or overload has been exhaustively mapped. API-by-API inventory and pinned version conformance fixtures remain an open verification task.

Quantity assertion review and execution evidence: [matrix](../conformance/quantity-interface-review.md), [results](../conformance/evidence/quantity-2026-10-20.md). These updates do not close JP-009, JP-017 or JP-028.
