# Java Unit API parity: first adapter increment

The pinned Java `UnitInterfaceTest` contains 27 public-method assertions. This increment introduces `JavaUnitAdapter` for unit metadata, dimensional composition, prefixes, scaling, shifts and compatible conversions. Tests cover electrical products and conversion factors.

**Not yet equivalent:** alternate unit identity, named unit metadata, base-unit sets versus exponent maps, scalar `Number` overloads, `transform(UnitConverter)`, generic `asType`, and Java's `getConverterTo` versus `getConverterToAny` type semantics. These remain explicit gaps. Do not mark the 27 TCK assertions VERIFIED based on this initial adapter.

The adapter is designed to expose .NET equivalents while preserving the existing `UnitDefinition` and `RationalUnitExpression` APIs. It does not provide Java binary compatibility.
