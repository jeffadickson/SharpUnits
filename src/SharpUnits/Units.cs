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
    public static UnitDefinition Mole { get; } = new("mol", Dimension.AmountOfSubstance, UnitConversion.Identity, "mole", allowPrefixes: true, defaultSpec: Isq.AmountOfSubstance);

    /// <summary>Candela (<c>cd</c>, luminous intensity — distinct from <see cref="Lumen"/>).</summary>
    public static UnitDefinition Candela { get; } = new("cd", Dimension.LuminousIntensity, UnitConversion.Identity, "candela", allowPrefixes: true, defaultSpec: Isq.LuminousIntensity);

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

    /// <summary>Lumen (<c>lm</c>, luminous flux).</summary>
    public static UnitDefinition Lumen { get; } = new("lm", Dimension.LuminousIntensity, UnitConversion.Identity, "lumen", allowPrefixes: true, defaultSpec: Isq.LuminousFlux);

    /// <summary>Lux (<c>lx</c>, illuminance).</summary>
    public static UnitDefinition Lux { get; } = new("lx", Dimension.Illuminance, UnitConversion.Identity, "lux", allowPrefixes: true, defaultSpec: Isq.Illuminance);

    /// <summary>Becquerel (<c>Bq</c>, radionuclide activity — distinct from <see cref="Hertz"/>).</summary>
    public static UnitDefinition Becquerel { get; } = new("Bq", Dimension.Frequency, UnitConversion.Identity, "becquerel", allowPrefixes: true, defaultSpec: Isq.Activity);

    /// <summary>Gray (<c>Gy</c>, absorbed radiation dose — distinct from <see cref="Sievert"/>).</summary>
    public static UnitDefinition Gray { get; } = new("Gy", Dimension.RadiationDose, UnitConversion.Identity, "gray", allowPrefixes: true, defaultSpec: Isq.AbsorbedDose);

    /// <summary>Sievert (<c>Sv</c>, dose equivalent — distinct from <see cref="Gray"/>).</summary>
    public static UnitDefinition Sievert { get; } = new("Sv", Dimension.RadiationDose, UnitConversion.Identity, "sievert", allowPrefixes: true, defaultSpec: Isq.DoseEquivalent);

    /// <summary>Katal (<c>kat</c>, catalytic activity).</summary>
    public static UnitDefinition Katal { get; } = new("kat", Dimension.CatalyticActivity, UnitConversion.Identity, "katal", allowPrefixes: true, defaultSpec: Isq.CatalyticActivity);

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

    /// <summary>Micrometer / micron (<c>µm</c>).</summary>
    public static UnitDefinition Micrometer { get; } = SiPrefixes.Apply("µ", Meter);

    // --- IEC 80000-13 Information & Data Rate Units (Decimal SI + IEC Binary Prefixes) ---

    /// <summary>Bit (<c>bit</c>, base unit of information).</summary>
    public static UnitDefinition Bit { get; } = new("bit", Dimension.None, UnitConversion.Identity, "bit", allowPrefixes: true, defaultSpec: Isq.Information, allowBinaryPrefixes: true);

    /// <summary>Byte (<c>B</c>, exact 8 bits).</summary>
    public static UnitDefinition Byte { get; } = new("B", Dimension.None, UnitConversion.Linear(8d), "byte", allowPrefixes: true, defaultSpec: Isq.Information, allowBinaryPrefixes: true);

    /// <summary>Bits per second (<c>bps</c>).</summary>
    public static UnitDefinition BitsPerSecond { get; } = new("bps", Dimension.Frequency, UnitConversion.Identity, "bits per second", allowPrefixes: true, defaultSpec: Isq.InformationRate, allowBinaryPrefixes: true);

    /// <summary>Bytes per second (<c>Bps</c>, 8 bps).</summary>
    public static UnitDefinition BytesPerSecond { get; } = new("Bps", Dimension.Frequency, UnitConversion.Linear(8d), "bytes per second", allowPrefixes: true, defaultSpec: Isq.InformationRate, allowBinaryPrefixes: true);

    /// <summary>Baud (<c>Bd</c>, symbol rate in symbols/s).</summary>
    public static UnitDefinition Baud { get; } = new("Bd", Dimension.Frequency, UnitConversion.Identity, "baud", allowPrefixes: true, defaultSpec: Isq.InformationRate);

    // --- Temperature, Time & Kinematics ---

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

    /// <summary>Week (<c>wk</c>, exact 7 days).</summary>
    public static UnitDefinition Week { get; } = new("wk", Dimension.TimeDimension, UnitConversion.Linear(604800d), "week", allowPrefixes: false, QuantityKind.Time);

    /// <summary>Julian year (<c>yr</c>, exact 365.25 days).</summary>
    public static UnitDefinition Year { get; } = new("yr", Dimension.TimeDimension, UnitConversion.Linear(31557600d), "Julian year", allowPrefixes: false, QuantityKind.Time);

    /// <summary>Revolutions per minute (<c>rpm</c>, 1/60 Hz).</summary>
    public static UnitDefinition Rpm { get; } = new("rpm", Dimension.Frequency, UnitConversion.Linear(1d / 60d), "revolutions per minute", allowPrefixes: false, QuantityKind.Frequency);

    /// <summary>Knot (<c>kn</c>, 1 nautical mile per hour = 1852/3600 m/s).</summary>
    public static UnitDefinition Knot { get; } = new("kn", Dimension.Velocity, UnitConversion.Linear(1852d / 3600d), "knot", allowPrefixes: false, QuantityKind.Velocity);

    /// <summary>Miles per hour (<c>mph</c>).</summary>
    public static UnitDefinition MilesPerHour { get; } = new("mph", Dimension.Velocity, UnitConversion.Linear(1609.344d / 3600d), "miles per hour", allowPrefixes: false, QuantityKind.Velocity);

    /// <summary>Kilometers per hour (<c>kph</c>).</summary>
    public static UnitDefinition KilometersPerHour { get; } = new("kph", Dimension.Velocity, UnitConversion.Linear(1000d / 3600d), "kilometers per hour", allowPrefixes: false, QuantityKind.Velocity);

    /// <summary>Standard gravity (<c>g0</c>, exact 9.80665 m/s² — accelerometer and shock testing standard).</summary>
    public static UnitDefinition StandardGravity { get; } = new("g0", Dimension.Acceleration, UnitConversion.Linear(9.80665d), "standard gravity", allowPrefixes: false, QuantityKind.Acceleration);

    /// <summary>Gal (<c>Gal</c>, 0.01 m/s² — gravimetry / CGS acceleration).</summary>
    public static UnitDefinition Gal { get; } = new("Gal", Dimension.Acceleration, UnitConversion.Linear(0.01d), "gal", allowPrefixes: true, QuantityKind.Acceleration);

    // --- Length, Astronomy, Area & Volume ---

    /// <summary>Inch (<c>in</c>, exact 0.0254 m).</summary>
    public static UnitDefinition Inch { get; } = new("in", Dimension.LengthDimension, UnitConversion.Linear(0.0254d), "inch", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Foot (<c>ft</c>, exact 0.3048 m).</summary>
    public static UnitDefinition Foot { get; } = new("ft", Dimension.LengthDimension, UnitConversion.Linear(0.3048d), "foot", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Yard (<c>yd</c>, exact 0.9144 m).</summary>
    public static UnitDefinition Yard { get; } = new("yd", Dimension.LengthDimension, UnitConversion.Linear(0.9144d), "yard", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Statute mile (<c>mi</c>, exact 1609.344 m).</summary>
    public static UnitDefinition Mile { get; } = new("mi", Dimension.LengthDimension, UnitConversion.Linear(1609.344d), "mile", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Nautical mile (<c>nmi</c>, exact 1852 m).</summary>
    public static UnitDefinition NauticalMile { get; } = new("nmi", Dimension.LengthDimension, UnitConversion.Linear(1852d), "nautical mile", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Thousandth of an inch (<c>mil</c>, 25.4 µm — common in PCB/EE engineering).</summary>
    public static UnitDefinition Mil { get; } = new("mil", Dimension.LengthDimension, UnitConversion.Linear(0.0000254d), "mil", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Fathom (<c>fathom</c>, exact 6 ft = 1.8288 m).</summary>
    public static UnitDefinition Fathom { get; } = new("fathom", Dimension.LengthDimension, UnitConversion.Linear(1.8288d), "fathom", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Surveyor's chain (<c>ch</c>, exact 66 ft = 20.1168 m).</summary>
    public static UnitDefinition Chain { get; } = new("ch", Dimension.LengthDimension, UnitConversion.Linear(20.1168d), "chain", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Furlong (<c>furlong</c>, exact 660 ft = 201.168 m).</summary>
    public static UnitDefinition Furlong { get; } = new("furlong", Dimension.LengthDimension, UnitConversion.Linear(201.168d), "furlong", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Angstrom (<c>Å</c>, 10⁻¹⁰ m).</summary>
    public static UnitDefinition Angstrom { get; } = new("Å", Dimension.LengthDimension, UnitConversion.Linear(1e-10d), "angstrom", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Astronomical unit (<c>au</c>, exact IAU 2012 value 149,597,870,700 m).</summary>
    public static UnitDefinition AstronomicalUnit { get; } = new("au", Dimension.LengthDimension, UnitConversion.Linear(149_597_870_700d), "astronomical unit", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Light-year (<c>ly</c>, exact Julian light-year 9,460,730,472,580,800 m).</summary>
    public static UnitDefinition LightYear { get; } = new("ly", Dimension.LengthDimension, UnitConversion.Linear(9_460_730_472_580_800d), "light-year", allowPrefixes: false, QuantityKind.Length);

    /// <summary>Parsec (<c>pc</c>, 648000/π au).</summary>
    public static UnitDefinition Parsec { get; } = new("pc", Dimension.LengthDimension, UnitConversion.Linear(149_597_870_700d * 648_000d / Math.PI), "parsec", allowPrefixes: true, QuantityKind.Length);

    /// <summary>Hectare (<c>ha</c>, 10⁴ m²).</summary>
    public static UnitDefinition Hectare { get; } = new("ha", Dimension.Area, UnitConversion.Linear(1e4d), "hectare", allowPrefixes: false, defaultSpec: Isq.Area);

    /// <summary>Are (<c>are</c>, 100 m²).</summary>
    public static UnitDefinition Are { get; } = new("are", Dimension.Area, UnitConversion.Linear(100d), "are", allowPrefixes: false, defaultSpec: Isq.Area);

    /// <summary>International acre (<c>ac</c>, exact 4046.8564224 m²).</summary>
    public static UnitDefinition Acre { get; } = new("ac", Dimension.Area, UnitConversion.Linear(4046.8564224d), "acre", allowPrefixes: false, defaultSpec: Isq.Area);

    /// <summary>Barn (<c>barn</c>, 10⁻²⁸ m² — nuclear cross section).</summary>
    public static UnitDefinition Barn { get; } = new("barn", Dimension.Area, UnitConversion.Linear(1e-28d), "barn", allowPrefixes: true, defaultSpec: Isq.Area);

    /// <summary>Liter (<c>L</c>, 10⁻³ m³).</summary>
    public static UnitDefinition Liter { get; } = new("L", Dimension.Volume, UnitConversion.Linear(1e-3d), "liter", allowPrefixes: true, defaultSpec: Isq.Volume);

    /// <summary>Milliliter / cubic centimeter (<c>mL</c>).</summary>
    public static UnitDefinition Milliliter { get; } = SiPrefixes.Apply("m", Liter);

    /// <summary>US liquid gallon (<c>gal</c>, exact 3.785411784×10⁻³ m³).</summary>
    public static UnitDefinition GallonUs { get; } = new("gal", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d), "US gallon", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>Imperial gallon (<c>gal_uk</c>, exact 4.54609×10⁻³ m³).</summary>
    public static UnitDefinition GallonImperial { get; } = new("gal_uk", Dimension.Volume, UnitConversion.Linear(4.54609e-3d), "Imperial gallon", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US liquid quart (<c>qt</c>, 1/4 US gallon).</summary>
    public static UnitDefinition QuartUs { get; } = new("qt", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 4d), "US quart", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US liquid pint (<c>pt</c>, 1/8 US gallon).</summary>
    public static UnitDefinition PintUs { get; } = new("pt", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 8d), "US pint", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US legal cup (<c>cup</c>, 1/16 US gallon).</summary>
    public static UnitDefinition CupUs { get; } = new("cup", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 16d), "US cup", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US fluid ounce (<c>floz</c>, 1/128 US gallon).</summary>
    public static UnitDefinition FluidOunceUs { get; } = new("floz", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 128d), "US fluid ounce", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US tablespoon (<c>tbsp</c>, 1/2 US fluid ounce).</summary>
    public static UnitDefinition TablespoonUs { get; } = new("tbsp", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 256d), "US tablespoon", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>US teaspoon (<c>tsp</c>, 1/6 US fluid ounce).</summary>
    public static UnitDefinition TeaspoonUs { get; } = new("tsp", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d / 768d), "US teaspoon", allowPrefixes: false, defaultSpec: Isq.Volume);

    /// <summary>Oil barrel (<c>bbl</c>, exact 42 US gallons).</summary>
    public static UnitDefinition OilBarrel { get; } = new("bbl", Dimension.Volume, UnitConversion.Linear(3.785411784e-3d * 42d), "oil barrel", allowPrefixes: false, defaultSpec: Isq.Volume);

    // --- Mass, Force, Torque & Pressure ---

    /// <summary>Metric tonne (<c>t</c>, 1000 kg).</summary>
    public static UnitDefinition Tonne { get; } = new("t", Dimension.MassDimension, UnitConversion.Linear(1000d), "tonne", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Avoirdupois pound (<c>lb</c>, exact 0.45359237 kg).</summary>
    public static UnitDefinition Pound { get; } = new("lb", Dimension.MassDimension, UnitConversion.Linear(0.45359237d), "pound", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Avoirdupois ounce (<c>oz</c>, 1/16 lb).</summary>
    public static UnitDefinition Ounce { get; } = new("oz", Dimension.MassDimension, UnitConversion.Linear(0.45359237d / 16d), "ounce", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Slug (<c>slug</c>, imperial engineering mass = 1 lbf·s²/ft).</summary>
    public static UnitDefinition Slug { get; } = new("slug", Dimension.MassDimension, UnitConversion.Linear(14.59390293720636d), "slug", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Unified atomic mass unit / dalton (<c>Da</c>, CODATA 2022 value 1.66053906892×10⁻²⁷ kg).</summary>
    public static UnitDefinition Dalton { get; } = new("Da", Dimension.MassDimension, UnitConversion.Linear(1.66053906892e-27d), "dalton", allowPrefixes: true, QuantityKind.Mass);

    /// <summary>US short ton (<c>ton_us</c>, exact 2000 lb).</summary>
    public static UnitDefinition ShortTon { get; } = new("ton_us", Dimension.MassDimension, UnitConversion.Linear(0.45359237d * 2000d), "short ton", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Imperial long ton (<c>ton_uk</c>, exact 2240 lb).</summary>
    public static UnitDefinition LongTon { get; } = new("ton_uk", Dimension.MassDimension, UnitConversion.Linear(0.45359237d * 2240d), "long ton", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Stone (<c>st</c>, exact 14 lb).</summary>
    public static UnitDefinition Stone { get; } = new("st", Dimension.MassDimension, UnitConversion.Linear(0.45359237d * 14d), "stone", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Grain (<c>gr</c>, exact 64.79891 mg).</summary>
    public static UnitDefinition Grain { get; } = new("gr", Dimension.MassDimension, UnitConversion.Linear(6.479891e-5d), "grain", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Metric carat (<c>ct</c>, exact 200 mg).</summary>
    public static UnitDefinition Carat { get; } = new("ct", Dimension.MassDimension, UnitConversion.Linear(2e-4d), "carat", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Troy ounce (<c>ozt</c>, exact 31.1034768 g).</summary>
    public static UnitDefinition TroyOunce { get; } = new("ozt", Dimension.MassDimension, UnitConversion.Linear(0.0311034768d), "troy ounce", allowPrefixes: false, QuantityKind.Mass);

    /// <summary>Pound-force (<c>lbf</c>).</summary>
    public static UnitDefinition PoundForce { get; } = new("lbf", Dimension.Force, UnitConversion.Linear(4.4482216152605d), "pound-force", allowPrefixes: false, QuantityKind.Force);

    /// <summary>Kilogram-force / kilopond (<c>kgf</c>, exact 9.80665 N).</summary>
    public static UnitDefinition KilogramForce { get; } = new("kgf", Dimension.Force, UnitConversion.Linear(9.80665d), "kilogram-force", allowPrefixes: false, QuantityKind.Force);

    /// <summary>Dyne (<c>dyn</c>, 10⁻⁵ N — CGS force).</summary>
    public static UnitDefinition Dyne { get; } = new("dyn", Dimension.Force, UnitConversion.Linear(1e-5d), "dyne", allowPrefixes: true, QuantityKind.Force);

    /// <summary>Kip (<c>kip</c>, 1000 lbf — structural engineering force).</summary>
    public static UnitDefinition Kip { get; } = new("kip", Dimension.Force, UnitConversion.Linear(4448.2216152605d), "kip", allowPrefixes: false, QuantityKind.Force);

    /// <summary>Ounce-force (<c>ozf</c>, 1/16 lbf).</summary>
    public static UnitDefinition OunceForce { get; } = new("ozf", Dimension.Force, UnitConversion.Linear(4.4482216152605d / 16d), "ounce-force", allowPrefixes: false, QuantityKind.Force);

    /// <summary>Bar (<c>bar</c>, 10⁵ Pa).</summary>
    public static UnitDefinition Bar { get; } = new("bar", Dimension.Pressure, UnitConversion.Linear(1e5d), "bar", allowPrefixes: true, QuantityKind.Pressure);

    /// <summary>Standard atmosphere (<c>atm</c>, exact 101325 Pa).</summary>
    public static UnitDefinition Atmosphere { get; } = new("atm", Dimension.Pressure, UnitConversion.Linear(101325d), "atmosphere", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Technical atmosphere (<c>at</c>, 1 kgf/cm² = 98066.5 Pa).</summary>
    public static UnitDefinition TechnicalAtmosphere { get; } = new("at", Dimension.Pressure, UnitConversion.Linear(98066.5d), "technical atmosphere", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Pounds per square inch (<c>psi</c>).</summary>
    public static UnitDefinition Psi { get; } = new("psi", Dimension.Pressure, UnitConversion.Linear(6894.757293168361d), "psi", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Kilopounds per square inch (<c>ksi</c>, 1000 psi).</summary>
    public static UnitDefinition Ksi { get; } = new("ksi", Dimension.Pressure, UnitConversion.Linear(6_894_757.293168361d), "ksi", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Torr (<c>Torr</c>, 101325/760 Pa).</summary>
    public static UnitDefinition Torr { get; } = new("Torr", Dimension.Pressure, UnitConversion.Linear(101325d / 760d), "torr", allowPrefixes: true, QuantityKind.Pressure);

    /// <summary>Inch of mercury (<c>inHg</c>, conventional 0 °C = 3386.388666666667 Pa).</summary>
    public static UnitDefinition InchOfMercury { get; } = new("inHg", Dimension.Pressure, UnitConversion.Linear(3386.388666666667d), "inch of mercury", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Inch of water (<c>inH2O</c>, conventional 4 °C = 249.08891 Pa).</summary>
    public static UnitDefinition InchOfWater { get; } = new("inH2O", Dimension.Pressure, UnitConversion.Linear(249.08891d), "inch of water", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Centimeter of water (<c>cmH2O</c>, 98.0665 Pa).</summary>
    public static UnitDefinition CentimeterOfWater { get; } = new("cmH2O", Dimension.Pressure, UnitConversion.Linear(98.0665d), "centimeter of water", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Millimeter of water (<c>mmH2O</c>, 9.80665 Pa).</summary>
    public static UnitDefinition MillimeterOfWater { get; } = new("mmH2O", Dimension.Pressure, UnitConversion.Linear(9.80665d), "millimeter of water", allowPrefixes: false, QuantityKind.Pressure);

    /// <summary>Barye (<c>Ba</c>, 0.1 Pa — CGS pressure).</summary>
    public static UnitDefinition Barye { get; } = new("Ba", Dimension.Pressure, UnitConversion.Linear(0.1d), "barye", allowPrefixes: true, QuantityKind.Pressure);

    // --- Angles & Dimensionless Ratios ---

    /// <summary>Degree of plane angle (<c>°</c>, π/180 rad).</summary>
    public static UnitDefinition Degree { get; } = new("°", Dimension.None, UnitConversion.Linear(Math.PI / 180d), "degree", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Arcminute (<c>arcmin</c>, 1/60 degree).</summary>
    public static UnitDefinition Arcminute { get; } = new("arcmin", Dimension.None, UnitConversion.Linear(Math.PI / 10_800d), "arcminute", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Arcsecond (<c>arcsec</c>, 1/3600 degree).</summary>
    public static UnitDefinition Arcsecond { get; } = new("arcsec", Dimension.None, UnitConversion.Linear(Math.PI / 648_000d), "arcsecond", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Gradian / gon (<c>grad</c>, π/200 rad).</summary>
    public static UnitDefinition Gradian { get; } = new("grad", Dimension.None, UnitConversion.Linear(Math.PI / 200d), "gradian", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Full revolution (<c>rev</c>, 2π rad).</summary>
    public static UnitDefinition Revolution { get; } = new("rev", Dimension.None, UnitConversion.Linear(2d * Math.PI), "revolution", allowPrefixes: false, QuantityKind.Angle);

    /// <summary>Percent (<c>%</c>, 0.01).</summary>
    public static UnitDefinition Percent { get; } = new("%", Dimension.None, UnitConversion.Linear(1e-2d), "percent");

    /// <summary>Permille (<c>‰</c>, 0.001).</summary>
    public static UnitDefinition Permille { get; } = new("‰", Dimension.None, UnitConversion.Linear(1e-3d), "permille");

    /// <summary>Parts per million (<c>ppm</c>, 10⁻⁶).</summary>
    public static UnitDefinition PartsPerMillion { get; } = new("ppm", Dimension.None, UnitConversion.Linear(1e-6d), "parts per million");

    /// <summary>Parts per billion (<c>ppb</c>, 10⁻⁹).</summary>
    public static UnitDefinition PartsPerBillion { get; } = new("ppb", Dimension.None, UnitConversion.Linear(1e-9d), "parts per billion");

    /// <summary>Parts per trillion (<c>ppt</c>, 10⁻¹²).</summary>
    public static UnitDefinition PartsPerTrillion { get; } = new("ppt", Dimension.None, UnitConversion.Linear(1e-12d), "parts per trillion");

    /// <summary>Neper (<c>Np</c>, natural logarithmic ratio unit).</summary>
    public static UnitDefinition Neper { get; } = new("Np", Dimension.None, UnitConversion.Identity, "neper", allowPrefixes: true);

    // --- Energy, Torque, Power, Magnetism, Fluid, Chemistry & Radiation ---

    /// <summary>Electronvolt (<c>eV</c>, exact 1.602176634×10⁻¹⁹ J).</summary>
    public static UnitDefinition ElectronVolt { get; } = new("eV", Dimension.Energy, UnitConversion.Linear(1.602176634e-19d), "electronvolt", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Watt-hour (<c>Wh</c>, 3600 J).</summary>
    public static UnitDefinition WattHour { get; } = new("Wh", Dimension.Energy, UnitConversion.Linear(3600d), "watt-hour", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Thermochemical calorie (<c>cal</c>, exact 4.184 J; prefixable to <c>kcal</c>).</summary>
    public static UnitDefinition Calorie { get; } = new("cal", Dimension.Energy, UnitConversion.Linear(4.184d), "calorie", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>Nutritional / food Calorie (<c>Cal</c>, 1 kcal = 4184 J).</summary>
    public static UnitDefinition Kilocalorie { get; } = new("Cal", Dimension.Energy, UnitConversion.Linear(4184d), "kilocalorie", allowPrefixes: false, QuantityKind.Energy);

    /// <summary>International Table British thermal unit (<c>BTU</c>, 1055.05585262 J).</summary>
    public static UnitDefinition Btu { get; } = new("BTU", Dimension.Energy, UnitConversion.Linear(1055.05585262d), "British thermal unit", allowPrefixes: false, QuantityKind.Energy);

    /// <summary>Erg (<c>erg</c>, 10⁻⁷ J — CGS energy).</summary>
    public static UnitDefinition Erg { get; } = new("erg", Dimension.Energy, UnitConversion.Linear(1e-7d), "erg", allowPrefixes: true, QuantityKind.Energy);

    /// <summary>US therm (<c>therm</c>, 100,000 BTU).</summary>
    public static UnitDefinition Therm { get; } = new("therm", Dimension.Energy, UnitConversion.Linear(1.05505585262e8d), "therm", allowPrefixes: false, QuantityKind.Energy);

    /// <summary>Ampere-hour (<c>Ah</c>, 3600 C).</summary>
    public static UnitDefinition AmpereHour { get; } = new("Ah", Dimension.ElectricCharge, UnitConversion.Linear(3600d), "ampere-hour", allowPrefixes: true, QuantityKind.Charge);

    /// <summary>Newton-meter (<c>N·m</c>, torque).</summary>
    public static UnitDefinition NewtonMeter { get; } = new("N·m", Dimension.Energy, UnitConversion.Identity, "newton-meter", allowPrefixes: false, QuantityKind.Torque);

    /// <summary>Foot-pound torque (<c>ft·lbf</c>, 1.3558179483314004 N·m).</summary>
    public static UnitDefinition FootPoundTorque { get; } = new("ft·lbf", Dimension.Energy, UnitConversion.Linear(1.3558179483314004d), "foot-pound", allowPrefixes: false, QuantityKind.Torque);

    /// <summary>Inch-pound torque (<c>in·lbf</c>, 1/12 ft·lbf).</summary>
    public static UnitDefinition InchPoundTorque { get; } = new("in·lbf", Dimension.Energy, UnitConversion.Linear(1.3558179483314004d / 12d), "inch-pound", allowPrefixes: false, QuantityKind.Torque);

    /// <summary>Volt-ampere (<c>VA</c>, apparent power).</summary>
    public static UnitDefinition VoltAmpere { get; } = new("VA", Dimension.Power, UnitConversion.Identity, "volt-ampere", allowPrefixes: true, QuantityKind.ApparentPower);

    /// <summary>Volt-ampere reactive (<c>var</c>, reactive power).</summary>
    public static UnitDefinition Var { get; } = new("var", Dimension.Power, UnitConversion.Identity, "var", allowPrefixes: true, QuantityKind.ReactivePower);

    /// <summary>Mechanical horsepower (<c>hp</c>, 745.69987158227022 W).</summary>
    public static UnitDefinition Horsepower { get; } = new("hp", Dimension.Power, UnitConversion.Linear(745.69987158227022d), "horsepower", allowPrefixes: false, QuantityKind.Power);

    /// <summary>Metric horsepower / Pferdestärke (<c>hp_m</c>, exact 735.49875 W; avoids collision with <c>PS</c> = petasiemens).</summary>
    public static UnitDefinition MetricHorsepower { get; } = new("hp_m", Dimension.Power, UnitConversion.Linear(735.49875d), "metric horsepower", allowPrefixes: false, QuantityKind.Power);

    /// <summary>Ton of refrigeration (<c>ton_ref</c>, 12,000 BTU/h = 3516.8528420667 W; avoids collision with <c>RT</c> = ronnatesla).</summary>
    public static UnitDefinition TonOfRefrigeration { get; } = new("ton_ref", Dimension.Power, UnitConversion.Linear(3516.8528420667d), "ton of refrigeration", allowPrefixes: false, QuantityKind.Power);

    /// <summary>Gauss (<c>G</c>, 10⁻⁴ T — Hall sensor / CGS magnetic flux density; avoids collision with <c>Gs</c> = gigasecond).</summary>
    public static UnitDefinition Gauss { get; } = new("G", Dimension.MagneticFluxDensity, UnitConversion.Linear(1e-4d), "gauss", allowPrefixes: true, QuantityKind.MagneticFluxDensity);

    /// <summary>Oersted (<c>Oe</c>, 1000/(4π) A/m — CGS magnetic field strength).</summary>
    public static UnitDefinition Oersted { get; } = new("Oe", Dimension.MagneticFieldStrength, UnitConversion.Linear(1000d / (4d * Math.PI)), "oersted", allowPrefixes: true, defaultSpec: Isq.MagneticFieldStrength);

    /// <summary>Maxwell (<c>Mx</c>, 10⁻⁸ Wb — CGS magnetic flux).</summary>
    public static UnitDefinition Maxwell { get; } = new("Mx", Dimension.MagneticFlux, UnitConversion.Linear(1e-8d), "maxwell", allowPrefixes: true, QuantityKind.MagneticFlux);

    /// <summary>Poise (<c>P</c>, 0.1 Pa·s — CGS dynamic viscosity; prefixable to <c>cP</c>).</summary>
    public static UnitDefinition Poise { get; } = new("P", Dimension.DynamicViscosity, UnitConversion.Linear(0.1d), "poise", allowPrefixes: true, defaultSpec: Isq.DynamicViscosity);

    /// <summary>Stokes (<c>St</c>, 10⁻⁴ m²/s — CGS kinematic viscosity; prefixable to <c>cSt</c>).</summary>
    public static UnitDefinition Stokes { get; } = new("St", Dimension.KinematicViscosity, UnitConversion.Linear(1e-4d), "stokes", allowPrefixes: true, defaultSpec: Isq.KinematicViscosity);

    /// <summary>Molar concentration (<c>M</c>, 1 mol/L = 1000 mol/m³; prefixable to <c>mM</c>, <c>µM</c>, <c>nM</c>).</summary>
    public static UnitDefinition Molar { get; } = new("M", Dimension.MolarConcentration, UnitConversion.Linear(1000d), "molar", allowPrefixes: true, defaultSpec: Isq.MolarConcentration);

    /// <summary>Curie (<c>Ci</c>, exact 3.7×10¹⁰ Bq).</summary>
    public static UnitDefinition Curie { get; } = new("Ci", Dimension.Frequency, UnitConversion.Linear(3.7e10d), "curie", allowPrefixes: true, defaultSpec: Isq.Activity);

    /// <summary>Rem (<c>rem</c>, 0.01 Sv — radiation dose equivalent).</summary>
    public static UnitDefinition Rem { get; } = new("rem", Dimension.RadiationDose, UnitConversion.Linear(0.01d), "rem", allowPrefixes: true, defaultSpec: Isq.DoseEquivalent);

    /// <summary>Roentgen (<c>R</c>, 2.58×10⁻⁴ C/kg — ionizing radiation exposure).</summary>
    public static UnitDefinition Roentgen { get; } = new("R", Dimension.RadiationExposure, UnitConversion.Linear(2.58e-4d), "roentgen", allowPrefixes: true, defaultSpec: Isq.RadiationExposure);

    /// <summary>All primary units included in <see cref="UnitRegistry.CreateStandard"/>.</summary>
    public static IReadOnlyList<UnitDefinition> AllStandard { get; } =
    [
        // SI base & named derived
        Meter, Kilogram, Gram, Second, Ampere, Kelvin, Mole, Candela,
        One, Radian, Steradian, Hertz, Newton, Pascal, Joule, Watt,
        Coulomb, Volt, Farad, Ohm, Siemens, Weber, Tesla, Henry,
        Celsius, Lumen, Lux, Becquerel, Gray, Sievert, Katal,
        // IEC 80000-13 information & data rate
        Bit, Byte, BitsPerSecond, BytesPerSecond, Baud,
        // Temperature, time & kinematics
        Fahrenheit, Rankine, Minute, Hour, Day, Week, Year,
        Rpm, Knot, MilesPerHour, KilometersPerHour, StandardGravity, Gal,
        // Length, astronomy, area & volume
        Inch, Foot, Yard, Mile, NauticalMile, Mil, Fathom, Chain, Furlong, Angstrom,
        AstronomicalUnit, LightYear, Parsec,
        Hectare, Are, Acre, Barn,
        Liter, GallonUs, GallonImperial, QuartUs, PintUs, CupUs, FluidOunceUs, TablespoonUs, TeaspoonUs, OilBarrel,
        // Mass, force & pressure
        Tonne, Pound, Ounce, Slug, Dalton, ShortTon, LongTon, Stone, Grain, Carat, TroyOunce,
        PoundForce, KilogramForce, Dyne, Kip, OunceForce,
        Bar, Atmosphere, TechnicalAtmosphere, Psi, Ksi, Torr,
        InchOfMercury, InchOfWater, CentimeterOfWater, MillimeterOfWater, Barye,
        // Angles & dimensionless ratios
        Degree, Arcminute, Arcsecond, Gradian, Revolution,
        Percent, Permille, PartsPerMillion, PartsPerBillion, PartsPerTrillion, Neper,
        // Energy, torque, power, magnetism, fluid, chemistry & radiation
        ElectronVolt, WattHour, Calorie, Kilocalorie, Btu, Erg, Therm, AmpereHour,
        NewtonMeter, FootPoundTorque, InchPoundTorque,
        VoltAmpere, Var, Horsepower, MetricHorsepower, TonOfRefrigeration,
        Gauss, Oersted, Maxwell, Poise, Stokes, Molar, Curie, Rem, Roentgen
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
            var d when d == RationalDimension.Illuminance => Lux,
            var d when d == RationalDimension.CatalyticActivity => Katal,
            _ => null
        };
        return unit is not null;
    }
}
