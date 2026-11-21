# Supporting Units in StarLark-cs, Teal, and Cameo

This document describes how to integrate **SharpUnits** into three distinct downstream environments while keeping all dimensional algebra, ISO 80000 ISQ semantics, affine-space safety, and unit conversion math in a single authoritative core (`SharpUnits`).

---

## 1. Shared Foundation in `SharpUnits`

All three integrations build on the same deterministic, zero-reflection core primitives already shipped in `SharpUnits`:

| SharpUnits Primitive | Role Across Hosts |
| --- | --- |
| [`UnitEnvironment`](../src/SharpUnits/UnitEnvironment.cs) | Culture-invariant (`InvariantCulture`), thread-safe unit/origin resolution, parsing, conversion, and formatting. Safe for hermetic/deterministic runtimes. |
| [`UnitScriptOps`](../src/SharpUnits/UnitEnvironment.cs) | Deterministic operator dispatch (`Add`, `Subtract`, `Multiply`, `Divide`, `Negate`, `Compare`) across `Quantity<double>`, `QuantityPoint<double>`, `SymbolicUnit`, and `double`. |
| [`QuantitySpec` / `Isq`](../src/SharpUnits/QuantitySpec.cs) | ISO 80000 ISQ quantity hierarchy (`Width + Height → Length`; `Energy + Torque` rejected; `Hz + rad/s` rejected). |
| [`SymbolicUnit`](../src/SharpUnits/SymbolicUnit.cs) | Algebraic unit expressions preserving structure and exact rational exponents (`m/s²`, `mA·kΩ`, `nV/Hz^(1/2)`). |
| [`PointOrigin` / `QuantityPoint<T>`](../src/SharpUnits/PointOrigin.cs) | Affine-space points (`°C`, `°F`, `bar` gauge vs. vacuum, custom tare offsets) separating points from displacements. |
| [`QuantitySeries<T>`](../src/SharpUnits/QuantitySeries.cs) | Structure-of-Arrays SIMD-accelerated buffers (`ReadOnlySpan<T>`) for waveforms, sweeps, and plotting without per-sample metadata overhead. |

**Core design rule:** Host bindings should be **thin adapters** (typically 200–400 LOC per host) that translate host values/operators into `UnitEnvironment` and `UnitScriptOps` calls. Never duplicate prefix tables, conversion factors, or affine rules in the host language.

---

## 2. `StarLark-cs` Integration (`SharpUnits.Starlark`)

`jeffadickson/StarLark-cs` provides first-class host value hooks in `Starlark.Eval` (`IHostValue`, `IThreadSafeHostValue`, `Interpreter.DefineValue`, and `Interpreter.Register`). Because every SharpUnits core type (`SymbolicUnit`, `Quantity<double>`, `QuantityPoint<double>`, `QuantitySeries<double>`) is **immutable**, it naturally satisfies `IThreadSafeHostValue` and can cross Starlark `parallel_map` / `spawn` futures safely.

### 2.1 Recommended Package & Types

Create a dedicated bridge assembly **`SharpUnits.Starlark`** referencing `SharpUnits` and `Starlark.Eval` with four `IThreadSafeHostValue` wrappers:

| Starlark `TypeName` | Wraps | Key Attributes & Methods |
| --- | --- | --- |
| `"unit"` | `SymbolicUnit` | `.symbol`, `.ascii`, `.scale_to_si`, `.dimension`, `.pow(n)`, `.root(n)`, `.to_si()`, `*`, `/` |
| `"quantity"` | `Quantity<double>` | `.value`, `.si_value`, `.unit`, `.spec`, `.to(unit)`, `.to_si()`, `.format(fmt)`, `+`, `-`, `*`, `/`, `<`, `==` |
| `"quantity_point"` | `QuantityPoint<double>` | `.value`, `.unit`, `.origin`, `.to(unit)`, `.relative_to(origin)`, `.to_c()`, `.to_f()`, `.to_k()`, `+ delta`, `- point`, `- delta`, `<`, `==` |
| `"quantity_series"` | `QuantitySeries<double>` | `.unit`, `.count`, `.to(unit)`, `.auto_scale()`, `.min()`, `.max()`, `.mean()`, `.rms()`, `.peak_to_peak()`, `len()`, `s[i]`, `for x in s:` |

### 2.2 Critical `StarLark-cs` Details to Handle

1. **Starlark `int` is `BigInteger`, `float` is `double`:**
   In `Starlark.Eval/Ops.cs`, numeric values passed into `IHostValue.TryBinary(BinOp op, object? other, bool reflected, out object? result)` are boxed as either `BigInteger` or `double` (never CLR `int`).
   - Normalize numeric operands via `Ops.BigToDouble(BigInteger)` (preserving Starlark's exact ties-to-even rounding) before delegating to `UnitScriptOps`.
   - Handle `reflected: true` on `StarUnitValue.TryBinary` so natural expressions like `250 * mA` or `10 * kOhm` work identically to `qty(250, "mA")`.

2. **Operator Dispatch Mapping (`TryBinary` & `TryNegate`):**
   - `BinOp.Add` → `UnitScriptOps.Add(lhs, rhs)` (respecting `reflected`)
   - `BinOp.Subtract` → `UnitScriptOps.Subtract(lhs, rhs)` (remember operand order when `reflected == true`!)
   - `BinOp.Multiply` → `UnitScriptOps.Multiply(lhs, rhs)`
   - `BinOp.Divide` → `UnitScriptOps.Divide(lhs, rhs)`
   - `BinOp.Power` → `SymbolicUnit.Pow(...)` / `Quantity.Pow(...)`
   - Catch `InvalidOperationException`, `FormatException`, `KeyNotFoundException`, and `DivideByZeroException` and wrap them into `StarlarkException` so scripts receive clean, catchable/diagnosable Starlark runtime errors rather than unhandled CLR exceptions.

3. **Equality, Hashing, and Freezing:**
   - `EqualsValue` on `StarQuantityValue`: compare physical equivalence via `a.IsEquivalentTo(b)` (or exact unit+value equality if strict dict keys are desired — document the choice; for `HashValue`, normalize to coherent SI value + `Dimension` + `Spec` so `1000 * mV == 1 * V` hashes consistently!).
   - `Freeze()`: no-op because SharpUnits values are deeply immutable.
   - Implement `IThreadSafeHostValue` so quantities and `QuantitySeries` can be shared across `spawn` and `parallel_map` tasks.

4. **Hermeticity, Budgets, and Sandboxing:**
   - Expose a single `units` module via `Interpreter.DefineValue("units", new StarUnitsModule(UnitEnvironment.CreateStandard()))` so each interpreter instance is isolated (custom units/origins registered in one script cannot leak into another).
   - Before allocating a new `QuantitySeries<double>` from a Starlark list (`units.series(list, "mV")`), charge `ExecutionContext.Current` / `MemoryBudget` proportional to `count * sizeof(double)` so scripts cannot bypass `StarLark-cs`'s per-allocation memory cap.

5. **Static Type Checker Integration (`Starlark.Eval/TypeCheck`):**
   - Register `unit`, `quantity`, `quantity_point`, and `quantity_series` host types and signatures (`qty(value: float | int, unit: str) -> quantity`, `point(value: float | int, unit: str, origin: str = ...) -> quantity_point`) so `StarLark-cs`'s static type checker validates script calls and catches `point + point` or wrong argument types before execution.

### 2.3 Target Starlark Script Ergonomics

```python
# In a StarLark-cs test-bench or plotting script:
load("@units", "qty", "unit", "point", "series")

v = 250 * unit("mA") * (4 * unit("kOhm"))
v_kv = v.to("kV")                            # 1 kV

t_rise = point(100, "°C") - point(32, "°F")  # 180 °R (100 K)
p_abs  = point(2.0, "bar", "gauge").relative_to("vacuum")  # 3.01325 bar

trace = series([-0.05, 0.0, 0.125, 0.25], "V").auto_scale() # mV
rms_mv = trace.rms()
```

---

## 3. Teal (Typed Lua) Integration

**Teal** (`tl`) compiles statically typed `.tl` files to standard Lua (5.3/5.4/LuaJIT) and supports typed `record` declarations, generics, and **typed metamethods** (`metamethod __add`, `__sub`, `__mul`, `__div`, `__unm`, `__lt`, `__le`, `__eq`, `__tostring`). This lets Teal enforce affine point-vs-displacement safety at **Teal compile time**, while delegating unit math at runtime to SharpUnits.

### 3.1 Architecture: `.d.tl` Type Declarations + Lua Runtime Binding

There are two ways a Lua/Teal host executes SharpUnits depending on where Lua runs:
- **In-process .NET host** (NLua / KeraLua / MoonSharp inside a C# app): expose a thin `LuaUnitsBridge` wrapping `UnitEnvironment` + `UnitScriptOps` as userdata metatables.
- **Standalone Lua / Teal CLI or embedded C host:** export a small **NativeAOT C ABI** (`SharpUnits.Native` with `[UnmanagedCallersOnly]` functions taking opaque handles to `UnitEnvironment`, `Quantity`, `QuantityPoint`, `SymbolicUnit`, and `QuantitySeries`) loaded via Lua C module (`require("sharpunits")`) or LuaJIT FFI.

In both cases, Teal scripts compile against one shared **`sharpunits.d.tl`** declaration file.

### 3.2 Compile-Time Safe Teal API (`sharpunits.d.tl`)

Notice how Teal's metamethod typing enforces `mp-units` affine rules statically:

```lua
-- sharpunits.d.tl
global record SharpUnits
    record SymbolicUnit
        symbol: string
        ascii_symbol: string
        scale_to_si: number
        pow: function(SymbolicUnit, integer): SymbolicUnit
        root: function(SymbolicUnit, integer): SymbolicUnit
        to_si: function(SymbolicUnit): SymbolicUnit

        metamethod __mul: function(SymbolicUnit, SymbolicUnit): SymbolicUnit
        metamethod __div: function(SymbolicUnit, SymbolicUnit): SymbolicUnit
        metamethod __tostring: function(SymbolicUnit): string
    end

    record Quantity
        value: number
        si_value: number
        unit: SymbolicUnit
        spec: string

        in_unit: function(Quantity, string | SymbolicUnit): Quantity
        to_si: function(Quantity): Quantity
        format: function(Quantity, string): string
        is_equivalent: function(Quantity, Quantity, number): boolean

        metamethod __add: function(Quantity, Quantity): Quantity
        metamethod __sub: function(Quantity, Quantity): Quantity
        metamethod __unm: function(Quantity): Quantity
        metamethod __mul: function(Quantity, Quantity | number): Quantity
        metamethod __div: function(Quantity, Quantity | number): Quantity
        metamethod __lt: function(Quantity, Quantity): boolean
        metamethod __le: function(Quantity, Quantity): boolean
        metamethod __eq: function(Quantity, Quantity): boolean
        metamethod __tostring: function(Quantity): string
    end

    record QuantityPoint
        value: number
        unit: SymbolicUnit
        origin: string

        in_unit: function(QuantityPoint, string | SymbolicUnit): QuantityPoint
        relative_to: function(QuantityPoint, string): QuantityPoint
        to_celsius: function(QuantityPoint): QuantityPoint
        to_fahrenheit: function(QuantityPoint): QuantityPoint
        to_kelvin: function(QuantityPoint): QuantityPoint

        -- Compile-time affine safety: Point + Point does NOT exist!
        metamethod __add: function(QuantityPoint, Quantity): QuantityPoint
        metamethod __sub: function(QuantityPoint, QuantityPoint): Quantity
        metamethod __lt: function(QuantityPoint, QuantityPoint): boolean
        metamethod __le: function(QuantityPoint, QuantityPoint): boolean
        metamethod __eq: function(QuantityPoint, QuantityPoint): boolean
        metamethod __tostring: function(QuantityPoint): string
    end

    record QuantitySeries
        count: integer
        unit: SymbolicUnit
        in_unit: function(QuantitySeries, string | SymbolicUnit): QuantitySeries
        auto_scale: function(QuantitySeries): QuantitySeries
        min: function(QuantitySeries): Quantity
        max: function(QuantitySeries): Quantity
        mean: function(QuantitySeries): Quantity
        rms: function(QuantitySeries): Quantity
        at: function(QuantitySeries, integer): Quantity
        metamethod __len: function(QuantitySeries): integer
    end

    unit: function(expr: string): SymbolicUnit
    qty: function(value: number, unit_expr: string): Quantity
    parse: function(literal: string): Quantity
    point: function(value: number, unit_expr: string, origin?: string): QuantityPoint
    series: function(values: {number}, unit_expr: string): QuantitySeries
end

return SharpUnits
```

### 3.3 Key Teal/Lua Implementation Considerations

1. **Lua 1-based indexing vs. .NET 0-based indexing:** `QuantitySeries:at(i)` and `__index` must translate Lua index `1..n` to C# `0..(n-1)`.
2. **Metamethod overloading on `QuantityPoint.__sub`:** Teal supports union/overloaded metamethods or explicit `shift(-delta)` if `QuantityPoint - Quantity` vs. `QuantityPoint - QuantityPoint` needs disambiguation under strict `tl check`. Providing `point:shift(delta: Quantity): QuantityPoint` alongside `point + delta` keeps Teal type inference unambiguous.
3. **Optional Phantom-Dimension Generics:** For high-assurance Teal scripts, you can optionally expose thin typed aliases (`record Voltage`, `record Current`, `record Resistance`, `record Frequency`) mirroring `SharpUnits.Electrical` so `Voltage / Resistance` type-checks as `Current` inside `tl check` before runtime!
4. **Error Propagation:** Convert `UnitEnvironment` / `UnitScriptOps` exceptions into Lua `lua_error` with clear messages containing unit symbols and ISQ kind names.
5. **Memory Lifetime (NativeAOT):** If using the NativeAOT C ABI, attach a Lua `__gc` metamethod on every userdata handle to release pinned handles (`GCHandle.Free()`), especially for large `QuantitySeries` buffers.

---

## 4. Cameo Systems Modeler / MagicDraw (SysML QUDV & Simulation) Integration

**Cameo Systems Modeler** (Dassault Systèmes / No Magic) models physical units using **SysML QUDV** (Quantities, Units, Dimensions, and Values — Annex E of SysML 1.x) and ISO 80000 ISQ (also standardized directly in **SysML v2**). SharpUnits was explicitly architected around the same ISO 80000 foundations, so the mapping is unusually direct.

### 4.1 Concept Mapping: SysML QUDV ↔ SharpUnits

| Cameo / SysML QUDV Element | SharpUnits Type | Notes |
| --- | --- | --- |
| `SimpleQuantityKind` / `DerivedQuantityKind` + `QuantityKindFactor` | [`QuantitySpec`](../src/SharpUnits/QuantitySpec.cs) (`Isq.*`) + [`RationalDimension`](../src/SharpUnits/RationalDimension.cs) | `QuantitySpec.Parent` + `IsKind` models QUDV `general` / distinct kinds (`Energy` vs. `Torque`, `Frequency` vs. `AngularFrequency`). |
| `SimpleUnit` | [`UnitDefinition`](../src/SharpUnits/UnitConversion.cs) (`Converter = Identity`) | Coherent SI base/named units (`m`, `kg`, `s`, `A`, `K`, `V`, `Ω`, `Hz`). |
| `LinearConversionUnit` (`factor`) | `UnitDefinition` (`UnitConversion.Linear(factor)`) | `in`, `ft`, `bar`, `psi`, `lb`, etc. |
| `AffineConversionUnit` (`factor`, `offset`) | `UnitDefinition` (`UnitConversion(factor, offset)`) + [`PointOrigin`](../src/SharpUnits/PointOrigin.cs) / [`QuantityPoint<T>`](../src/SharpUnits/GenericQuantity.cs) | Note sign convention! Verify Cameo's `y = factor * x + offset` vs. `x_si = factor * x + offset` during import/export. |
| `PrefixedUnit` (`prefix`, `unit`) | [`SiPrefix`](../src/SharpUnits/SiPrefix.cs) + `UnitRegistry` dynamic prefix resolution | Maps all 24 BIPM SI prefixes. |
| `DerivedUnit` (`unitFactors`) | [`SymbolicUnit`](../src/SharpUnits/SymbolicUnit.cs) (`UnitFactor[]`) | Preserves exact symbolic unit factors and rational exponents. |
| `ValueType` (`unit`, `quantityKind`, primitive type) | `Quantity<T>` / `QuantityPoint<T>` | Maps SysML `Real` → `double`, `Integer` → `long`/`int`, exact metrology → `decimal`. |
| `ConstraintBlock` (Parametric equations) | [`SharpUnits.Formulas`](../src/SharpUnits.Formulas/FormulaParser.cs) (`FormulaAnalyzer`, `ProcedureValidator`) | Validates dimensional & ISQ consistency of Cameo parametric equations statically. |
| Requirement limits / Verification (`satisfy` / `verify`) | [`SharpUnits.Metrology`](../src/SharpUnits.Metrology/CalibrationDecision.cs) (`QuantityAcceptanceLimit`, `MeasurementDecisionRule`, `UncertaintyBudget`) | Evaluates instrument test data against Cameo requirement bounds with guard bands and calibration provenance. |

### 4.2 Integration Patterns for Cameo

Because Cameo runs on the JVM (Java plugins / Cameo Simulation Toolkit) while SharpUnits runs on .NET 10, there are **three clean integration boundaries** depending on your workflow:

#### Pattern A — Model-Driven Catalog & Requirement Interchange (Recommended Starting Point)
Create a small **`SharpUnits.SysMl`** (or `SharpUnits.Cameo`) library that imports/exports a versioned JSON/XML interchange schema between Cameo and .NET test/plotting tools:
1. **Export from Cameo** (via Cameo Open API / Velocity report / SysML v2 REST API / XMI extractor):
   - QUDV catalog (`quantityKinds`, `units`, `valueTypes`)
   - Parametric constraint equations (`constraintBlocks`)
   - Requirement acceptance limits (`min`, `max`, `unit`, `quantityKind`, `guardBand`)
2. **Load into SharpUnits** (`UnitEnvironment` + `QuantityKindRegistry`):
   - Register project-specific QUDV `UnitDefinition`s, `QuantitySpec`s, and `PointOrigin`s into a dedicated `UnitEnvironment`.
   - Run [`ProcedureValidator.Validate`](../src/SharpUnits.Formulas/ProcedureValidator.cs) on exported parametric equations to catch dimensional/ISQ bugs in CI before hardware-in-the-loop (HIL) tests run.
   - Run automated instrument tests (`SharpTestBench` / `SharpUnits.Metrology`) against the Cameo-defined `QuantityAcceptanceLimit`s and emit versioned [`InstrumentMeasurementJson`](../src/SharpUnits.Metrology/InstrumentMeasurement.cs) verification evidence back into Cameo!

#### Pattern B — Co-Simulation / FMI & Cameo Simulation Toolkit (CST)
- Export unit-aware C# simulation or test-bench models as an **FMI 2.0/3.0 FMU** (using .NET NativeAOT) whose `modelDescription.xml` `<UnitDefinitions>` and `<TypeDefinitions>` are generated directly from SharpUnits `UnitDefinition` (`Scale`, `Offset`, `RationalDimension` SI base exponents — FMI's `<BaseUnit m="..." kg="..." s="..." A="..." K="..." mol="..." cd="..." factor="..." offset="..."/>` matches `UnitDefinition` **field-for-field**!).
- Cameo Simulation Toolkit imports the FMU natively and enforces unit consistency on every port.

#### Pattern C — Direct SysML v2 / REST Service Bridge
- Wrap `UnitEnvironment`, `ProcedureValidator`, and `MeasurementDecisionRule` in a lightweight local HTTP/gRPC service (or CLI tool) invoked from Cameo custom validation rules or CST scripts to validate equations, convert units, and generate Bode/waveform plots with `PlotAxisScaler`.

### 4.3 Subtle Cameo QUDV Edge Cases to Watch

1. **Affine Offset Sign Convention:** Always verify whether a QUDV `AffineConversionUnit` stores offset in target-SI units (`T_K = a * T_unit + b`, matching `UnitConversion`) or source units (`T_K = a * (T_unit + b_src)`), and normalize explicitly at the import boundary.
2. **Temperature Points vs. Intervals:** Cameo models often use `degC` for both ambient temperature (`25 °C`, point) and temperature rise (`Δ10 °C`, interval). Map absolute `ValueProperty` slots to `QuantityPoint<T>` (`PointOrigins.IcePoint`) and tolerance/delta properties to linear `Quantity<T>` (`Units.Kelvin`, `Isq.TemperatureInterval`).
3. **Angular Units & Frequency:** Cameo's ISO 80000 library distinguishes `rad/s` (`AngularFrequency`) from `Hz` (`Frequency`) and `N·m` (`Torque`) from `J` (`Energy`). Map QUDV `QuantityKind` URIs directly to `Isq.AngularFrequency`, `Isq.Frequency`, `Isq.Torque`, and `Isq.Energy` so `2π` and torque/energy mix-ups are caught automatically.
4. **Rational Dimensions:** SysML 1.x QUDV `QuantityKindFactor.exponent` is typed as `Rational` (numerator/denominator), which maps losslessly to `SharpUnits.Rational` — no integer truncation needed for quantities like noise spectral density (`V/Hz^(1/2)`).

---

## 5. Recommended Rollout Order

| Phase | Target | Deliverable | Effort |
| --- | --- | --- | --- |
| **1** | `StarLark-cs` | New `SharpUnits.Starlark` project implementing `IThreadSafeHostValue` wrappers over `UnitEnvironment` & `UnitScriptOps` + `StarLark-cs` `TypeChecker` registration + unit tests. | Small (~300 LOC) |
| **2** | Teal | `sharpunits.d.tl` typed declaration file + Lua metatable bridge (in-process .NET or NativeAOT C ABI) + `tl check` test suite verifying compile-time affine rejection. | Small–Medium (~350 LOC) |
| **3** | Cameo | `SharpUnits.SysMl` QUDV JSON/XMI + FMI `<UnitDefinitions>` interchange library wiring Cameo `ValueType`s, `ConstraintBlock`s (`SharpUnits.Formulas`), and requirement verification (`SharpUnits.Metrology`). | Medium (~450 LOC) |
