# Java JSR 385 / Indriya compatibility gap analysis

Baseline: JSR 385 (javax.measure API), Indriya reference implementation, and UnitsNet as an optional .NET interoperability target. Target runtime: .NET 10.

**Assessment scope:** reviewed currently imported SharpUnits GitHub source, not historical local archives. A feature is NOT VERIFIED until its CI tests pass. Java behavioral compatibility does not imply Java binary/API compatibility.

| ID | Java capability | Current GitHub status | Priority | Acceptance tests |
|---|---|---|---|---|
| JP-001 | Base SI dimensions | Partial: integer and exact rational exponent models implemented; API parity unverified | P0 | all seven dimensions |
| JP-002 | Product and quotient units | Partial: quantity arithmetic | P0 | derived dimensions, associativity |
| JP-003 | Rational powers/roots | Implemented in RationalDimension/RationalUnitExpression; numeric scale uses double; unverified | P0 | square root, rational exponent |
| JP-004 | Dimensional equality and compatibility | Partial | P0 | positive/negative |
| JP-005 | Named derived and alternate units | Missing | P0 | V, Ω, W, Hz, N, Pa, J |
| JP-006 | Prefixes (SI decimal, binary where applicable) | Partial factories only | P0 | mA*kΩ, µV, kHz |
| JP-007 | Arbitrary user-defined units | Partial UnitDefinition | P0 | registration, collision, identity |
| JP-008 | Linear converters | Partial | P0 | round-trip, overflow, zero scale |
| JP-009 | Affine converters | Partial: Java adapter affine arithmetic reconciled; see quantity assertion review | P0 | °C/°F/K, temperature differences |
| JP-010 | Converter composition/inverse | Partial: UnitConversion and JavaUnitConverterAdapter; Java Number/step identity gaps | P0 | identity, composition, inverse |
| JP-011 | Nonlinear converters | Missing | P1 | domain restrictions, invertibility |
| JP-012 | Quantity conversion to compatible units | Partial | P0 | arbitrary compatible units |
| JP-013 | Unit parsing | Partial: registered linear symbols, parentheses, products/quotients and integer powers; full Java grammar absent | P0 | parentheses, symbols, prefixes |
| JP-014 | Quantity parsing | Partial: invariant number, ASCII-space separator, exact registered symbol; Java grammar/locale gaps | P0 | numeric and unit locale cases |
| JP-015 | Unit and quantity formatting | Partial: QuantityTextFormat invariant SI-to-display formatting; Java Number text, appendable and locale APIs absent | P0 | round-trip, locale, Unicode |
| JP-016 | Precision strategies | double only | P1 | decimal, rational, large/small |
| JP-017 | Quantity arithmetic and comparison | Partial: 185 shared quantity fixtures; signed-zero construction corrected; Number precision, affine inverse and general unit identity remain open | P0 | mixed scale, incompatible, NaN |
| JP-018 | Typed quantity interfaces | Partial electrical types | P0 | generic typed operations |
| JP-019 | Dimension vs semantic kind | Missing systematic kind model | P0 | torque vs energy |
| JP-020 | Unit systems and discovery | Partial: explicit registries and named provider catalog; Java automatic/type-based discovery absent | P1 | SI and custom registries |
| JP-021 | SPI/providers/extensibility | Partial: CLR provider catalog and scale-preserving IJavaQuantityFactory; no Java service-loader or generic factory equivalence | P1 | custom provider, isolation, both factory scales |
| JP-022 | Annotation and metadata | Missing | P2 | symbols, aliases, provenance |
| JP-023 | Compound unit normalization | Partial | P0 | A*Ω -> V, V/A -> Ω |
| JP-024 | Unit scale in symbolic algebra | Partial: RationalUnitExpression retains multiplicative scale; general symbolic/API parity unverified | P0 | mA*kΩ numeric preservation |
| JP-025 | Offset unit algebra safeguards | Partial: RationalUnitExpression rejects affine products; Java quantity adapter converts affine operands to SI | P0 | reject °C multiplication |
| JP-026 | Serialization interoperability | Missing | P1 | JSON versioned round-trip |
| JP-027 | UnitsNet adapters | Missing | P0 | bidirectional loss accounting |
| JP-028 | Java reference conformance corpus | Implemented, partial coverage: 258 shared fixtures, including 50 actual provider factory cases and 20 API admission probes; exact pinned TCK runner | P0 | independent fixtures |
| JP-029 | Documentation and examples | Partial XML docs | P1 | DocFX and examples |
| JP-030 | .NET 10 CI proof | GitHub workflows disabled by user; local .NET 10 checks do not provide new Ubuntu/Windows CI proof | P0 | passing Ubuntu/Windows |

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

The [second fifty-iteration review](../conformance/quantity-second-fifty-review.md) records local-only results after GitHub CI was disabled. Internal rational evaluation does not close JP-016 or establish full Java Number semantics.

The [third fifty-scenario review](../conformance/quantity-third-fifty-review.md) records local inverse/scalar, exact standard Fahrenheit, unit-identity and division-admissibility results. GitHub CI remains disabled and no new formal closure is claimed.

The [factory fifty-scenario review](../conformance/factory-fifty-review.md) adds a
scale-preserving factory API, corrects signed-zero construction, and reconciles
stale capability descriptions against current source. `51-A2` is a partial CLR
behavioral mapping; service discovery, Java generic types and CI remain open.
