# Converter composition and inversion

A converter maps source-unit values to coherent SI with `y = scale*x + offset`.

`a.Then(b)` means apply `a`, then `b`:

```csharp
var c = new UnitConversion(2, 3);
var d = new UnitConversion(5, 7);
var composed = c.Then(d); // scale=10, offset=22
```

`Inverse()` returns the inverse affine transformation, not a conversion between arbitrary units by itself. Both operations are defined only for finite nonzero scales. Converters involving non-linear transformations are outside this API.

For temperatures, distinguish absolute temperature conversions from temperature differences. Never use affine units in multiplicative unit expressions.

Requirement: JP-010. Tests: `ConverterCompositionTests`. Status: IMPLEMENTED_UNVERIFIED until passing CI evidence is attached.
