# Exact unit graphs and typed text results

`ExactTextFormat` parses and formats the existing exact graph/value model without passing through `double`. Its bindings are immutable per-instance snapshots. It is a native invariant format, not a claim of complete Java SIMPLE/EBNF, locale or runtime-number-class interchange.

```csharp
var format = new ExactTextFormat();
var result = format.ParseQuantity("9007199254740993 A @relative");
var text = result.Bind(q => TextResult<string>.Success(format.FormatQuantity(q)));
Console.WriteLine(text.Match(value => value,
    error => $"Correct {error.Parameter}: {error.Code} at {error.Position}"));
```

`ParseUnit`, `ParseQuantity`, `TryFormatUnit` and `TryFormatQuantity` return `TextResult<T>`. `FormatUnit` and `FormatQuantity` are convenience throwing wrappers. `Match` handles exactly one outcome; `Bind` runs only after success and forwards the original error. Callback faults and cancellation propagate. Constructor argument validation remains programmer-error exceptions.

## Native syntax and roundtrip contract

| Syntax | Meaning |
| --- | --- |
| `m`, `AMPERE`, `°C` | Registered unit atom/catalog spelling |
| `unit()` | Alias-independent dimensionless unit |
| `unit("odd*name")` | Explicit binding using JSON string quoting |
| `m*0.5`, `m*(1÷3)` | Exact decimal or rational scalar factor |
| `m*2147483648` | Bounded arbitrary-size integer, without Java's observed narrowing |
| `(K*2)+3` | Shift by3 in the scaled unit: converter2x+6 |
| `(K+3)*2` | Scale before the shifted parent: converter2x+3 |
| `m^2/s`, `m^(1/2)` | Integral power followed by division; rational powers require parentheses |
| `transform(m;prefix(10,-3))` | Retain prefix radix/exponent identity |
| `transform(K;scale(2);offset(3))` | Stored converter composition order: scale(offset(x)); this is not pipeline order |
| `binary(0.1) A @absolute` | Preserve finite binary64 family |
| `1.2300 A @relative` | Preserve public decimal scale and quantity scale |

Ordinary multiplication/division and affine shifts associate left-to-right. Parentheses are explicit. Native power zero is conventional dimensionless identity; it does not copy the Java parser's missing/zero exponent sentinel. Whole-value parsing consumes all input. There is no implicit cursor or ignored suffix. Names containing operators/whitespace require `unit(JSON-string)` in this native grammar so aliases cannot intercept generated expressions. Legacy label precedence is unchanged in `JavaUnitTextFormat`.

The canonical writer may use `coefficient e exponent` notation without spaces to retain decimal scale, `n÷d` for rationals, and explicit `transform` operations where simplifying to a single scale/offset would lose graph identity. For log/exp, nonintegral bases use explicit `binary(...)` and must retain the existing floating backend parameter representation. Invalid bases and silent parameter projection return typed failures. These operations remain floating numerical functions; a lossless codec does not turn logarithms into exact arithmetic.

A successful roundtrip retains public magnitude fraction/family/decimal scale, display graph, quantity scale, normalized product exponents and atomic converter/prefix metadata. It does not promise hidden Java/CLR input-class or promotion-path provenance. Existing `JavaNumber` arithmetic promotion/decimal128 policies and core graph normalization are unchanged. Fractional roots and non-linear product graphs can be represented without obtaining an unsupported system converter. Unknown custom base/alternate atoms require an explicit binding.

Native physical temperature safety stays in `AbsoluteTemperature` and `TemperatureDifference`. Parsing an absolute/relative compatibility quantity does not replace those physical types or change their arithmetic policy.

## Recoverable failures and budgets

Errors have stable code, public parameter and zero-based UTF16 position when an input token is responsible. Missing tokens may point to end-of-input. Formatting failures have no source position. Error objects retain no original input string. A rejected parse leaves bindings and previously created quantities usable.

Limits:8192 UTF16 input/output characters;64 nested expressions;16384-bit exact numbers; existing4096 decimal token/scale limits;128 product atoms and exponent magnitude1024;8192 rendering visits and65536 memoized graph-comparison attempts. The writer rejects over-budget output rather than returning text it cannot read. Binding keys must be well-formed UTF16. Native bases/units remain immutable.

Examples of actionable failures:

```text
m/0       -> ZeroDivisor, position2
m^        -> InvalidExponent, position2
(m        -> UnexpectedToken, position2
unknown   -> UnknownUnit, position0
```

## Compatibility bridge and Java observations

The legacy throwing paths now read decimal-factor and affine expressions that previously failed. Exact registrations/labels and previously successful Java association/sentinel rules still win. The recognized native Fahrenheit definition round-trips its emitted `((K*5)/9)+459.67` expression. Legacy quantities still contain display doubles; use this exact codec for retained arbitrary precision and explicit quantity scale.

Independent Java observations retain16 declared cases and raw number metadata. There are three intentional grammar differences: the native parser accepts `(A*0.25)` and `K-10` which the pinned Java parser rejects; the native parser rejects incomplete `m/` which the pinned Java parser accepts as `m`. Eight commonly accepted cases provide32 paired converter observations with explicit1e-12 absolute/relative projection tolerances. Separate xUnit tests check raw fractions and graph metadata; projection agreement alone is not exact conformance.

Run `tools/Invoke-ExactTextVerification.ps1` modes Reference, Mutation, Faults and Example with a fresh output directory below the configured artifact root. Apply normal full Tests/Reference/JavaTck/Requirements modes using `Invoke-TypedErrorVerification.ps1` as well. All heavy runs use the existing native process-tree budget guard. `Faults` operates only in an artifact snapshot. A Stryker compiler failure is not a mutation pass; finite curated controls are reported separately from a mutation score.

Stable requirements:TXT-EXACT-GRAPH, TXT-EXACT-NUMBER, TXT-EXACT-ERROR, TXT-EXACT-BOUNDS, TXT-EXACT-BRIDGE, TXT-EXACT-GRAMMAR. Evidence and open qualifications:conformance/exact-text-review.md. CI remains off and no formal TCK status is advanced by this increment.
