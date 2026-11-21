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
        public readonly bool IsAffine;

        public UnitInfo(string symbol, DimVector dimension, bool allowPrefixes = false, bool isAffine = false)
        {
            Symbol = symbol;
            Dimension = dimension;
            AllowPrefixes = allowPrefixes;
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
        private static readonly DimVector Volume = new DimVector(3, 0, 0, 0, 0, 0, 0);
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

        private static readonly HashSet<string> SingleCharPrefixes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Q", "R", "Y", "Z", "E", "P", "T", "G", "M", "k", "h",
            "d", "c", "m", "µ", "μ", "u", "n", "p", "f", "a", "z", "y", "r", "q"
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
            // Non-SI / Engineering / Imperial
            ["°F"] = new UnitInfo("°F", Temp, allowPrefixes: false, isAffine: true),
            ["°R"] = new UnitInfo("°R", Temp),
            ["min"] = new UnitInfo("min", Time),
            ["h"] = new UnitInfo("h", Time),
            ["d"] = new UnitInfo("d", Time),
            ["in"] = new UnitInfo("in", Length),
            ["ft"] = new UnitInfo("ft", Length),
            ["yd"] = new UnitInfo("yd", Length),
            ["mi"] = new UnitInfo("mi", Length),
            ["mil"] = new UnitInfo("mil", Length),
            ["Å"] = new UnitInfo("Å", Length),
            ["L"] = new UnitInfo("L", Volume, allowPrefixes: true),
            ["gal"] = new UnitInfo("gal", Volume),
            ["t"] = new UnitInfo("t", Mass),
            ["lb"] = new UnitInfo("lb", Mass),
            ["oz"] = new UnitInfo("oz", Mass),
            ["lbf"] = new UnitInfo("lbf", Force),
            ["bar"] = new UnitInfo("bar", Pressure, allowPrefixes: true),
            ["atm"] = new UnitInfo("atm", Pressure),
            ["psi"] = new UnitInfo("psi", Pressure),
            ["Torr"] = new UnitInfo("Torr", Pressure, allowPrefixes: true),
            ["°"] = new UnitInfo("°", None),
            ["rev"] = new UnitInfo("rev", None),
            ["%"] = new UnitInfo("%", None),
            ["ppm"] = new UnitInfo("ppm", None),
            ["ppb"] = new UnitInfo("ppb", None),
            ["eV"] = new UnitInfo("eV", Energy, allowPrefixes: true),
            ["Wh"] = new UnitInfo("Wh", Energy, allowPrefixes: true),
            ["Ah"] = new UnitInfo("Ah", Charge, allowPrefixes: true),
            ["N·m"] = new UnitInfo("N·m", Energy),
            ["VA"] = new UnitInfo("VA", Power, allowPrefixes: true),
            ["var"] = new UnitInfo("var", Power, allowPrefixes: true),
            ["hp"] = new UnitInfo("hp", Power),
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
            ["mmHg"] = new UnitInfo("Torr", Pressure),
            ["Nm"] = new UnitInfo("N·m", Energy)
        };

        private static readonly Dictionary<string, string> UnitsPropertyMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Meter"] = "m", ["Kilogram"] = "kg", ["Gram"] = "g", ["Second"] = "s", ["Ampere"] = "A",
            ["Kelvin"] = "K", ["Mole"] = "mol", ["Candela"] = "cd", ["One"] = "1", ["Radian"] = "rad",
            ["Steradian"] = "sr", ["Hertz"] = "Hz", ["Newton"] = "N", ["Pascal"] = "Pa", ["Joule"] = "J",
            ["Watt"] = "W", ["Coulomb"] = "C", ["Volt"] = "V", ["Farad"] = "F", ["Ohm"] = "Ω",
            ["Siemens"] = "S", ["Weber"] = "Wb", ["Tesla"] = "T", ["Henry"] = "H", ["Celsius"] = "°C",
            ["Fahrenheit"] = "°F", ["Rankine"] = "°R", ["Milliampere"] = "mA", ["Microampere"] = "µA",
            ["Millivolt"] = "mV", ["Microvolt"] = "µV", ["Kilovolt"] = "kV", ["Kiloohm"] = "kΩ",
            ["Megaohm"] = "MΩ", ["Kilohertz"] = "kHz", ["Megahertz"] = "MHz", ["Gigahertz"] = "GHz",
            ["Millisecond"] = "ms", ["Microsecond"] = "µs", ["Nanosecond"] = "ns", ["Microfarad"] = "µF",
            ["Nanofarad"] = "nF", ["Picofarad"] = "pF", ["Millihenry"] = "mH", ["Microhenry"] = "µH",
            ["Milliwatt"] = "mW", ["Kilowatt"] = "kW", ["Minute"] = "min", ["Hour"] = "h", ["Day"] = "d",
            ["Inch"] = "in", ["Foot"] = "ft", ["Yard"] = "yd", ["Mile"] = "mi", ["Mil"] = "mil",
            ["Liter"] = "L", ["Pound"] = "lb", ["Ounce"] = "oz", ["Bar"] = "bar", ["Atmosphere"] = "atm",
            ["Psi"] = "psi", ["Torr"] = "Torr", ["Degree"] = "°", ["Revolution"] = "rev",
            ["Percent"] = "%", ["PartsPerMillion"] = "ppm", ["PartsPerBillion"] = "ppb",
            ["NewtonMeter"] = "N·m", ["VoltAmpere"] = "VA", ["Var"] = "var", ["Horsepower"] = "hp"
        };

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

            if (symbol.Length > 2 && symbol.StartsWith("da", StringComparison.Ordinal))
            {
                string rest = symbol.Substring(2);
                if (StandardUnits.TryGetValue(rest, out var baseInfo) && baseInfo.AllowPrefixes)
                {
                    info = new UnitInfo(symbol, baseInfo.Dimension, allowPrefixes: false, isAffine: false);
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
