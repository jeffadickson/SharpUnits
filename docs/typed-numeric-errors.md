# Recovering from numeric input failures

`JavaNumber.ParseDecimal(string?)` and `JavaNumber.ConvertNumeric(object?)` return `NumericResult<JavaNumber>`. A success holds a non-null value; a failure holds an immutable `NumericError`. Use `Match` to handle both outcomes and `Bind` to run a later stage only on success. Neither method catches unexpected faults or cancellation from application code.

```csharp
var result = JavaNumber.ParseDecimal(input)
    .Bind(number => NumericResult<Rational>.Success(number.Fraction));

var display = result.Match(
    fraction => fraction.ToString(),
    error => $"Correct {error.Parameter}: {error.Code} at {error.Position}");
```

Applications can translate these codes into their own field messages and recovery actions. `Position` uses zero-based UTF-16 offsets. Missing digits may identify the end of the token; `InvalidExponent` identifies the start of the exponent field. A null position means the failure is not tied to one character. The error retains no input object or token.

Supported objects are JavaNumber, BigInteger, Rational, sbyte, byte, short, ushort,
int, uint, long, ulong, decimal, float and double. Boxed long retains its existing
Java Long multiplication-promotion provenance. The numeric budget uses the existing
BigInteger.GetBitLength convention on normalized numerator and denominator, including
its signed-boundary behavior.

| Code | Recovery |
| --- | --- |
| NullInput (1) | Supply an input. |
| TokenLength (2) | Use 1 through 4,096 characters. |
| InvalidDigit (3) | Remove the invalid mantissa character, including whitespace. |
| MissingDigits (4) | Supply at least one digit before the exponent. |
| InvalidExponent (5) | Supply a signed Int32 exponent without whitespace. |
| ScaleBudget (6) | Keep fractional-digit count minus exponent between -4,096 and 4,096. |
| NumericBudget (7) | Reduce the normalized exact numerator or denominator to at most 16,384 bits. |
| NonFinite (8) | Replace NaN/infinity with a finite value. |
| UnsupportedNumericType (9) | Supply a supported numeric object; strings use ParseDecimal. |
| ZeroDenominator (10) | Replace an invalid default Rational with a constructed nonzero-denominator rational. |

`Decimal` and `FromNumeric` remain throwing APIs. Their exception types and parameter names are preserved. In particular, legacy Decimal reports an oversized exact number against `value`; ParseDecimal reports the supplied `text`. Typed errors are additive CLR behavior, not Java classes or a new TCK closure.

The railway boundary covers parsing and numeric-object conversion. Arithmetic methods and unit/quantity conversion retain their existing exception contracts. Bind does not turn exceptions from those operations into typed failures; callers must choose an appropriate boundary rather than assume every operation is exception-free.

Null handlers, null success values, invalid error construction and a continuation
returning null are programming faults and throw. Null inputs supplied to the two
numeric boundary methods instead return the expected NullInput error.

Run the [example](../examples/TypedNumericErrors.cs) for successful composition and field correction. Stable codes and structured metadata are tested independently of English legacy messages. Mutation reports retain wording changes as observable variations outside this recovery contract; they are not disguised as test kills.
