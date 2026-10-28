# Work package 1: core behavioral reconciliation

Baseline: 41eb77b. CI remains disabled. Completion means implemented CLR behavioral contracts with pinned Java evidence and explicit Java API identity differences; it does not waive the existing formal verification gate.

## Acceptance checklist

- [x] Audit all 66 occurrences in UnitDimensionTest (7), UnitConversionTest (10), PrefixInterfaceTest (6), UnitInterfaceTest (27), QuantityInterfaceTest (16), retaining exact source methods and structural semantics.
- [x] Correct Java converter concatenation and unit-transform ordering; retain a named native-order composition operation.
- [x] Provide finite integer/rational/decimal/binary64 numeric representation, precision-preserving arithmetic, and explicit decimal128 rounding where pinned Java requires it.
- [x] Provide immutable base/alternate/transformed/product unit representation, normalized product identity, exact factors, roots/powers, converter composition and equivalence distinct from equality.
- [x] Integrate the core with unit adapters, quantity arithmetic/conversion/inversion, factories and semantic-kind checks without replacing native engineering safety policies.
- [x] Compare exact and negative cases against actual pinned Java; retain seeded property/fuzz checks and Stryker evidence.
- [x] Run full .NET regression, Java TCK, coverage and StrictDoc; document limitations and per-assertion dispositions; commit and push coherent increments.

Package 2 owns textual rendering/parsing of general graphs. Package 3 owns external provider discovery and Java type/reflection adaptation. Package 4 owns cross-platform qualification and independent review. Unsupported behavior in the core cannot be hidden by reclassifying it as a structural API difference.

## Design decisions

Use the installed .NET 10 standard library BigInteger and the existing Rational implementation for exact numeric/domain modeling. Microsoft documents [BigInteger](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.biginteger?view=net-10.0) as an immutable arbitrary-size integer; [CLR decimal](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types) has 28–29 digits and cannot represent the required Java decimal domain. This avoids a new numeric dependency while preserving explicit domain-specific rounding and promotion rules. Existing native ConversionPipeline remains apply-left-then-right. Java adapter concatenation adopts Java's right-then-left contract, with Then provided for explicit native ordering. C# signatures and runtime type identities cannot be Java binary interfaces.

## Evidence

See conformance/work-package-one-verification.json and the accompanying review. The local implementation profile passes 3,073 tests and 1,734 differential observations. Formal closure, cross-platform qualification and independent review remain separate package-4 gates. No VERIFIED TCK status may be assigned while the current CI gate is unmet.
