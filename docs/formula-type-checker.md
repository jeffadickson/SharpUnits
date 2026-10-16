# Static engineering formula type checking

The `FormulaTypeChecker` validates an expression tree before execution. Variables are supplied with exact rational dimensions and optional physical-kind identities.

Supported operators: addition, subtraction, multiplication, division and constant integer powers. Supported functions: `sin`, `cos`, `tan`, `exp`, `log`, `sqrt` and `abs`.

Examples:
- `v / r` has current dimensions.
- `v + i` emits `SU-FORM-003` because voltage and current dimensions differ.
- `sin(v)` emits `SU-FORM-009` because the argument is dimensional.
- `sqrt(v)` produces exact fractional SI exponents.

The checker does not execute expressions or parse text. It is designed to sit behind a future DSL/parser adapter. Addition/subtraction optionally enforces semantic-kind identity. Multiplication and division currently return dimensional results without inferring semantic kinds; a future rule registry could assign a kind to products such as current times resistance.

The checker is independent of the JSR 385 TCK; it is a SharpUnits extension for procedure validation and unit-aware code generation.
