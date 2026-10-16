# Textual engineering formula analysis

The `FormulaParser` converts ordinary formulas into the existing `FormulaNode` expression tree. `FormulaAnalyzer` performs dimensional and semantic checking against a supplied symbol table.

```csharp
var symbols = new Dictionary<string, FormulaType>
{
    ["voltage"] = new(RationalDimension.From(Dimension.Voltage), QuantityKind.Voltage),
    ["current"] = new(RationalDimension.From(Dimension.ElectricCurrent), QuantityKind.Current),
    ["resistance"] = new(RationalDimension.From(Dimension.Resistance), QuantityKind.Resistance)
};
var result = FormulaAnalyzer.Analyze("voltage = current * resistance", symbols);
```

The parser supports numeric literals including scientific notation, identifiers, parentheses, unary signs, +, -, *, /, ^, and single-argument function calls. It reports malformed input with a position and a stable diagnostic code. Equality is checked as a statement, not numerically evaluated.

The parser does **not** yet support implicit multiplication, multi-argument functions, named unit literals, variable declarations, numerical evaluation, or semantic-kind inference from multiplication. Powers must have constant integer exponents for static checking. This is a deterministic dependency-free parser intended as a future DSL adapter, not a complete symbolic algebra system.

Tests: `FormulaParserTests`; requirements SU-FORM-012 through SU-FORM-015.
