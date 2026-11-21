namespace SharpUnits;

/// <summary>Canonical catalog of SI base, SI derived, electrical, engineering, and common imperial units.</summary>
public static class Units
{
    // --- SI Base Units ---

    /// <summary>Meter (<c>m</c>).</summary>
    public static UnitDefinition Meter { get; } = new("m", Dimension.LengthDimension, UnitConversion.Identity, "meter", allowPrefixes: true, QuantityKind.Length);

    /// <summary>Kilogram (<c>kg</c>) — coherent SI mass unit (prefixes apply to <see cref="Gram"/>).</summary>
    public static UnitDefinition Kilogram { get; } = new("kg", Dimension.MassDimension, UnitConversion.Identity, "kilogram", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Gram (<c>g</c>) — prefixable mass unit (<c>mg</c>, <c>µg</c>, etc.).</summary>
    public static UnitDefinition Gram { get; } = new("g", Dimension.MassDimension, UnitConversion.Linear(1e-3), "gram", allowPrefixes: true, QuantityKind.Mass);

    /// <summary>Second (<c>s</c>).</summary>
    public static UnitDefinition Second { get; } = new("s", Dimension.TimeDimension, UnitConversion.Identity, "second", allowPrefixes: true, QuantityKind.Time);

    /// <summary>Ampere (<c>A</c>).</summary>
    public static UnitDefinition Ampere { get; } = new("A", Dimension.ElectricCurrent, UnitConversion.Identity, "ampere", allowPrefixes: true, QuantityKind.Current);

    /// <summary>Kelvin (<c>K</c>).</summary>
    public static UnitDefinition Kelvin { get; } = new("K", Dimension.ThermodynamicTemperature, UnitConversion.Identity, "kelvin", allowPrefixes: true, QuantityKind.Temperature);

    /// <summary>Mole (<c>mol</c>).</summary>
    public static UnitDefinition Mole { get; } = new("mol", Dimension.AmountOfSubstance, UnitConversion.Identity, "mole", allowPrefixes: true);

    /// <summary>Candela (<c>cd</c>).</summary>
    public static UnitDefinition Candela { get; } = new("cd", Dimension.LuminousIntensity, UnitConversion.Identity, "candela", allowPrefixes: true);

    // --- SI Named Derived Units ---

    /// <summary>Dimensionless unity (<c>1</c>).</summary>
    public static UnitDefinition One { get; } = new("1", Dimension.None, UnitConversion.Identity, "one");

    /// <summary>Radian (<c>rad</c>).</summary>
    public static UnitDefinition Radian { get; } = new("rad", Dimension.None, UnitConversion.Identity, "radian", allowPrefixes: true, QuantityKind.Angle);

    /// <summary>Steradian (<c>sr</c>).</summary>
    public static UnitDefinition Steradian { get; } = new("sr", Dimension.None, UnitConversion.Identity, "steradian", allowPrefixes: true, QuantityKind.SolidAngle);

    /// <summary>Hertz (<c>Hz</c>).</summary>
    public static UnitDefinition Hertz { get; } = new("Hz", Dimension.Frequency, UnitConversion.Identity, "hertz", allowPrefixes: true, QuantityKind.Frequency);

    /// <summary>Newton (<c>N</c>).</summary>
    public static UnitDefinition Newton { get; } = new("N", Dimension.Force, UnitConversion.Identity, "newton", allowPrefixes: true, QuantityKind.Force);

    /// <summary>Pascal (<c>Pa</c>).</summary>
    public static UnitDefinition Pascal { get; } = new("Pa", Dimension.Pressure, UnitConversion.Identity, "pascal", allowPrefixes: true, QuantityKind.Pressure);

    /// <summary>Joule (<c>J</c>).</summary>
    public static UnitDefinition Joule { get; } = new("J", Dimension.Energy, UnitConversion.Identity, "joule", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Watt (<c>W</c>).</summary>
    public static UnitDefinition Watt { get; } = new("W", Dimension.Power, UnitConversion.Identity, "watt", allowPrefixes: true, QuantityKind.Power);

    /// <summary>Coulomb (<c>C</c>).</summary>
    public static UnitDefinition Coulomb { get; } = new("C", Dimension.ElectricCharge, UnitConversion.Identity, "coulomb", allowPrefixes: true, QuantityKind.Charge);

    /// <summary>Volt (<c>V</c>).</summary>
    public static UnitDefinition Volt { get; } = new("V", Dimension.Voltage, UnitConversion.Identity, "volt", allowPrefixes: true, QuantityKind.Voltage);

    /// <summary>Farad (<c>F</c>).</summary>
    public static UnitDefinition Farad { get; } = new("F", Dimension.Capacitance, UnitConversion.Identity, "farad", allowPrefixes: true, QuantityKind.Capacitance);

    /// <summary>Ohm (<c>Ω</c>).</summary>
    public static UnitDefinition Ohm { get; } = new("Ω", Dimension.Resistance, UnitConversion.Identity, "ohm", allowPrefixes: true, QuantityKind.Resistance);

    /// <summary>Siemens (<c>S</c>).</summary>
    public static UnitDefinition Siemens { get; } = new("S", Dimension.Conductance, UnitConversion.Identity, "siemens", allowPrefixes: true, QuantityKind.Conductance);

    /// <summary>Weber (<c>Wb</c>).</summary>
    public static UnitDefinition Weber { get; } = new("Wb", Dimension.MagneticFlux, UnitConversion.Identity, "weber", allowPrefixes: true, QuantityKind.MagneticFlux);

    /// <summary>Tesla (<c>T</c>).</summary>
    public static UnitDefinition Tesla { get; } = new("T", Dimension.MagneticFluxDensity, UnitConversion.Identity, "tesla", allowPrefixes: true, QuantityKind.MagneticFluxDensity);

    /// <summary>Henry (<c>H</c>).</summary>
    public static UnitDefinition Henry { get; } = new("H", Dimension.Inductance, UnitConversion.Identity, "henry", allowPrefixes: true, QuantityKind.Inductance);

    /// <summary>Degree Celsius (<c>°C</c>).</summary>
    public static UnitDefinition Celsius { get; } = new("°C", Dimension.ThermodynamicTemperature, UnitConversion.Celsius, "degree Celsius", allowPrefixes: false, QuantityKind.Temperature);

    /// <summary>Lumen (<c>lm</c>).</summary>
    public static UnitDefinition Lumen { get; } = new("lm", Dimension.LuminousIntensity, UnitConversion.Identity, "lumen", allowPrefixes: true);

    /// <summary>Lux (<c>lx</c>).</summary>
    public static UnitDefinition Lux { get; } = new("lx", Dimension.Illuminance, UnitConversion.Identity, "lux", allowPrefixes: true);

    /// <summary>Becquerel (<c>Bq</c>).</summary>
    public static UnitDefinition Becquerel { get; } = new("Bq", Dimension.Frequency, UnitConversion.Identity, "becquerel", allowPrefixes: true);

    /// <summary>Gray (<c>Gy</c>).</summary>
    public static UnitDefinition Gray { get; } = new("Gy", Dimension.RadiationDose, UnitConversion.Identity, "gray", allowPrefixes: true);

    /// <summary>Sievert (<c>Sv</c>).</summary>
    public static UnitDefinition Sievert { get; } = new("Sv", Dimension.RadiationDose, UnitConversion.Identity, "sievert", allowPrefixes: true);

    /// <summary>Katal (<c>kat</c>).</summary>
    public static UnitDefinition Katal { get; } = new("kat", Dimension.CatalyticActivity, UnitConversion.Identity, "katal", allowPrefixes: true);

    // --- Common Prefixed Electrical & Instrument Shorthands ---

    /// <summary>Milliampere (<c>mA</c>).</summary>
    public static UnitDefinition Milliampere { get; } = SiPrefixes.Apply("m", Ampere);

    /// <summary>Microampere (<c>µA</c>).</summary>
    public static UnitDefinition Microampere { get; } = SiPrefixes.Apply("µ", Ampere);

    /// <summary>Millivolt (<c>mV</c>).</summary>
    public static UnitDefinition Millivolt { get; } = SiPrefixes.Apply("m", Volt);

    /// <summary>Microvolt (<c>µV</c>).</summary>
    public static UnitDefinition Microvolt { get; } = SiPrefixes.Apply("µ", Volt);

    /// <summary>Kilovolt (<c>kV</c>).</summary>
    public static UnitDefinition Kilovolt { get; } = SiPrefixes.Apply("k", Volt);

    /// <summary>Kiloohm (<c>kΩ</c>).</summary>
    public static UnitDefinition Kiloohm { get; } = SiPrefixes.Apply("k", Ohm);

    /// <summary>Megaohm (<c>MΩ</c>).</summary>
    public static UnitDefinition Megaohm { get; } = SiPrefixes.Apply("M", Ohm);

    /// <summary>Kilohertz (<c>kHz</c>).</summary>
    public static UnitDefinition Kilohertz { get; } = SiPrefixes.Apply("k", Hertz);

    /// <summary>Megahertz (<c>MHz</c>).</summary>
    public static UnitDefinition Megahertz { get; } = SiPrefixes.Apply("M", Hertz);

    /// <summary>Gigahertz (<c>GHz</c>).</summary>
    public static UnitDefinition Gigahertz { get; } = SiPrefixes.Apply("G", Hertz);

    /// <summary>Millisecond (<c>ms</c>).</summary>
    public static UnitDefinition Millisecond { get; } = SiPrefixes.Apply("m", Second);

    /// <summary>Microsecond (<c>µs</c>).</summary>
    public static UnitDefinition Microsecond { get; } = SiPrefixes.Apply("µ", Second);

    /// <summary>Nanosecond (<c>ns</c>).</summary>
    public static UnitDefinition Nanosecond { get; } = SiPrefixes.Apply("n", Second);

    /// <summary>Microfarad (<c>µF</c>).</summary>
    public static UnitDefinition Microfarad { get; } = SiPrefixes.Apply("µ", Farad);

    /// <summary>Nanofarad (<c>nF</c>).</summary>
    public static UnitDefinition Nanofarad { get; } = SiPrefixes.Apply("n", Farad);

    /// <summary>Picofarad (<c>pF</c>).</summary>
    public static UnitDefinition Picofarad { get; } = SiPrefixes.Apply("p", Farad);

    /// <summary>Millihenry (<c>mH</c>).</summary>
    public static UnitDefinition Millihenry { get; } = SiPrefixes.Apply("m", Henry);

    /// <summary>Microhenry (<c>µH</c>).</summary>
    public static UnitDefinition Microhenry { get; } = SiPrefixes.Apply("µ", Henry);

    /// <summary>Milliwatt (<c>mW</c>).</summary>
    public static UnitDefinition Milliwatt { get; } = SiPrefixes.Apply("m", Watt);

    /// <summary>Kilowatt (<c>kW</c>).</summary>
    public static UnitDefinition Kilowatt { get; } = SiPrefixes.Apply("k", Watt);

    // --- Common Non-SI, Engineering, Mechanical & Imperial Units ---

    /// <summary>Degree Fahrenheit (<c>°F</c>).</summary>
    public static UnitDefinition Fahrenheit { get; } = new("°F", Dimension.ThermodynamicTemperature, UnitConversion.Fahrenheit, "degree Fahrenheit", allowPrefixes: false, QuantityKind.Temperature);

    /// <summary>Degree Rankine (<c>°R</c>).</summary>
    public static UnitDefinition Rankine { get; } = new("°R", Dimension.ThermodynamicTemperature, UnitConversion.Rankine, "degree Rankine", allowPrefixes: false, QuantityKind.Temperature);

    /// <summary>Minute (<c>min</c>).</summary>
    public static UnitDefinition Minute { get; } = new("min", Dimension.TimeDimension, UnitConversion.Linear(60d), "minute", allowPrefixes: false, QuantityKind.Time);

    /// <summary>Hour (<c>h</c>).</summary>
    public static UnitDefinition Hour { get; } = new("h", Dimension.TimeDimension, UnitConversion.Linear(3600d), "hour", allowPrefixes: false, QuantityKind.Time);

    /// <summary>Day (<c>d</c>).</summary>
    public static UnitDefinition Day { get; } = new("d", Dimension.TimeDimension, UnitConversion.Linear(86400d), "day", allowPrefixes: false, QuantityKind.Time);

    /// <summary>Inch (<c>in</c>, exact 0.0254 m).</summary>
    public static UnitDefinition Inch { get; } = new("in", Dimension.LengthDimension, UnitConversion.Linear(0.0254d), "inch", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Foot (<c>ft</c>, exact 0.3048 m).</summary>
    public static UnitDefinition Foot { get; } = new("ft", Dimension.LengthDimension, UnitConversion.Linear(0.3048d), "foot", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Yard (<c>yd</c>, exact 0.9144 m).</summary>
    public static UnitDefinition Yard { get; } = new("yd", Dimension.LengthDimension, UnitConversion.Linear(0.9144d), "yard", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Statute mile (<c>mi</c>, exact 1609.344 m).</summary>
    public static UnitDefinition Mile { get; } = new("mi", Dimension.LengthDimension, UnitConversion.Linear(1609.344d), "mile", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Thousandth of an inch (<c>mil</c>, 25.4 µm — common in PCB/EE engineering).</summary>
    public static UnitDefinition Mil { get; } = new("mil", Dimension.LengthDimension, UnitConversion.Linear(0.0000254d), "mil", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Angstrom (<c>Å</c>, 10⁻¹⁰ m).</summary>
    public static UnitDefinition Angstrom { get; } = new("Å", Dimension.LengthDimension, UnitConversion.Linear(1e-10d), "angstrom", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Liter (<c>L</c>, 10⁻³ m³).</summary>
    public static UnitDefinition Liter { get; } = new("L", Dimension.Volume, UnitConversion.Linear(1e-3d), "liter", allowPrefixes: true);

    /// <summary>US liquid gallon (<c>gal</c>, exact 3.785411784×10⁻³ m³).</summary>
    public static UnitDefinition GallonUs { get; } = new("gal", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d), "US gallon");

    /// <summary>Metric tonne (<c>t</c>, 1000 kg).</summary>
    public static UnitDefinition Tonne { get; } = new("t", Dimension.MassDimension, UnitConversion.Linear(1000d), "tonne", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Avoirdupois pound (<c>lb</c>, exact 0.45359237 kg).</summary>
    public static UnitDefinition Pound { get; } = new("lb", Dimension.MassDimension, UnitConversion.Linear(0.45359237d), "pound", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Avoirdupois ounce (<c>oz</c>, 1/16 lb).</summary>
    public static UnitDefinition Ounce { get; } = new("oz", Dimension.MassDimension, UnitConversion.Linear(0.45359237d / 16d), "ounce", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Pound-force (<c>lbf</c>).</summary>
    public static UnitDefinition PoundForce { get; } = new("lbf", Dimension.Force, UnitConversion.Linear(4.4482216152605d), "pound-force", allowPrefixes: false, QuantityKind.Force);

    /// <summary>Bar (<c>bar</c>, 10⁵ Pa).</summary>
    public static UnitDefinition Bar { get; } = new("bar", Dimension.Pressure, UnitConversion.Linear(1e5d), "bar", allowPrefixes: true, QuantityKind.Pressure);

    /// <summary>Standard atmosphere (<c>atm</c>, exact 101325 Pa).</summary>
    public static UnitDefinition Atmosphere { get; } = new("atm", Dimension.Pressure, UnitConversion.Linear(101325d), "atmosphere", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Pounds per square inch (<c>psi</c>).</summary>
    public static UnitDefinition Psi { get; } = new("psi", Dimension.Pressure, UnitConversion.Linear(6894.757293168361d), "psi", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Torr (<c>Torr</c>, 101325/760 Pa).</summary>
    public static UnitDefinition Torr { get; } = new("Torr", Dimension.Pressure, UnitConversion.Linear(101325d / 760d), "torr", allowPrefixes: true, QuantityKind.Pressure);

    /// <summary>Degree of plane angle (<c>°</c>, π/180 rad).</summary>
    public static UnitDefinition Degree { get; } = new("°", Dimension.None, UnitConversion.Linear(Math.PI / 180d), "degree", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Full revolution (<c>rev</c>, 2π rad).</summary>
    public static UnitDefinition Revolution { get; } = new("rev", Dimension.None, UnitConversion.Linear(2d * Math.PI), "revolution", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Percent (<c>%</c>, 0.01).</summary>
    public static UnitDefinition Percent { get; } = new("%", Dimension.None, UnitConversion.Linear(1e-2d), "percent");

    /// <summary>Parts per million (<c>ppm</c>, 10⁻⁶).</summary>
    public static UnitDefinition PartsPerMillion { get; } = new("ppm", Dimension.None, UnitConversion.Linear(1e-6d), "parts per million");

    /// <summary>Parts per billion (<c>ppb</c>, 10⁻⁹).</summary>
    public static UnitDefinition PartsPerBillion { get; } = new("ppb", Dimension.None, UnitConversion.Linear(1e-9d), "parts per billion");

    /// <summary>Electronvolt (<c>eV</c>, exact 1.602176634×10⁻¹⁹ J).</summary>
    public static UnitDefinition ElectronVolt { get; } = new("eV", Dimension.Energy, UnitConversion.Linear(1.602176634e-19d), "electronvolt", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Watt-hour (<c>Wh</c>, 3600 J).</summary>
    public static UnitDefinition WattHour { get; } = new("Wh", Dimension.Energy, UnitConversion.Linear(3600d), "watt-hour", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Ampere-hour (<c>Ah</c>, 3600 C).</summary>
    public static UnitDefinition AmpereHour { get; } = new("Ah", Dimension.ElectricCharge, UnitConversion.Linear(3600d), "ampere-hour", allowPrefixes: true, QuantityKind.Charge);

    /// <summary>Newton-meter (<c>N·m</c>, torque).</summary>
    public static UnitDefinition NewtonMeter { get; } = new("N·m", Dimension.Energy, UnitConversion.Identity, "newton-meter", allowPrefixes: false, QuantityKind.Torque);

    /// <summary>Volt-ampere (<c>VA</c>, apparent power).</summary>
    public static UnitDefinition VoltAmpere { get; } = new("VA", Dimension.Power, UnitConversion.Identity, "volt-ampere", allowPrefixes: true, QuantityKind.ApparentPower);

    /// <summary>Volt-ampere reactive (<c>var</c>, reactive power).</summary>
    public static UnitDefinition Var { get; } = new("var", Dimension.Power, UnitConversion.Identity, "var", allowPrefixes: true, QuantityKind.ReactivePower);

    /// <summary>Mechanical horsepower (<c>hp</c>, 745.69987158227022 W).</summary>
    public static UnitDefinition Horsepower { get; } = new("hp", Dimension.Power, UnitConversion.Linear(745.69987158227022d), "horsepower", allowPrefixes: false, QuantityKind.Power);

    /// <summary>All primary units included in <see cref="UnitRegistry.CreateStandard"/>.</summary>
    public static IReadOnlyList<UnitDefinition> AllStandard { get; } =
    [
        Meter, Kilogram, Gram, Second, Ampere, Kelvin, Mole, Candela,
        One, Radian, Steradian, Hertz, Newton, Pascal, Joule, Watt,
        Coulomb, Volt, Farad, Ohm, Siemens, Weber, Tesla, Henry,
        Celsius, Lumen, Lux, Becquerel, Gray, Sievert, Katal,
        Fahrenheit, Rankine, Minute, Hour, Day,
        Inch, Foot, Yard, Mile, Mil, Angstrom,
        Liter, GallonUs, Tonne, Pound, Ounce, PoundForce,
        Bar, Atmosphere, Psi, Torr,
        Degree, Revolution, Percent, PartsPerMillion, PartsPerBillion,
        ElectronVolt, WattHour, AmpereHour, NewtonMeter, VoltAmpere, Var, Horsepower
    ];

    /// <summary>Attempts to look up the canonical coherent SI unit for a given dimension.</summary>
    public static bool TryGetCoherentSiUnit(RationalDimension dimension, out UnitDefinition? unit)
    {
        unit = dimension switch
        {
            var d when d == RationalDimension.None => One,
            var d when d == RationalDimension.LengthDimension => Meter,
            var d when d == RationalDimension.MassDimension => Kilogram,
            var d when d == RationalDimension.TimeDimension => Second,
            var d when d == RationalDimension.ElectricCurrent => Ampere,
            var d when d == RationalDimension.ThermodynamicTemperature => Kelvin,
            var d when d == RationalDimension.AmountOfSubstance => Mole,
            var d when d == RationalDimension.LuminousIntensity => Candela,
            var d when d == RationalDimension.Frequency => Hertz,
            var d when d == RationalDimension.Force => Newton,
            var d when d == RationalDimension.Pressure => Pascal,
            var d when d == RationalDimension.Energy => Joule,
            var d when d == RationalDimension.Power => Watt,
            var d when d == RationalDimension.ElectricCharge => Coulomb,
            var d when d == RationalDimension.Voltage => Volt,
            var d when d == RationalDimension.Capacitance => Farad,
            var d when d == RationalDimension.Resistance => Ohm,
            var d when d == RationalDimension.Conductance => Siemens,
            var d when d == RationalDimension.MagneticFlux => Weber,
            var d when d == RationalDimension.MagneticFluxDensity => Tesla,
            var d when d == RationalDimension.Inductance => Henry,
            _ => null
        };
        return unit is not null;
    }
}
