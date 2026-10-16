# Static procedure validation

`ProcedureValidator` combines variable declarations, textual formulas, dimensional inference and the formula type checker into one static analysis pass.

```csharp
var report = ProcedureValidator.Validate(new[] {
    new ProcedureVariable("voltage", RationalDimension.From(Dimension.Voltage)),
    new ProcedureVariable("current", RationalDimension.From(Dimension.ElectricCurrent)),
    new ProcedureVariable("resistance")
}, new[] {
    new ProcedureFormula("voltage = current * resistance")
});
```

The engine infers that `resistance` has electrical resistance dimensions and then checks the complete formula. Diagnostics cover duplicate declarations, underdetermined dimensions, invalid formulas and mismatched dimensions.

Current inference accepts multiplicative equations and integer powers. It does not yet infer dimensions through addition, transcendental functions, or semantic physical kinds, and it does not evaluate formulas or interact with instruments. The implementation is intentionally a pure library component suitable for use by a procedure editor or code generator.

Tests: `ProcedureValidatorTests`. This is a SharpUnits extension, not an official JSR 385 conformance suite.
