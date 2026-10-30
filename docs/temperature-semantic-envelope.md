# Temperature semantic envelope

Absolute temperatures and temperature intervals have different arithmetic rules.
An absolute temperature behaves like a point; an interval is a signed difference
between points. This distinction matters for shifted units such as Celsius and
Fahrenheit: converting to kelvin does not erase the distinction.

## Physical temperature arithmetic

Use `AbsoluteTemperature` and `TemperatureDifference` for distinct compile-time
types. `TemperatureDifference.Kelvin` measures kelvin increments, not an absolute
temperature. A difference of 10 °C equals 10 K or 18 °F.

| Operation | Result |
| --- | --- |
| Absolute − absolute | Difference |
| Absolute + difference | Absolute |
| Absolute − difference | Absolute |
| Difference + difference | Difference |
| Difference − difference | Difference |
| Absolute + absolute | Rejected |
| Difference − absolute | Rejected |

```csharp
var room = AbsoluteTemperature.FromCelsius(20);
var rise = new TemperatureDifference(10); // 10 K increment = 10 °C increment
var warmer = room + rise;                // 303.15 K = 30 °C
var difference = warmer - room;          // TemperatureDifference: 10 K

// Does not compile: no operator adds two AbsoluteTemperature values.
// var invalid = room + AbsoluteTemperature.FromCelsius(10);
```

`TemperatureMeasurement` stores magnitude in kelvin and a `TemperatureKind` of
`Absolute` or `Difference`. It enforces the same table at runtime and also permits
difference + absolute. Unsupported combinations throw `InvalidOperationException`.
JSON serialization preserves kind. The two distinct compile-time types currently
provide absolute + difference, but do not provide the reversed difference + absolute
operator; use that order or the measurement envelope.

## Conversion of shifted units

For an absolute value, conversion applies both scale and offset. For an interval,
conversion applies scale only:

```text
Absolute:   kelvin = celsius + 273.15
Interval:  delta kelvin = delta celsius
Absolute:   kelvin = (fahrenheit + 459.67) × 5/9
Interval:  delta kelvin = delta fahrenheit × 5/9
```

Do not use an absolute Celsius/Fahrenheit converter to convert an interval. For
example, a 10 °C increase is 10 K, not 283.15 K.

## Indriya compatibility behavior

`JavaQuantityAdapter` deliberately follows the pinned Indriya implementation,
including its permissive absolute arithmetic. This is a compatibility convention,
not the physical arithmetic policy of the dedicated temperature types.

With **two absolute operands**, Indriya adds coherent kelvin magnitudes and converts
the result back to the left operand's unit:

```text
20 °C absolute + 10 °C absolute
= 293.15 K + 283.15 K
= 576.30 K
= 303.15 °C absolute
```

The reported `303.15 °C` is therefore not `303.15 K`. For heating by ten degrees,
the second operand must be a **relative interval**:

```csharp
var units = UnitRegistry.CreateStandard();
var celsius = units.Resolve("°C");
var room = new JavaQuantityAdapter(20, celsius); // Absolute is the default
var rise = new JavaQuantityAdapter(10, celsius, JavaQuantityScale.Relative);
var warmer = room.Add(rise); // Value ≈ 30 °C; Scale == Absolute
```

The subtraction distinction is equally important: dedicated absolute subtraction
`100 °C − 20 °C` gives an interval of 80 K. The compatibility adapter instead
returns `−193.15 °C` with absolute scale, matching Indriya's system-magnitude
subtraction and conversion back to Celsius. Use the dedicated types when you need
the physical interval policy.

Generic `Quantity` tracks dimensions but does not retain absolute-versus-difference
kind. Converting through the [temperature bridge](temperature-quantity-bridge.md)
discards that protection. Dimension compatibility alone cannot enforce these rules.

The [quantity assertion review](../conformance/quantity-interface-review.md) and
[execution evidence](../conformance/evidence/quantity-2026-10-20.md) record the pinned
Java behavior and remaining compatibility limits. Matching these examples does not
establish complete JSR 385 conformance.

Requirement IDs: SU-TEMP-006 through SU-TEMP-010. Tests: `TemperatureMeasurementTests`.

Typed arithmetic is covered by `TemperatureQuantityTests`. Adapter examples are
covered by `QuantityReferenceFixtureTests`, including `Q-absolute-add`,
`Q-mixed-add`, `Q-absolute-subtract`, and `Q-absolute-subtract-scale`.
