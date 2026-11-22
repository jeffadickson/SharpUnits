# Guide to Adding Units in SharpUnits

SharpUnits separates **defining a unit at runtime** (for your own application, instrument bench, StarLark/Teal script, or Cameo QUDV model) from **contributing a built-in unit** to the core SharpUnits catalog. Both workflows are designed to be declarative, zero-reflection, and dimensionally verified.

---

## 1. Adding Custom Units at Runtime (Application & Scripting Hosts)

You never have to fork SharpUnits to add domain-specific units (for example, `sccm` flow rate, `dwt` pennyweight, PCB copper weight `oz_cu`, or a custom sensor scale).

### A. Register a Linear Unit in an Isolated `UnitEnvironment`

```csharp
using SharpUnits;

// Create a fresh environment (never mutates the global UnitRegistry.Standard)
var env = UnitEnvironment.CreateStandard();

// Define a custom linear unit: 1 sccm = (1e-6 / 60) m³/s (standard cubic cm per minute)
var sccm = new UnitDefinition(
    symbol: "sccm",
    dimension: Dimension.VolumetricFlowRate,
    converter: UnitConversion.Linear(1e-6d / 60d),
    name: "standard cubic centimeter per minute",
    allowPrefixes: true); // automatically unlocks ksccm, msccm, etc.

env.Registry.Register(sccm);
env.Registry.RegisterAlias("SCCM", sccm);

// Now expressions, parsing, StarLark-cs, Teal, and JSON converters all recognize it!
var flow = env.CreateQuantity(500, "sccm");
var si   = flow.In(Units.Liter / Units.Minute); // 0.5 L/min
```

### B. Compose Units Without Registering Anything (`SymbolicUnit`)

Most derived units do not need to be registered at all — SharpUnits composes any existing units algebraically with exact rational exponents:

```csharp
using SharpUnits;

// Spectral noise density, thermal conductivity, specific impulse, etc.
var noiseDensity = UnitRegistry.Standard.Resolve("nV") / Units.Hertz.Root(2); // "nV/Hz^(1/2)"
var thermalCond  = Units.Watt / (Units.Meter * Units.Kelvin);                 // "W/(m·K)"
var fuelEconomy  = Units.Liter / (SiPrefixes.Apply("k", Units.Meter));        // "L/km"

var measured = Quantity<double>.Of(4.2, noiseDensity);
```

### C. Add a Distinct ISO 80000 `QuantitySpec` Kind

When two quantities share identical SI dimensions but must **never** be added implicitly (like `Energy` vs. `Torque`, `Frequency` vs. `Activity`, or `FuelConsumption` vs. `Area`), define a `QuantitySpec` with `isKind: true`:

```csharp
using SharpUnits;

var fuelConsumption = new QuantitySpec(
    id: "custom.fuel_consumption",
    name: "fuel consumption",
    dimension: RationalDimension.Volume - RationalDimension.LengthDimension, // L² (same as Area!)
    isKind: true);

var lPer100Km = new UnitDefinition(
    symbol: "L/100km",
    dimension: fuelConsumption.Dimension,
    converter: UnitConversion.Linear(1e-3d / 1e5d),
    name: "liter per 100 kilometers",
    defaultSpec: fuelConsumption);

// Mixing fuel consumption with Area now throws InvalidOperationException!
```

### D. Add a Custom Affine Scale or Instrument Zero (`PointOrigin`)

For shifted zero references (gauge pressure, instrument tare, room-temperature delta, altitude above runway), use `PointOrigin` + `QuantityPoint<T>`:

```csharp
using SharpUnits;

// Define a custom instrument tare origin at 12.5 mV above 0 V
var dacZero = PointOrigin.RelativeTo(
    PointOrigins.NaturalZero(RationalDimension.Voltage, Isq.Voltage),
    offsetSi: 0.0125d,
    id: "bench.dac_zero",
    name: "DAC tare zero (12.5 mV)");

var env = UnitEnvironment.CreateStandard();
env.RegisterOrigin("dac_zero", dacZero);

var reading = QuantityPoint<double>.At(5.0, Units.Millivolt, dacZero);
var trueVoltage = reading.RelativeTo(PointOrigins.NaturalZero(RationalDimension.Voltage, Isq.Voltage)); // 17.5 mV
```

### E. Enable IEC Binary Prefixes (`Ki`..`Yi`)

For information/memory/rate units that support both SI decimal prefixes (`k`, `M`, `G`) and IEC 80000-13 binary prefixes (`Ki`, `Mi`, `Gi`, `Ti`, `Pi`, `Ei`, `Zi`, `Yi`), pass `allowBinaryPrefixes: true`:

```csharp
var sector = new UnitDefinition(
    symbol: "sec512",
    dimension: Dimension.None,
    converter: UnitConversion.Linear(512d * 8d), // 512 bytes in bits
    name: "512-byte sector",
    allowPrefixes: true,
    defaultSpec: Isq.Information,
    allowBinaryPrefixes: true);
```

### F. Import Custom Units from Cameo SysML QUDV or FMI XML (`SharpUnits.SysMl`)

In MBSE workflows, load your Cameo/MagicDraw QUDV catalog directly into a `UnitEnvironment`:

```csharp
using SharpUnits.SysMl;

var catalog = QudvBridge.FromJson(qudvJsonText);
var (env, customSpecs) = QudvBridge.Import(catalog, includeStandardUnits: true);
```

---

## 2. Contributing a Built-In Unit to SharpUnits

When adding a permanent unit to the core SharpUnits catalog, follow this 6-step checklist:

1. **Dimension (`src/SharpUnits/Dimension.cs` & `RationalDimension.cs`)**
   - Verify whether the physical dimension already exists; if not, add it to both `Dimension` and `RationalDimension`.
2. **ISO 80000 Quantity Specification (`src/SharpUnits/QuantitySpec.cs`)**
   - Reuse an existing `Isq.*` spec where appropriate, or add a new `QuantitySpec` if ISO/IEC 80000 treats it as a distinct kind (for example `Isq.Activity` vs. `Isq.Frequency`, `Isq.AbsorbedDose` vs. `Isq.DoseEquivalent`, `Isq.Information` vs. `Isq.Dimensionless`).
3. **Unit Definition (`src/SharpUnits/Units.cs`)**
   - Define the `public static UnitDefinition` property using exact NIST / BIPM / ISO 80000 conversion constants.
   - Set `allowPrefixes: true` only for linear base units where SI decimal prefixes are valid (never for affine units, and never for `kg` where prefixes belong on `g`).
   - Set `allowBinaryPrefixes: true` only for IEC 80000-13 information/rate units.
   - Add the new property to `Units.AllStandard` (which automatically feeds `UnitRegistry.CreateStandard()` and `QudvBridge.ExportStandardCatalog()`).
4. **Aliases (`src/SharpUnits/UnitRegistry.cs`)**
   - Register ASCII-friendly or domain aliases in `UnitRegistry.CreateStandard()` (for example `Ohm` for `Ω`, `G` for `Gs`, `cc` for `mL`, `thou` for `mil`).
   - Double-check that neither the symbol nor alias collides with any existing unit or `<prefix><unit>` combination.
5. **Roslyn Compile-Time Analyzer (`src/SharpUnits.Analyzers/CompileTimeUnitValidator.cs`)**
   - Add the matching entry to `StandardUnits`, `UnitsPropertyMap`, and (if widely used) `CommonUnitSuggestions`.
   - Because `SharpUnits.Analyzers` targets `netstandard2.0` without loading `SharpUnits.dll`, keeping this table in sync ensures compile-time checks (`SU0001`–`SU0005`) and IDE quick-fixes work immediately.
6. **Verify Parity & Conversions**
   - Run the full test suite (`dotnet test SharpUnits.sln -c Release`).
   - The automated parity test in `tests/SharpUnits.Analyzers.Tests/SharpUnitsLiteralAnalyzerTests.cs` compiles every symbol in `Units.AllStandard` through the live Roslyn analyzer and fails immediately if any built-in unit is missing from `CompileTimeUnitValidator`.
   - Add conversion accuracy assertions in `tests/SharpUnits.Tests/UnitCatalogAndPrefixTests.cs`.
