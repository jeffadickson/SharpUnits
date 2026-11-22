# Changelog

All notable changes to SharpUnits are documented in this file.

## [1.0.0-rc.1] - 2026-11-21

### Added
- **Core `mp-units` & `.NET INumber<T>` architecture (`SharpUnits`)**:
  - Allocation-free `long`-based `Rational` (canonical `default == 0/1`) and `RationalDimension`.
  - ISO 80000 ISQ `QuantitySpec` hierarchy (`Isq.*`) distinguishing Energy vs. Torque, Frequency vs. AngularFrequency, and supporting sub-quantity promotion (`Width + Height → Length`).
  - Algebraic `SymbolicUnit` preserving exact unit factors, superscripts, and rational exponents (`m/s²`, `nV/Hz^(1/2)`).
  - Compile-time affine `PointOrigin` and `QuantityPoint<T>` separating points (`°C`, `°F`, gauge vs. vacuum pressure) from displacements.
  - Generic `Quantity<T>` (`INumber<T>`) supporting `double`, `float`, `decimal`, `int`, `long`, plus unified `Quantity` with `IFormattable`, `ISpanFormattable`, and `IParsable<T>`.
  - Complete 130+ unit SI, IEC 80000-13 (bits/bytes/bps + binary prefixes `Ki`–`Yi`), aerospace/kinematics (`g0`, `rpm`, `kn`, `nmi`), pressure/vacuum (`inHg`, `inH2O`, `ksi`), fluid viscosity (`cP`, `cSt`), thermal (`cal`, `BTU`, `hp_m`, `ton_ref`), CGS magnetism (`G`, `Oe`, `Mx`), astronomy (`au`, `ly`, `pc`), chemistry (`M`, `Da`), radiation (`Ci`, `rem`, `R`), and imperial/USC `Units` catalog with dynamic SI + IEC prefix `UnitRegistry`, fluent C# extensions, and [docs/adding-units.md](docs/adding-units.md).
  - Complete engineering structs (`Length`, `Mass`, `Duration`, `Frequency`, `Force`, `Pressure`, `Energy`, `Power`, `Area`, `Volume`, `Velocity`, `Acceleration`, `Angle`, `Torque`, `Density`, `MagneticFluxDensity`, `DataSize`, `BitRate`) with dimensional cross-operators.
  - Hardware SIMD (`Vector<T>`) Structure-of-Arrays `QuantitySeries<T>` buffers and unit-aware `PlotAxisScaler` (linear & logarithmic Bode ticks).
  - NativeAOT-safe `System.Text.Json` converters (`options.AddSharpUnits()`, `options.AddSharpUnitsElectrical()`) supporting both structured JSON objects and compact string literals.
  - Universal serialization & frictionless value drop-down: `ValueIn` / `FromValueIn` / `FromSiValue` / `Deconstruct`, serializer-agnostic primitive DTOs (`QuantityData`, `QuantityData<T>`, `QuantityPointData<T>`, `QuantitySeriesData<T>`) for MessagePack/MemoryPack/Protobuf/YAML/XML/SQL, zero-allocation compact binary framing (`SharpUnitsBinary`), and `.NET` platform contracts (`ISpanParsable`, `IUtf8SpanFormattable`, `IUtf8SpanParsable`, `TypeConverter`).
  - Parser security hardening (`MaxInputLength` and `MaxNestingDepth`) in `UnitExpressionParser` and `FormulaParser`.
- **Layered companion packages**:
  - `SharpUnits.Tensors`: N-dimensional unit-safe `QuantityTensor<T>`, zero-copy `Tensor<T>` / `ReadOnlyTensorSpan<T>` interop, and `TensorPrimitives` SIMD kernels (`Dot`, `Norm`, `SumOfSquares`, `Distance`, `CosineSimilarity`, `MultiplyAdd`, `Clamp`, vectorized `Log10` dB/dBm/dBV).
  - `SharpUnits.Electrical`: typed electrical/RF algebra (`Voltage`, `Current`, `Resistance`, `Capacitance`, `Inductance`, `Decibels`, `PowerLevelDbm`, `VoltageLevelDbv`, AC phasors, RLC, Bode).
  - `SharpUnits.Metrology`: uncorrelated `Measurement` propagation, correlated GUM `UncertaintyBudget`, `CalibrationRecord`, and guard-banded `MeasurementDecisionRule`.
  - `SharpUnits.Formulas`: textual formula parsing, static dimensional/kind checking, exact rational dimensional inference, and `ProcedureValidator`.
  - `SharpUnits.SysMl`: Cameo / SysML QUDV JSON interchange (`QudvBridge`), FMI 2.0/3.0 `<UnitDefinitions>` XML (`FmiUnitXml`), and `CameoModelValidator`.
  - `SharpUnits.Interop`: deterministic `StarLark-cs` host values (`IThreadSafeHostValue` adapter), Teal/Lua bindings (`sharpunits.d.tl` + `LuaUnitsBridge`), and NativeAOT C-ABI `NativeUnitHandleTable`.
  - `SharpUnits.Analyzers` & `SharpUnits.CodeFixes`: Roslyn compile-time analyzer (`SU0001`–`SU0005`) with local-variable flow tracking, typo suggestions, and IDE `CodeFixProvider`s (both bundled automatically in `SharpUnits.nupkg`).

### Removed
- Removed legacy JSR-385 / Java TCK submodules, Java API adapters, reflection surface tests, and Java CI workflow.
