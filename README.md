# SharpUnits

Fast, pure-.NET 10 units library inspired by **C++ `mp-units`** (ISO 80000 ISQ quantity hierarchy, algebraic symbolic units, compile-time affine quantity points) and **.NET generic math (`INumber<T>`)** — purpose-built for **scientific plotting libraries** and **automated instrument testing**.

Zero external runtime dependencies.

## Package Architecture

| Project | Purpose |
| --- | --- |
| `SharpUnits` | Core engine: allocation-free `Rational` & `RationalDimension`, ISO 80000 `Isq` / `QuantitySpec` hierarchy, algebraic `SymbolicUnit`, affine `PointOrigin` / `QuantityPoint<T>`, `.NET INumber<T>` generic `Quantity<T>` + unified `Quantity`, full SI/engineering/imperial `Units` catalog + dynamic SI prefix `UnitRegistry`, Structure-of-Arrays `QuantitySeries<T>` buffers, `UnitAutoScaler` + `PlotAxisScaler` (linear & log decade ticks), thread-safe `UnitExpressionParser`, `QuantityTextFormat`, and strongly-typed core structs. |
| `SharpUnits.Electrical` | Strongly-typed electrical quantities (`Voltage`, `Current`, `Resistance`, `ElectricPower`, `Capacitance`, `Inductance`, `ElectricCharge`, `Conductance`, `FluxLinkage`), logarithmic RF/audio levels (`Decibels`, `PowerLevelDbm`, `VoltageLevelDbv`), complex AC impedance & phasors, `SeriesRlcCircuit`, and `BodeFrequencyResponse`. |
| `SharpUnits.Metrology` | Uncorrelated uncertainty propagation (`Measurement`), provenance-tagged `InstrumentMeasurement` + strict versioned JSON persistence, correlated GUM `UncertaintyBudget` (Cholesky PSD validation), `CalibrationRecord`, and guard-banded `MeasurementDecisionRule`. |
| `SharpUnits.Formulas` | Textual engineering formula parser (`FormulaParser`), static dimensional & semantic-kind checker (`FormulaTypeChecker`), exact rational dimensional inference (`DimensionInference`), and multi-step `ProcedureValidator`. |
| `SharpUnits.Analyzers` | Roslyn compile-time analyzer (bundled automatically in the `SharpUnits` NuGet package) validating unit/quantity string literals (`SU0001`, `SU0002`), incompatible conversions (`SU0003`), affine compound expressions (`SU0004`), and absolute affine additions (`SU0005`). |

## Highlights & Examples

### 1. `.NET INumber<T>` Generic Math + Symbolic Units + ISO 80000 ISQ

```csharp
using SharpUnits;

// Exact decimal arithmetic with zero binary floating-point drift
var sum = Quantity<decimal>.Of(0.1m, Units.Millivolt) + Quantity<decimal>.Of(0.2m, Units.Millivolt); // 0.3 mV

// Lossless integral common-unit algebra (rejects silent truncation!)
var total = Quantity<int>.Of(1, Units.Volt) + Quantity<int>.Of(500, Units.Millivolt); // 1500 mV

// Algebraic symbolic units with exact rational exponents
var accel = Units.Meter / Units.Second.Pow(2);                         // "m/s²" (ASCII: "m/s^2")
var noise = UnitRegistry.Standard.Resolve("nV") / Units.Hertz.Root(2); // "nV/Hz^(1/2)"

// ISO 80000 ISQ hierarchy: Width + Height => Length, while Energy + Torque is rejected
var halfPerimeter = Quantity<double>.Of(3, Units.Meter, Isq.Width) + Quantity<double>.Of(4, Units.Meter, Isq.Height); // Isq.Length
```

### 2. Compile-Time Affine Spaces (`QuantityPoint<T>` & `PointOrigins`)

```csharp
using SharpUnits;

// Temperature points stored without 273.15 offset drift; point + point does not compile!
var t1 = QuantityPoint<decimal>.FromCelsius(20.1m); // 20.1 °C (Origin: IcePoint)
var t2 = QuantityPoint<double>.FromFahrenheit(212); // 100 °C / 373.15 K
var delta = QuantityPoint<double>.FromCelsius(100) - QuantityPoint<double>.FromFahrenheit(32); // 100 K

// Gauge vs. Absolute pressure (plus custom instrument tare/zero origins)
var gauge = QuantityPoint<double>.FromGaugePressure(2.0, Units.Bar);
var abs   = gauge.RelativeTo(PointOrigins.Vacuum); // 3.01325 bar
```

### 3. Plotting & DAQ: Structure-of-Arrays `QuantitySeries<T>` & `PlotAxisScaler`

```csharp
using SharpUnits;

// Single shared unit metadata + contiguous buffer (ReadOnlySpan<T>) for zero-overhead plotting
var vTrace = QuantitySeries<double>.FromValues([-0.05, 0.0, 0.125, 0.25], Units.Volt);
var scaled = vTrace.AutoScale();     // "mV": [-50, 0, 125, 250]
var gpuBuf = scaled.As<float>();     // Single-pass float conversion for GPU rendering
var rms    = vTrace.Rms();           // Unit-safe RMS quantity

// Unit-aware linear & Bode logarithmic axis tick generation
var yAxis = PlotAxisScaler.CreateLinearAxis(-0.048, 0.242, Units.Volt);     // mV ticks: -50, 0, 50, ...
var fAxis = PlotAxisScaler.CreateLogarithmicAxis(10, 1_000_000, Units.Hertz); // 10 Hz .. 1 MHz + minor ticks
```

### 4. Idiomatic C# Fluent Extensions & Scripting Host Runtime (`StarLark-cs`, Teal, DSLs)

```csharp
using SharpUnits;
using SharpUnits.Electrical;

// Fluent C# style + IFormattable engineering interpolation
var v   = 250.0.Milliamperes() * 4.0.Kiloohms();      // 1000 V
var tau = 10.0.Kiloohms() * 1.0.Microfarads();        // 10 ms
var txt = $"{Quantity.Of(0.0025, Units.Volt):eng}";   // "2.5 mV"

// Deterministic, zero-reflection runtime for StarLark-cs / Teal / test-bench scripts
var env   = UnitEnvironment.Standard;
var qty   = UnitScriptOps.Multiply(250.0, env.ResolveUnit("mA"));
var gauge = env.CreatePoint(2.0, "bar", "gauge");
var abs   = env.ConvertPoint(gauge, "bar", "vacuum"); // 3.01325 bar
```

## Build & Test

```bash
dotnet test SharpUnits.sln -c Release
```

## Integration Guides

- [Supporting Units in StarLark-cs, Teal, and Cameo](docs/ecosystem-integration.md) — host value design (`IThreadSafeHostValue`), Teal `.d.tl` typed metamethods, and Cameo SysML QUDV / FMI interchange.

