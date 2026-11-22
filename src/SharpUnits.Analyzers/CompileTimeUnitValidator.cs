using System;
using System.Collections.Generic;
using System.Globalization;

namespace SharpUnits.Analyzers
{
    internal readonly struct DimRational : IEquatable<DimRational>
    {
        public readonly long Num;
        private readonly long denMinusOne;
        public long Den => denMinusOne + 1;

        public DimRational(long num, long den)
        {
            if (den == 0) throw new DivideByZeroException();
            if (num == 0) { Num = 0; denMinusOne = 0; return; }
            if (den < 0) { num = -num; den = -den; }
            long g = Gcd(Math.Abs(num), den);
            Num = num / g;
            denMinusOne = (den / g) - 1;
        }

        public static DimRational operator +(DimRational a, DimRational b) =>
            new DimRational(a.Num * b.Den + b.Num * a.Den, a.Den * b.Den);

        public static DimRational operator -(DimRational a, DimRational b) =>
            new DimRational(a.Num * b.Den - b.Num * a.Den, a.Den * b.Den);

        public static DimRational operator *(DimRational a, DimRational b) =>
            new DimRational(a.Num * b.Num, a.Den * b.Den);

        public bool Equals(DimRational other) => Num == other.Num && denMinusOne == other.denMinusOne;
        public override bool Equals(object? obj) => obj is DimRational r && Equals(r);
        public override int GetHashCode() => unchecked((int)(Num * 397 ^ denMinusOne));
        public static bool operator ==(DimRational a, DimRational b) => a.Equals(b);
        public static bool operator !=(DimRational a, DimRational b) => !a.Equals(b);

        private static long Gcd(long a, long b)
        {
            while (b != 0) { long t = a % b; a = b; b = t; }
            return a == 0 ? 1 : a;
        }
    }

    internal readonly struct DimVector : IEquatable<DimVector>
    {
        public readonly DimRational L, M, T, I, Th, N, J;

        public DimVector(int l, int m, int t, int i, int th, int n, int j)
            : this(new DimRational(l, 1), new DimRational(m, 1), new DimRational(t, 1),
                   new DimRational(i, 1), new DimRational(th, 1), new DimRational(n, 1), new DimRational(j, 1))
        {
        }

        public DimVector(DimRational l, DimRational m, DimRational t, DimRational i, DimRational th, DimRational n, DimRational j)
        {
            L = l; M = m; T = t; I = i; Th = th; N = n; J = j;
        }

        public static DimVector operator +(DimVector a, DimVector b) =>
            new DimVector(a.L + b.L, a.M + b.M, a.T + b.T, a.I + b.I, a.Th + b.Th, a.N + b.N, a.J + b.J);

        public static DimVector operator -(DimVector a, DimVector b) =>
            new DimVector(a.L - b.L, a.M - b.M, a.T - b.T, a.I - b.I, a.Th - b.Th, a.N - b.N, a.J - b.J);

        public DimVector Pow(DimRational exp) =>
            new DimVector(L * exp, M * exp, T * exp, I * exp, Th * exp, N * exp, J * exp);

        public bool Equals(DimVector o) =>
            L == o.L && M == o.M && T == o.T && I == o.I && Th == o.Th && N == o.N && J == o.J;

        public override bool Equals(object? obj) => obj is DimVector v && Equals(v);
        public override int GetHashCode() => L.GetHashCode() ^ M.GetHashCode() ^ T.GetHashCode() ^ I.GetHashCode() ^ Th.GetHashCode();
        public static bool operator ==(DimVector a, DimVector b) => a.Equals(b);
        public static bool operator !=(DimVector a, DimVector b) => !a.Equals(b);
    }

    internal readonly struct UnitInfo
    {
        public readonly string Symbol;
        public readonly DimVector Dimension;
        public readonly bool AllowPrefixes;
        public readonly bool AllowBinaryPrefixes;
        public readonly bool IsAffine;

        public UnitInfo(string symbol, DimVector dimension, bool allowPrefixes = false, bool isAffine = false, bool allowBinaryPrefixes = false)
        {
            Symbol = symbol;
            Dimension = dimension;
            AllowPrefixes = allowPrefixes;
            AllowBinaryPrefixes = allowBinaryPrefixes;
            IsAffine = isAffine;
        }
    }

    internal static class CompileTimeUnitValidator
    {
        private static readonly DimVector None = new DimVector(0, 0, 0, 0, 0, 0, 0);
        private static readonly DimVector Length = new DimVector(1, 0, 0, 0, 0, 0, 0);
        private static readonly DimVector Mass = new DimVector(0, 1, 0, 0, 0, 0, 0);
        private static readonly DimVector Time = new DimVector(0, 0, 1, 0, 0, 0, 0);
        private static readonly DimVector Current = new DimVector(0, 0, 0, 1, 0, 0, 0);
        private static readonly DimVector Temp = new DimVector(0, 0, 0, 0, 1, 0, 0);
        private static readonly DimVector Amount = new DimVector(0, 0, 0, 0, 0, 1, 0);
        private static readonly DimVector Lum = new DimVector(0, 0, 0, 0, 0, 0, 1);
        private static readonly DimVector Area = new DimVector(2, 0, 0, 0, 0, 0, 0);
        private static readonly DimVector Volume = new DimVector(3, 0, 0, 0, 0, 0, 0);
        private static readonly DimVector Velocity = new DimVector(1, 0, -1, 0, 0, 0, 0);
        private static readonly DimVector Acceleration = new DimVector(1, 0, -2, 0, 0, 0, 0);
        private static readonly DimVector Freq = new DimVector(0, 0, -1, 0, 0, 0, 0);
        private static readonly DimVector Force = new DimVector(1, 1, -2, 0, 0, 0, 0);
        private static readonly DimVector Pressure = new DimVector(-1, 1, -2, 0, 0, 0, 0);
        private static readonly DimVector Energy = new DimVector(2, 1, -2, 0, 0, 0, 0);
        private static readonly DimVector Power = new DimVector(2, 1, -3, 0, 0, 0, 0);
        private static readonly DimVector Charge = new DimVector(0, 0, 1, 1, 0, 0, 0);
        private static readonly DimVector Voltage = new DimVector(2, 1, -3, -1, 0, 0, 0);
        private static readonly DimVector Capacitance = new DimVector(-2, -1, 4, 2, 0, 0, 0);
        private static readonly DimVector Resistance = new DimVector(2, 1, -3, -2, 0, 0, 0);
        private static readonly DimVector Conductance = new DimVector(-2, -1, 3, 2, 0, 0, 0);
        private static readonly DimVector Flux = new DimVector(2, 1, -2, -1, 0, 0, 0);
        private static readonly DimVector FluxDensity = new DimVector(0, 1, -2, -1, 0, 0, 0);
        private static readonly DimVector Inductance = new DimVector(2, 1, -2, -2, 0, 0, 0);
        private static readonly DimVector Illuminance = new DimVector(-2, 0, 0, 0, 0, 0, 1);
        private static readonly DimVector Dose = new DimVector(2, 0, -2, 0, 0, 0, 0);
        private static readonly DimVector Catalytic = new DimVector(0, 0, -1, 0, 0, 1, 0);
        private static readonly DimVector DynamicViscosity = new DimVector(-1, 1, -1, 0, 0, 0, 0);
        private static readonly DimVector KinematicViscosity = new DimVector(2, 0, -1, 0, 0, 0, 0);
        private static readonly DimVector MagneticField = new DimVector(-1, 0, 0, 1, 0, 0, 0);
        private static readonly DimVector MolarConcentration = new DimVector(-3, 0, 0, 0, 0, 1, 0);
        private static readonly DimVector RadiationExposure = new DimVector(0, -1, 1, 1, 0, 0, 0);

        private static readonly HashSet<string> SingleCharPrefixes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Q", "R", "Y", "Z", "E", "P", "T", "G", "M", "k", "h",
            "d", "c", "m", "µ", "μ", "u", "n", "p", "f", "a", "z", "y", "r", "q"
        };

        private static readonly HashSet<string> IecBinaryPrefixes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Ki", "Mi", "Gi", "Ti", "Pi", "Ei", "Zi", "Yi"
        };

        private static readonly Dictionary<string, UnitInfo> StandardUnits = new Dictionary<string, UnitInfo>(StringComparer.Ordinal)
        {
            // SI Base
            ["m"] = new UnitInfo("m", Length, allowPrefixes: true),
            ["kg"] = new UnitInfo("kg", Mass, allowPrefixes: false),
            ["g"] = new UnitInfo("g", Mass, allowPrefixes: true),
            ["s"] = new UnitInfo("s", Time, allowPrefixes: true),
            ["A"] = new UnitInfo("A", Current, allowPrefixes: true),
            ["K"] = new UnitInfo("K", Temp, allowPrefixes: true),
            ["mol"] = new UnitInfo("mol", Amount, allowPrefixes: true),
            ["cd"] = new UnitInfo("cd", Lum, allowPrefixes: true),
            // SI Named Derived
            ["1"] = new UnitInfo("1", None),
            ["rad"] = new UnitInfo("rad", None, allowPrefixes: true),
            ["sr"] = new UnitInfo("sr", None, allowPrefixes: true),
            ["Hz"] = new UnitInfo("Hz", Freq, allowPrefixes: true),
            ["N"] = new UnitInfo("N", Force, allowPrefixes: true),
            ["Pa"] = new UnitInfo("Pa", Pressure, allowPrefixes: true),
            ["J"] = new UnitInfo("J", Energy, allowPrefixes: true),
            ["W"] = new UnitInfo("W", Power, allowPrefixes: true),
            ["C"] = new UnitInfo("C", Charge, allowPrefixes: true),
            ["V"] = new UnitInfo("V", Voltage, allowPrefixes: true),
            ["F"] = new UnitInfo("F", Capacitance, allowPrefixes: true),
            ["Ω"] = new UnitInfo("Ω", Resistance, allowPrefixes: true),
            ["S"] = new UnitInfo("S", Conductance, allowPrefixes: true),
            ["Wb"] = new UnitInfo("Wb", Flux, allowPrefixes: true),
            ["T"] = new UnitInfo("T", FluxDensity, allowPrefixes: true),
            ["H"] = new UnitInfo("H", Inductance, allowPrefixes: true),
            ["°C"] = new UnitInfo("°C", Temp, allowPrefixes: false, isAffine: true),
            ["lm"] = new UnitInfo("lm", Lum, allowPrefixes: true),
            ["lx"] = new UnitInfo("lx", Illuminance, allowPrefixes: true),
            ["Bq"] = new UnitInfo("Bq", Freq, allowPrefixes: true),
            ["Gy"] = new UnitInfo("Gy", Dose, allowPrefixes: true),
            ["Sv"] = new UnitInfo("Sv", Dose, allowPrefixes: true),
            ["kat"] = new UnitInfo("kat", Catalytic, allowPrefixes: true),
            // IEC 80000-13 Information & Data Rate
            ["bit"] = new UnitInfo("bit", None, allowPrefixes: true, allowBinaryPrefixes: true),
            ["B"] = new UnitInfo("B", None, allowPrefixes: true, allowBinaryPrefixes: true),
            ["bps"] = new UnitInfo("bps", Freq, allowPrefixes: true, allowBinaryPrefixes: true),
            ["Bps"] = new UnitInfo("Bps", Freq, allowPrefixes: true, allowBinaryPrefixes: true),
            ["Bd"] = new UnitInfo("Bd", Freq, allowPrefixes: true),
            // Temperature, Time & Kinematics
            ["°F"] = new UnitInfo("°F", Temp, allowPrefixes: false, isAffine: true),
            ["°R"] = new UnitInfo("°R", Temp),
            ["min"] = new UnitInfo("min", Time),
            ["h"] = new UnitInfo("h", Time),
            ["d"] = new UnitInfo("d", Time),
            ["wk"] = new UnitInfo("wk", Time),
            ["yr"] = new UnitInfo("yr", Time),
            ["rpm"] = new UnitInfo("rpm", Freq),
            ["kn"] = new UnitInfo("kn", Velocity),
            ["mph"] = new UnitInfo("mph", Velocity),
            ["kph"] = new UnitInfo("kph", Velocity),
            ["g0"] = new UnitInfo("g0", Acceleration),
            ["Gal"] = new UnitInfo("Gal", Acceleration, allowPrefixes: true),
            // Length, Astronomy, Area & Volume
            ["in"] = new UnitInfo("in", Length),
            ["ft"] = new UnitInfo("ft", Length),
            ["yd"] = new UnitInfo("yd", Length),
            ["mi"] = new UnitInfo("mi", Length),
            ["nmi"] = new UnitInfo("nmi", Length),
            ["mil"] = new UnitInfo("mil", Length),
            ["fathom"] = new UnitInfo("fathom", Length),
            ["ch"] = new UnitInfo("ch", Length),
            ["furlong"] = new UnitInfo("furlong", Length),
            ["Å"] = new UnitInfo("Å", Length),
            ["au"] = new UnitInfo("au", Length),
            ["ly"] = new UnitInfo("ly", Length),
            ["pc"] = new UnitInfo("pc", Length, allowPrefixes: true),
            ["ha"] = new UnitInfo("ha", Area),
            ["are"] = new UnitInfo("are", Area),
            ["ac"] = new UnitInfo("ac", Area),
            ["barn"] = new UnitInfo("barn", Area, allowPrefixes: true),
            ["L"] = new UnitInfo("L", Volume, allowPrefixes: true),
            ["gal"] = new UnitInfo("gal", Volume),
            ["gal_uk"] = new UnitInfo("gal_uk", Volume),
            ["qt"] = new UnitInfo("qt", Volume),
            ["pt"] = new UnitInfo("pt", Volume),
            ["cup"] = new UnitInfo("cup", Volume),
            ["floz"] = new UnitInfo("floz", Volume),
            ["tbsp"] = new UnitInfo("tbsp", Volume),
            ["tsp"] = new UnitInfo("tsp", Volume),
            ["bbl"] = new UnitInfo("bbl", Volume),
            // Mass, Force & Pressure
            ["t"] = new UnitInfo("t", Mass),
            ["lb"] = new UnitInfo("lb", Mass),
            ["oz"] = new UnitInfo("oz", Mass),
            ["slug"] = new UnitInfo("slug", Mass),
            ["Da"] = new UnitInfo("Da", Mass, allowPrefixes: true),
            ["ton_us"] = new UnitInfo("ton_us", Mass),
            ["ton_uk"] = new UnitInfo("ton_uk", Mass),
            ["st"] = new UnitInfo("st", Mass),
            ["gr"] = new UnitInfo("gr", Mass),
            ["ct"] = new UnitInfo("ct", Mass),
            ["ozt"] = new UnitInfo("ozt", Mass),
            ["lbf"] = new UnitInfo("lbf", Force),
            ["kgf"] = new UnitInfo("kgf", Force),
            ["dyn"] = new UnitInfo("dyn", Force, allowPrefixes: true),
            ["kip"] = new UnitInfo("kip", Force),
            ["ozf"] = new UnitInfo("ozf", Force),
            ["bar"] = new UnitInfo("bar", Pressure, allowPrefixes: true),
            ["atm"] = new UnitInfo("atm", Pressure),
            ["at"] = new UnitInfo("at", Pressure),
            ["psi"] = new UnitInfo("psi", Pressure),
            ["ksi"] = new UnitInfo("ksi", Pressure),
            ["Torr"] = new UnitInfo("Torr", Pressure, allowPrefixes: true),
            ["inHg"] = new UnitInfo("inHg", Pressure),
            ["inH2O"] = new UnitInfo("inH2O", Pressure),
            ["cmH2O"] = new UnitInfo("cmH2O", Pressure),
            ["mmH2O"] = new UnitInfo("mmH2O", Pressure),
            ["Ba"] = new UnitInfo("Ba", Pressure, allowPrefixes: true),
            // Angles & Dimensionless
            ["°"] = new UnitInfo("°", None),
            ["arcmin"] = new UnitInfo("arcmin", None),
            ["arcsec"] = new UnitInfo("arcsec", None),
            ["grad"] = new UnitInfo("grad", None),
            ["rev"] = new UnitInfo("rev", None),
            ["%"] = new UnitInfo("%", None),
            ["‰"] = new UnitInfo("‰", None),
            ["ppm"] = new UnitInfo("ppm", None),
            ["ppb"] = new UnitInfo("ppb", None),
            ["ppt"] = new UnitInfo("ppt", None),
            ["Np"] = new UnitInfo("Np", None, allowPrefixes: true),
            // Energy, Torque, Power, Magnetism, Viscosity, Chemistry & Radiation
            ["eV"] = new UnitInfo("eV", Energy, allowPrefixes: true),
            ["Wh"] = new UnitInfo("Wh", Energy, allowPrefixes: true),
            ["cal"] = new UnitInfo("cal", Energy, allowPrefixes: true),
            ["Cal"] = new UnitInfo("Cal", Energy),
            ["BTU"] = new UnitInfo("BTU", Energy),
            ["erg"] = new UnitInfo("erg", Energy, allowPrefixes: true),
            ["therm"] = new UnitInfo("therm", Energy),
            ["Ah"] = new UnitInfo("Ah", Charge, allowPrefixes: true),
            ["N·m"] = new UnitInfo("N·m", Energy),
            ["ft·lbf"] = new UnitInfo("ft·lbf", Energy),
            ["in·lbf"] = new UnitInfo("in·lbf", Energy),
            ["VA"] = new UnitInfo("VA", Power, allowPrefixes: true),
            ["var"] = new UnitInfo("var", Power, allowPrefixes: true),
            ["hp"] = new UnitInfo("hp", Power),
            ["hp_m"] = new UnitInfo("hp_m", Power),
            ["ton_ref"] = new UnitInfo("ton_ref", Power),
            ["G"] = new UnitInfo("G", FluxDensity, allowPrefixes: true),
            ["Oe"] = new UnitInfo("Oe", MagneticField, allowPrefixes: true),
            ["Mx"] = new UnitInfo("Mx", Flux, allowPrefixes: true),
            ["P"] = new UnitInfo("P", DynamicViscosity, allowPrefixes: true),
            ["St"] = new UnitInfo("St", KinematicViscosity, allowPrefixes: true),
            ["M"] = new UnitInfo("M", MolarConcentration, allowPrefixes: true),
            ["Ci"] = new UnitInfo("Ci", Freq, allowPrefixes: true),
            ["rem"] = new UnitInfo("rem", Dose, allowPrefixes: true),
            ["R"] = new UnitInfo("R", RadiationExposure, allowPrefixes: true),
            // Aliases
            ["Ohm"] = new UnitInfo("Ω", Resistance, allowPrefixes: true),
            ["ohm"] = new UnitInfo("Ω", Resistance, allowPrefixes: true),
            ["degC"] = new UnitInfo("°C", Temp, allowPrefixes: false, isAffine: true),
            ["degF"] = new UnitInfo("°F", Temp, allowPrefixes: false, isAffine: true),
            ["degR"] = new UnitInfo("°R", Temp),
            ["deg"] = new UnitInfo("°", None),
            ["hr"] = new UnitInfo("h", Time),
            ["sec"] = new UnitInfo("s", Time, allowPrefixes: true),
            ["l"] = new UnitInfo("L", Volume, allowPrefixes: true),
            ["cc"] = new UnitInfo("mL", Volume),
            ["mmHg"] = new UnitInfo("Torr", Pressure, allowPrefixes: true),
            ["Nm"] = new UnitInfo("N·m", Energy),
            ["ftlbf"] = new UnitInfo("ft·lbf", Energy),
            ["inlbf"] = new UnitInfo("in·lbf", Energy),
            ["thou"] = new UnitInfo("mil", Length),
            ["micron"] = new UnitInfo("µm", Length),
            ["NM"] = new UnitInfo("nmi", Length),
            ["kt"] = new UnitInfo("kn", Velocity),
            ["kmh"] = new UnitInfo("kph", Velocity),
            ["gn"] = new UnitInfo("g0", Acceleration),
            ["gon"] = new UnitInfo("grad", None),
            ["turn"] = new UnitInfo("rev", None),
            ["′"] = new UnitInfo("arcmin", None),
            ["″"] = new UnitInfo("arcsec", None),
            ["amu"] = new UnitInfo("Da", Mass, allowPrefixes: true),
            ["kp"] = new UnitInfo("kgf", Force),
            ["Btu"] = new UnitInfo("BTU", Energy),
            ["gauss"] = new UnitInfo("G", FluxDensity, allowPrefixes: true),
            ["CV"] = new UnitInfo("hp_m", Power),
            ["permille"] = new UnitInfo("‰", None),
            ["o"] = new UnitInfo("B", None, allowPrefixes: true, allowBinaryPrefixes: true)
        };

        private static readonly Dictionary<string, string> UnitsPropertyMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Meter"] = "m", ["Kilogram"] = "kg", ["Gram"] = "g", ["Second"] = "s", ["Ampere"] = "A",
            ["Kelvin"] = "K", ["Mole"] = "mol", ["Candela"] = "cd", ["One"] = "1", ["Radian"] = "rad",
            ["Steradian"] = "sr", ["Hertz"] = "Hz", ["Newton"] = "N", ["Pascal"] = "Pa", ["Joule"] = "J",
            ["Watt"] = "W", ["Coulomb"] = "C", ["Volt"] = "V", ["Farad"] = "F", ["Ohm"] = "Ω",
            ["Siemens"] = "S", ["Weber"] = "Wb", ["Tesla"] = "T", ["Henry"] = "H", ["Celsius"] = "°C",
            ["Lumen"] = "lm", ["Lux"] = "lx", ["Becquerel"] = "Bq", ["Gray"] = "Gy", ["Sievert"] = "Sv", ["Katal"] = "kat",
            ["Fahrenheit"] = "°F", ["Rankine"] = "°R", ["Milliampere"] = "mA", ["Microampere"] = "µA",
            ["Millivolt"] = "mV", ["Microvolt"] = "µV", ["Kilovolt"] = "kV", ["Kiloohm"] = "kΩ",
            ["Megaohm"] = "MΩ", ["Kilohertz"] = "kHz", ["Megahertz"] = "MHz", ["Gigahertz"] = "GHz",
            ["Millisecond"] = "ms", ["Microsecond"] = "µs", ["Nanosecond"] = "ns", ["Microfarad"] = "µF",
            ["Nanofarad"] = "nF", ["Picofarad"] = "pF", ["Millihenry"] = "mH", ["Microhenry"] = "µH",
            ["Milliwatt"] = "mW", ["Kilowatt"] = "kW", ["Micrometer"] = "µm", ["Milliliter"] = "mL",
            ["Bit"] = "bit", ["Byte"] = "B", ["BitsPerSecond"] = "bps", ["BytesPerSecond"] = "Bps", ["Baud"] = "Bd",
            ["Minute"] = "min", ["Hour"] = "h", ["Day"] = "d", ["Week"] = "wk", ["Year"] = "yr",
            ["Rpm"] = "rpm", ["Knot"] = "kn", ["MilesPerHour"] = "mph", ["KilometersPerHour"] = "kph",
            ["StandardGravity"] = "g0", ["Gal"] = "Gal",
            ["Inch"] = "in", ["Foot"] = "ft", ["Yard"] = "yd", ["Mile"] = "mi", ["NauticalMile"] = "nmi",
            ["Mil"] = "mil", ["Fathom"] = "fathom", ["Chain"] = "ch", ["Furlong"] = "furlong", ["Angstrom"] = "Å",
            ["AstronomicalUnit"] = "au", ["LightYear"] = "ly", ["Parsec"] = "pc",
            ["Hectare"] = "ha", ["Are"] = "are", ["Acre"] = "ac", ["Barn"] = "barn",
            ["Liter"] = "L", ["GallonUs"] = "gal", ["GallonImperial"] = "gal_uk", ["QuartUs"] = "qt",
            ["PintUs"] = "pt", ["CupUs"] = "cup", ["FluidOunceUs"] = "floz", ["TablespoonUs"] = "tbsp",
            ["TeaspoonUs"] = "tsp", ["OilBarrel"] = "bbl",
            ["Tonne"] = "t", ["Pound"] = "lb", ["Ounce"] = "oz", ["Slug"] = "slug", ["Dalton"] = "Da",
            ["ShortTon"] = "ton_us", ["LongTon"] = "ton_uk", ["Stone"] = "st", ["Grain"] = "gr",
            ["Carat"] = "ct", ["TroyOunce"] = "ozt",
            ["PoundForce"] = "lbf", ["KilogramForce"] = "kgf", ["Dyne"] = "dyn", ["Kip"] = "kip", ["OunceForce"] = "ozf",
            ["Bar"] = "bar", ["Atmosphere"] = "atm", ["TechnicalAtmosphere"] = "at", ["Psi"] = "psi",
            ["Ksi"] = "ksi", ["Torr"] = "Torr", ["InchOfMercury"] = "inHg", ["InchOfWater"] = "inH2O",
            ["CentimeterOfWater"] = "cmH2O", ["MillimeterOfWater"] = "mmH2O", ["Barye"] = "Ba",
            ["Degree"] = "°", ["Arcminute"] = "arcmin", ["Arcsecond"] = "arcsec", ["Gradian"] = "grad",
            ["Revolution"] = "rev", ["Percent"] = "%", ["Permille"] = "‰",
            ["PartsPerMillion"] = "ppm", ["PartsPerBillion"] = "ppb", ["PartsPerTrillion"] = "ppt", ["Neper"] = "Np",
            ["ElectronVolt"] = "eV", ["WattHour"] = "Wh", ["Calorie"] = "cal", ["Kilocalorie"] = "Cal",
            ["Btu"] = "BTU", ["Erg"] = "erg", ["Therm"] = "therm", ["AmpereHour"] = "Ah",
            ["NewtonMeter"] = "N·m", ["FootPoundTorque"] = "ft·lbf", ["InchPoundTorque"] = "in·lbf",
            ["VoltAmpere"] = "VA", ["Var"] = "var", ["Horsepower"] = "hp", ["MetricHorsepower"] = "hp_m",
            ["TonOfRefrigeration"] = "ton_ref", ["Gauss"] = "G", ["Oersted"] = "Oe", ["Maxwell"] = "Mx",
            ["Poise"] = "P", ["Stokes"] = "St", ["Molar"] = "M", ["Curie"] = "Ci", ["Rem"] = "rem", ["Roentgen"] = "R"
        };

        private static readonly string[] CommonUnitSuggestions =
        {
            "m", "mm", "cm", "km", "in", "ft", "mi", "nmi", "mil",
            "kg", "g", "mg", "lb", "oz", "slug", "Da",
            "s", "ms", "µs", "us", "ns", "min", "h", "hr", "rpm", "g0", "kn", "mph", "kph",
            "A", "mA", "µA", "uA", "nA",
            "V", "mV", "µV", "uV", "kV", "MV",
            "Ω", "kΩ", "MΩ", "Ohm", "kOhm", "MOhm",
            "F", "µF", "uF", "nF", "pF",
            "H", "mH", "µH", "uH", "nH",
            "Hz", "kHz", "MHz", "GHz",
            "W", "mW", "kW", "MW", "hp",
            "J", "kJ", "MJ", "Wh", "kWh", "eV", "cal", "kcal", "BTU",
            "N", "kN", "lbf", "kgf", "N·m", "Nm", "ft·lbf",
            "Pa", "kPa", "MPa", "GPa", "bar", "mbar", "atm", "psi", "ksi", "Torr", "inHg", "inH2O",
            "K", "°C", "degC", "°F", "degF", "°R",
            "rad", "deg", "°", "arcmin", "arcsec", "rev", "%", "ppm", "ppb", "ppt", "L", "mL", "cc",
            "VA", "var", "Gs", "G", "cP", "cSt", "M", "mM",
            "bit", "B", "kB", "MB", "GB", "KiB", "MiB", "GiB", "TiB", "bps", "kbps", "Mbps", "Gbps"
        };

        public static string? SuggestClosestUnit(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length > 16) return null;
            string trimmed = input.Trim();
            string? best = null;
            int bestDist = 3; // Only suggest within edit distance <= 2
            foreach (string candidate in CommonUnitSuggestions)
            {
                int d = Levenshtein(trimmed, candidate);
                if (d < bestDist && d > 0)
                {
                    bestDist = d;
                    best = candidate;
                }
            }
            return best;
        }

        private static int Levenshtein(string a, string b)
        {
            int n = a.Length, m = b.Length;
            if (Math.Abs(n - m) > 2) return 3;
            var prev = new int[m + 1];
            var curr = new int[m + 1];
            for (int j = 0; j <= m; j++) prev[j] = j;
            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[m];
        }

        public static bool TryResolveUnitsProperty(string propertyName, out UnitInfo info)
        {
            if (UnitsPropertyMap.TryGetValue(propertyName, out string? symbol))
                return TryResolveSingle(symbol, out info);
            info = default;
            return false;
        }

        public static bool TryResolveSingle(string symbol, out UnitInfo info)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                info = default;
                return false;
            }
            if (StandardUnits.TryGetValue(symbol, out info))
                return true;

            if (symbol.Length > 2)
            {
                string twoChar = symbol.Substring(0, 2);
                string rest = symbol.Substring(2);
                if (IecBinaryPrefixes.Contains(twoChar) &&
                    StandardUnits.TryGetValue(rest, out var binBase) &&
                    binBase.AllowBinaryPrefixes)
                {
                    info = new UnitInfo(symbol, binBase.Dimension, allowPrefixes: false, isAffine: false);
                    return true;
                }
                if (twoChar == "da" &&
                    StandardUnits.TryGetValue(rest, out var daBase) &&
                    daBase.AllowPrefixes)
                {
                    info = new UnitInfo(symbol, daBase.Dimension, allowPrefixes: false, isAffine: false);
                    return true;
                }
            }

            if (symbol.Length > 1)
            {
                string prefix = symbol.Substring(0, 1);
                string rest = symbol.Substring(1);
                if (SingleCharPrefixes.Contains(prefix) &&
                    StandardUnits.TryGetValue(rest, out var baseInfo) &&
                    baseInfo.AllowPrefixes)
                {
                    info = new UnitInfo(symbol, baseInfo.Dimension, allowPrefixes: false, isAffine: false);
                    return true;
                }
            }

            info = default;
            return false;
        }

        public static bool TryValidateUnitExpression(
            string text,
            bool allowCompound,
            out UnitInfo info,
            out string? error,
            out bool affineInCompound)
        {
            affineInCompound = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                info = default;
                error = "Unit literal cannot be empty.";
                return false;
            }
            string trimmed = text.Trim();
            if (TryResolveSingle(trimmed, out info))
            {
                error = null;
                return true;
            }
            if (!allowCompound)
            {
                error = $"Unknown unit symbol '{trimmed}'.";
                return false;
            }
            return TryParseCompound(trimmed, out info, out error, out affineInCompound);
        }

        public static bool TryValidateQuantityLiteral(
            string text,
            out UnitInfo info,
            out bool isRelative,
            out string? error,
            out bool affineInCompound)
        {
            info = default;
            isRelative = false;
            affineInCompound = false;

            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Expected '<number> <unit>'.";
                return false;
            }
            string trimmed = text.Trim();
            int numEnd = ScanNumberEnd(trimmed);
            if (numEnd <= 0 || numEnd >= trimmed.Length)
            {
                error = "Expected '<number> <unit>'.";
                return false;
            }
            string numPart = trimmed.Substring(0, numEnd);
            string unitPart = trimmed.Substring(numEnd).Trim();
            if (unitPart.Length == 0 ||
                !double.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ||
                double.IsNaN(v) || double.IsInfinity(v))
            {
                error = "Invalid numeric magnitude.";
                return false;
            }

            if (unitPart[0] == 'Δ')
            {
                isRelative = true;
                unitPart = unitPart.Substring(1).TrimStart();
            }

            if (TryResolveSingle(unitPart, out info))
            {
                error = null;
                return true;
            }

            if (ContainsOperator(unitPart))
                return TryParseCompound(unitPart, out info, out error, out affineInCompound);

            error = $"Unknown unit symbol '{unitPart}'.";
            return false;
        }

        private static bool TryParseCompound(string input, out UnitInfo info, out string? error, out bool affineInCompound)
        {
            int pos = 0;
            affineInCompound = false;
            if (!ParseProduct(input, ref pos, out var dim, out error, out affineInCompound))
            {
                info = default;
                return false;
            }
            SkipSpace(input, ref pos);
            if (pos != input.Length)
            {
                info = default;
                error = $"Unexpected character '{input[pos]}' at position {pos}.";
                return false;
            }
            info = new UnitInfo(input, dim, allowPrefixes: false, isAffine: false);
            error = null;
            return true;
        }

        private static bool ParseProduct(string input, ref int pos, out DimVector result, out string? error, out bool affineInCompound)
        {
            if (!ParseFactor(input, ref pos, out result, out error, out affineInCompound))
                return false;
            while (true)
            {
                SkipSpace(input, ref pos);
                if (pos >= input.Length || input[pos] == ')') return true;
                char op = input[pos];
                if (op != '*' && op != '/' && op != '·') return true;
                pos++;
                if (!ParseFactor(input, ref pos, out var rhs, out error, out affineInCompound))
                    return false;
                result = op == '/' ? result - rhs : result + rhs;
            }
        }

        private static bool ParseFactor(string input, ref int pos, out DimVector result, out string? error, out bool affineInCompound)
        {
            SkipSpace(input, ref pos);
            affineInCompound = false;
            if (pos < input.Length && input[pos] == '(')
            {
                pos++;
                if (!ParseProduct(input, ref pos, out result, out error, out affineInCompound)) return false;
                SkipSpace(input, ref pos);
                if (pos >= input.Length || input[pos] != ')')
                {
                    error = "Missing closing parenthesis.";
                    return false;
                }
                pos++;
            }
            else
            {
                int start = pos;
                while (pos < input.Length && !IsDelimiter(input[pos]) && !IsSuperscript(input[pos])) pos++;
                if (pos == start)
                {
                    result = default;
                    error = "Expected a unit symbol.";
                    return false;
                }
                string token = input.Substring(start, pos - start);
                if (token == "1")
                {
                    result = None;
                }
                else if (TryResolveSingle(token, out var single))
                {
                    if (single.IsAffine)
                    {
                        result = default;
                        affineInCompound = true;
                        error = $"Affine unit '{token}' cannot participate in multiplicative unit expressions.";
                        return false;
                    }
                    result = single.Dimension;
                }
                else
                {
                    result = default;
                    error = $"Unknown unit symbol '{token}'.";
                    return false;
                }
            }

            SkipSpace(input, ref pos);
            if (pos < input.Length && input[pos] == '^')
            {
                pos++;
                SkipSpace(input, ref pos);
                if (!ParseExponent(input, ref pos, out var exp, out error))
                    return false;
                result = result.Pow(exp);
            }
            else if (pos < input.Length && IsSuperscript(input[pos]))
            {
                if (!ParseSuperscript(input, ref pos, out int power, out error))
                    return false;
                result = result.Pow(new DimRational(power, 1));
            }

            error = null;
            return true;
        }

        private static bool ParseExponent(string input, ref int pos, out DimRational exp, out string? error)
        {
            if (pos < input.Length && input[pos] == '(')
            {
                pos++;
                SkipSpace(input, ref pos);
                if (!ParseSignedLong(input, ref pos, out long num))
                {
                    exp = default;
                    error = "Invalid unit exponent.";
                    return false;
                }
                SkipSpace(input, ref pos);
                long den = 1;
                if (pos < input.Length && input[pos] == '/')
                {
                    pos++;
                    SkipSpace(input, ref pos);
                    if (!ParseSignedLong(input, ref pos, out den) || den == 0)
                    {
                        exp = default;
                        error = "Invalid rational unit exponent denominator.";
                        return false;
                    }
                    SkipSpace(input, ref pos);
                }
                if (pos >= input.Length || input[pos] != ')')
                {
                    exp = default;
                    error = "Missing closing parenthesis in unit exponent.";
                    return false;
                }
                pos++;
                exp = new DimRational(num, den);
                error = null;
                return true;
            }
            if (!ParseSignedLong(input, ref pos, out long value))
            {
                exp = default;
                error = "Invalid integer unit exponent.";
                return false;
            }
            exp = new DimRational(value, 1);
            error = null;
            return true;
        }

        private static bool ParseSignedLong(string input, ref int pos, out long value)
        {
            int start = pos;
            if (pos < input.Length && (input[pos] == '-' || input[pos] == '+')) pos++;
            while (pos < input.Length && input[pos] >= '0' && input[pos] <= '9') pos++;
            return long.TryParse(input.Substring(start, pos - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool ParseSuperscript(string input, ref int pos, out int value, out string? error)
        {
            bool neg = false;
            if (input[pos] == '⁻') { neg = true; pos++; }
            else if (input[pos] == '⁺') { pos++; }
            int digits = 0;
            value = 0;
            while (pos < input.Length && TryDecodeSuperscript(input[pos], out int d))
            {
                value = value * 10 + d;
                digits++;
                pos++;
            }
            if (digits == 0)
            {
                error = "Invalid superscript exponent.";
                return false;
            }
            if (neg) value = -value;
            error = null;
            return true;
        }

        private static int ScanNumberEnd(string s)
        {
            int i = 0;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
            bool digits = false;
            while (i < s.Length && s[i] >= '0' && s[i] <= '9') { i++; digits = true; }
            if (i < s.Length && s[i] == '.')
            {
                i++;
                while (i < s.Length && s[i] >= '0' && s[i] <= '9') { i++; digits = true; }
            }
            if (!digits) return 0;
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                int expStart = i;
                int j = i + 1;
                if (j < s.Length && (s[j] == '+' || s[j] == '-')) j++;
                int digitStart = j;
                while (j < s.Length && s[j] >= '0' && s[j] <= '9') j++;
                i = j > digitStart ? j : expStart;
            }
            return i;
        }

        private static bool ContainsOperator(string s)
        {
            foreach (char c in s)
                if (c == '*' || c == '/' || c == '·' || c == '^' || c == '(' || c == ')' || IsSuperscript(c))
                    return true;
            return false;
        }

        private static void SkipSpace(string input, ref int pos)
        {
            while (pos < input.Length && char.IsWhiteSpace(input[pos])) pos++;
        }

        private static bool IsDelimiter(char c) =>
            char.IsWhiteSpace(c) || c == '*' || c == '/' || c == '·' || c == '^' || c == '(' || c == ')';

        private static bool IsSuperscript(char c) =>
            c == '⁰' || c == '¹' || c == '²' || c == '³' || c == '⁴' ||
            c == '⁵' || c == '⁶' || c == '⁷' || c == '⁸' || c == '⁹' || c == '⁺' || c == '⁻';

        private static bool TryDecodeSuperscript(char c, out int digit)
        {
            switch (c)
            {
                case '⁰': digit = 0; return true;
                case '¹': digit = 1; return true;
                case '²': digit = 2; return true;
                case '³': digit = 3; return true;
                case '⁴': digit = 4; return true;
                case '⁵': digit = 5; return true;
                case '⁶': digit = 6; return true;
                case '⁷': digit = 7; return true;
                case '⁸': digit = 8; return true;
                case '⁹': digit = 9; return true;
                default: digit = -1; return false;
            }
        }
    }
}
