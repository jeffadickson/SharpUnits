# Dimensional inference — engineering equation analysis

The `DimensionInference` engine solves for the SI dimensions of unknown variables in multiplicative equations, using exact rational Gaussian elimination across all seven SI base dimensions.

## Example: infer resistance

```csharp
var voltage = DimensionExpression.Of(Dimension.Voltage);
var current = DimensionExpression.Of(Dimension.ElectricCurrent);
var resistance = DimensionExpression.Variable("R");

var solved = DimensionInference.Solve(new[] {
    new DimensionEquation(voltage, current * resistance)
});
// solved["R"] is the exact SI dimension of electrical resistance
```

Multiple independent equations can determine several unknown dimensions simultaneously. Rational powers allow fractional dimensional exponents. Contradictory and underdetermined systems throw descriptive exceptions rather than returning arbitrary units.

This is **dimensional inference**, not a numerical equation solver, a physical-law validator, or a general computer-algebra system. It does not infer a numerical scale, a named unit symbol, or distinguish semantic kinds such as energy versus torque. Addition, transcendental functions and offset units require a future expression-level type checker.

The engine is useful for instrument procedure validation, code generation, and unit-aware DSLs. Requirements: SU-INF-001 through SU-INF-008. Tests: `DimensionInferenceTests`.
