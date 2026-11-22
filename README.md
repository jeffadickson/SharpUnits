# SharpUnits

Fast, pure-.NET units library (targeting `.NET 10`, seamlessly consumable from `.NET 11+`, with full NativeAOT & trimming compatibility) inspired by **C++ `mp-units`** (ISO 80000 ISQ quantity hierarchy, algebraic symbolic units, compile-time affine quantity points) and **.NET generic math (`INumber<T>`)** — purpose-built for **scientific plotting libraries**, **automated instrument testing**, and **scripting / MBSE hosts**.

Zero external runtime dependencies.

## Package Architecture

| Project | Purpose |
| --- | --- |
| `SharpUnits` | Core engine (`IsAotCompatible` + `IsTrimmable`, 0 external deps): allocation-free `Rational` & `RationalDimension`, ISO 80000 `Isq` / `QuantitySpec` hierarchy, algebraic `SymbolicUnit`, affine `PointOrigin` / `QuantityPoint<T>`, `.NET INumber<T>` generic `Quantity<T>` + unified `Quantity` (`IFormattable`, `ISpanFormattable`, `IUtf8SpanFormattable`, `ISpanParsable<T>`, `IUtf8SpanParsable<T>`, `TypeConverter`), full SI/engineering/imperial `Units` catalog + dynamic SI prefix `UnitRegistry`, SIMD-accelerated Structure-of-Arrays `QuantitySeries<T>`, `UnitAutoScaler` + `PlotAxisScaler`, security-hardened `UnitExpressionParser`, NativeAOT-safe `System.Text.Json` converters (`AddSharpUnits`), serializer-agnostic primitive DTOs (`QuantityData<T>`, `QuantitySeriesData<T>`), compact binary framing (`SharpUnitsBinary`), frictionless value drop-down (`ValueIn` / `Deconstruct`), and 18 strongly-typed engineering structs. |
| `SharpUnits.Tensors` | N-dimensional unit-safe `QuantityTensor<T>`, zero-copy `Tensor<T>` / `ReadOnlyTensorSpan<T>` interop, and `TensorPrimitives` SIMD kernels (`Dot` with unit multiplication `V·A → W`, `Norm`, `SumOfSquares`, `Distance`, `CosineSimilarity`, unit-checked FMA `MultiplyAdd`, `Clamp`, vectorized `Log10` dB/dBm/dBV). |
| `SharpUnits.Electrical` | Strongly-typed electrical quantities (`Voltage`, `Current`, `Resistance`, `ElectricPower`, `Capacitance`, `Inductance`, `ElectricCharge`, `Conductance`, `FluxLinkage`), logarithmic RF/audio levels (`Decibels`, `PowerLevelDbm`, `VoltageLevelDbv`), complex AC impedance & phasors, `SeriesRlcCircuit`, `BodeFrequencyResponse`, and NativeAOT-safe JSON converters (`AddSharpUnitsElectrical`). |
| `SharpUnits.Metrology` | Uncorrelated uncertainty propagation (`Measurement`), provenance-tagged `InstrumentMeasurement` + strict versioned JSON persistence, correlated GUM `UncertaintyBudget` (Cholesky PSD validation), `CalibrationRecord`, and guard-banded `MeasurementDecisionRule`. |
| `SharpUnits.Formulas` | Security-hardened textual engineering formula parser (`FormulaParser`), static dimensional & semantic-kind checker (`FormulaTypeChecker`), exact rational dimensional inference (`DimensionInference`), and multi-step `ProcedureValidator`. |
| `SharpUnits.SysMl` | Cameo Systems Modeler / MagicDraw SysML QUDV JSON interchange (`QudvBridge`), FMI 2.0/3.0 `<UnitDefinitions>` XML export/import (`FmiUnitXml`), parametric `ConstraintBlock` validation, and requirement verification (`CameoModelValidator`). |
| `SharpUnits.Interop` | Deterministic scripting & FFI bridges for `StarLark-cs` (`StarUnitsModule` + `bindings/starlark-cs/SharpUnitsStarlarkAdapter.cs`), Teal/Lua (`LuaUnitsBridge` + `bindings/teal/sharpunits.d.tl`), and NativeAOT C ABI (`NativeUnitHandleTable`). |
| `SharpUnits.Analyzers` & `SharpUnits.CodeFixes` | Roslyn compile-time analyzers & IDE quick-fixes (both bundled automatically in `SharpUnits.nupkg`) validating unit/quantity string literals with typo suggestions (`SU0001`, `SU0002`), local-variable flow-tracked incompatible conversions (`SU0003`), affine compound expressions (`SU0004`), and absolute affine additions with one-click rewrite to `Quantity.Delta(...)` (`SU0005`). |

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

### 3. Plotting & DAQ: SIMD `QuantitySeries<T>` & `PlotAxisScaler`

```csharp
using SharpUnits;

// Single shared unit metadata + contiguous buffer (ReadOnlySpan<T>) for zero-overhead plotting
var vTrace = QuantitySeries<double>.FromValues([-0.05, 0.0, 0.125, 0.25], Units.Volt);
var scaled = vTrace.AutoScale();     // "mV": [-50, 0, 125, 250]
var gpuBuf = scaled.As<float>();     // Single-pass float conversion for GPU rendering
var rms    = vTrace.Rms();           // SIMD-accelerated unit-safe RMS quantity

// Zero-allocation SIMD conversion directly into a reusable staging buffer
Span<double> plotBuffer = stackalloc double[vTrace.Count];
vTrace.CopyConvertedTo(Units.Millivolt, plotBuffer);

// Unit-aware linear & Bode logarithmic axis tick generation
var yAxis = PlotAxisScaler.CreateLinearAxis(-0.048, 0.242, Units.Volt);     // mV ticks: -50, 0, 50, ...
var fAxis = PlotAxisScaler.CreateLogarithmicAxis(10, 1_000_000, Units.Hertz); // 10 Hz .. 1 MHz + minor ticks
```

### 4. Universal Serialization, Binary Framing & Frictionless Value Drop-Down

```csharp
using System.Text.Json;
using SharpUnits;
using SharpUnits.Electrical;

// 1) Frictionless drop to raw primitives & back (explicit unit — never ambiguous!)
var q = Quantity<double>.Of(12.5, Units.Millivolt);
double rawVolts = q.ValueIn(Units.Volt);                 // 0.0125
var (mag, unit) = q;                                     // (12.5, "mV")
var restored    = Quantity<double>.FromValueIn(rawVolts, Units.Volt).In(Units.Millivolt);

// 2) Serializer-agnostic primitive DTOs (MessagePack, MemoryPack, Protobuf, YAML, XML, SQL)
QuantityData<double> dto = q.ToData();                   // { Value: 12.5, Unit: "mV", Spec: "isq.voltage" }
var fromDto = Quantity<double>.FromData(dto);

// 3) Compact binary framing over IBufferWriter<byte> (~14-byte header for 1M-sample waveforms!)
byte[] frame = SharpUnitsBinary.ToBytes(vTrace);
var traceBack = SharpUnitsBinary.ReadQuantitySeries<double>(frame, out _);

// 4) NativeAOT-safe System.Text.Json (Structured or CompactString) + TypeConverter / UTF-8 Span
var options = new JsonSerializerOptions().AddSharpUnits().AddSharpUnitsElectrical();
string qJson = JsonSerializer.Serialize(Quantity.Of(12.5, Units.Millivolt), options); // {"value":12.5,"unit":"mV"}
```

### 5. N-Dimensional Tensors & `TensorPrimitives` (`SharpUnits.Tensors`)

```csharp
using System.Numerics.Tensors;
using SharpUnits;
using SharpUnits.Tensors;

// Wrap 4-channel oscilloscope capture [4, 1_000_000] or thermal camera frame [480, 640]
var scope = QuantityTensor<double>.FromValues(rawSamples, [4, 1_000_000], Units.Volt);
var ch0Mv = scope.SliceLeadingAxis(0).In(Units.Millivolt);  // SIMD unit conversion
ReadOnlyTensorSpan<double> zeroCopy = scope.AsReadOnlyTensorSpan(); // Direct to ONNX / ML.NET / GPU

// TensorPrimitives SIMD engineering kernels with full dimensional safety
var power = QuantityTensorMath.Dot(vSeries, iSeries);       // V · A => Watts!
var dbm   = QuantityTensorMath.ConvertToDbm(wattsSeries);   // Vectorized 10 * log10(W / 1 mW)
```

### 6. Idiomatic C# Fluent Extensions, Cross-Operators & Scripting Host Runtime

```csharp
using SharpUnits;
using SharpUnits.Electrical;

// Fluent C# style + dimensional cross-operators + zero-allocation ISpanFormattable
var v    = 250.0.Milliamperes() * 4.0.Kiloohms();      // 1000 V
var tau  = 10.0.Kiloohms() * 1.0.Microfarads();        // 10 ms
var area = Length.FromMeters(3) * Length.FromMeters(4); // 12 m² (Area)
var spd  = Length.FromMeters(100) / Duration.FromSeconds(10); // 10 m/s (Velocity)
var txt  = $"{Quantity.Of(0.0025, Units.Volt):eng}";   // "2.5 mV"

// Deterministic, zero-reflection runtime for StarLark-cs / Teal / test-bench scripts
var env   = UnitEnvironment.Standard;
var qty   = UnitScriptOps.Multiply(250.0, env.ResolveUnit("mA"));
var gauge = env.CreatePoint(2.0, "bar", "gauge");
var abs   = env.ConvertPoint(gauge, "bar", "vacuum"); // 3.01325 bar
```

## Measured Performance (`benchmarks/SharpUnits.Benchmarks`)

Measured on `.NET 10.0.15` (`Vector<double>.Count = 4`, hardware SIMD enabled):

| Benchmark | Result | Notes |
| --- | --- | --- |
| `QuantitySeries<double>.Rms()` (1M samples, SIMD) | **0.640 ms/op** | **2.83× faster** than raw scalar `double[]` RMS (1.812 ms/op) |
| `QuantitySeries<double>.CopyConvertedTo(mV)` (1M samples, SIMD) | **1.258 ms/op** | **1.23× faster** than raw scalar `double[]` scale (1.544 ms/op), zero GC allocation |
| `Quantity.TryFormat` (`ISpanFormattable`, 100k ticks) | **0 bytes allocated** | 800,000 chars formatted with zero heap allocations |
| `System.Text.Json` `QuantitySeries<double>` round-trip (10k pts) | **7.331 ms/op** | NativeAOT-safe SoA JSON serialize + deserialize |

Run locally with:

```bash
dotnet run --project benchmarks/SharpUnits.Benchmarks/SharpUnits.Benchmarks.csproj -c Release
```

## Build & Test

```bash
dotnet test SharpUnits.sln -c Release
```

## Integration Guides & Release Docs

- [Guide to Adding Units in SharpUnits](docs/adding-units.md) — runtime custom units, distinct ISQ kinds, custom affine origins, IEC binary prefixes, QUDV import, and contributing built-in catalog units.
- [Supporting Units in StarLark-cs, Teal, and Cameo](docs/ecosystem-integration.md) — host value design (`IThreadSafeHostValue`), Teal `.d.tl` typed metamethods, and Cameo SysML QUDV / FMI interchange.
- [Changelog](CHANGELOG.md) | [MIT License](LICENSE)

