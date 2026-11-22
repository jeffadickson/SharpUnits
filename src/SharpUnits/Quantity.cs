using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace SharpUnits;

/// <summary>Distinguishes absolute values/points from relative intervals/differences.</summary>
public enum QuantityScale
{
    /// <summary>Absolute magnitude or affine point (for example <c>25 °C</c>, <c>5 V</c>).</summary>
    Absolute,

    /// <summary>Relative interval or difference (for example <c>Δ10 °C</c>).</summary>
    Relative
}

/// <summary>Unified physical quantity value type preserving coherent SI magnitude, exact rational dimension, optional display unit, scale, and ISQ kind/specification.</summary>
[TypeConverter(typeof(QuantityTypeConverter))]
public readonly record struct Quantity :
    IComparable<Quantity>,
    IFormattable,
    ISpanFormattable,
    IUtf8SpanFormattable,
    IParsable<Quantity>,
    ISpanParsable<Quantity>,
    IUtf8SpanParsable<Quantity>,
    IComparisonOperators<Quantity, Quantity, bool>,
    IAdditionOperators<Quantity, Quantity, Quantity>,
    ISubtractionOperators<Quantity, Quantity, Quantity>,
    IUnaryNegationOperators<Quantity, Quantity>,
    IUnaryPlusOperators<Quantity, Quantity>,
    IMultiplyOperators<Quantity, double, Quantity>,
    IDivisionOperators<Quantity, double, Quantity>
{
    /// <summary>Numerical magnitude in coherent SI units.</summary>
    public double SiValue { get; }

    /// <summary>Canonical exact rational dimension vector.</summary>
    public RationalDimension RationalDimension { get; }

    /// <summary>Optional preferred display unit (null indicates unnamed coherent SI).</summary>
    public UnitDefinition? Unit { get; }

    /// <summary>Absolute point/value versus relative interval/difference.</summary>
    public QuantityScale Scale { get; }

    /// <summary>Optional semantic physical kind (for example <see cref="QuantityKind.Torque"/> vs. <see cref="QuantityKind.Energy"/>).</summary>
    public QuantityKind? Kind { get; }

    /// <summary>Optional ISO 80000 ISQ specification enabling hierarchical kind compatibility (for example <see cref="Isq.Width"/> + <see cref="Isq.Height"/> = <see cref="Isq.Length"/>).</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Integer dimension projection; throws for fractional exponents.</summary>
    public Dimension Dimension => RationalDimension.ToIntegerDimension();

    /// <summary>Magnitude expressed in <see cref="Unit"/> (or coherent SI when <see cref="Unit"/> is null).</summary>
    public double Value => Unit is null ? SiValue : Unit.FromQuantity(this);

    /// <summary>Display unit symbol, falling back to coherent SI notation.</summary>
    public string UnitSymbol
    {
        get
        {
            if (Unit is not null)
                return Scale == QuantityScale.Relative && Unit.IsAffine ? "Δ" + Unit.Symbol : Unit.Symbol;
            return Units.TryGetCoherentSiUnit(RationalDimension, out var si) && si is not null
                ? si.Symbol
                : RationalDimension.ToCoherentSiSymbol();
        }
    }

    /// <summary>Creates a coherent SI quantity with integer dimensions.</summary>
    public Quantity(double siValue, Dimension dimension)
        : this(siValue, RationalDimension.From(dimension))
    {
    }

    /// <summary>Creates a coherent SI quantity with exact rational dimensions.</summary>
    public Quantity(double siValue, RationalDimension dimension)
        : this(siValue, dimension, ResolveCoherentSiUnit(dimension), QuantityScale.Absolute, null, null)
    {
    }

    /// <summary>Creates a fully specified quantity.</summary>
    public Quantity(
        double siValue,
        RationalDimension dimension,
        UnitDefinition? unit,
        QuantityScale scale = QuantityScale.Absolute,
        QuantityKind? kind = null,
        QuantitySpec? spec = null)
    {
        if (!Enum.IsDefined(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        if (unit is not null && unit.RationalDimension != dimension)
            throw new InvalidOperationException("Quantity and display unit dimensions differ.");
        var resolvedSpec = spec;
        if (resolvedSpec is null && kind.HasValue && Isq.TryFromKind(kind.Value, out var fromKind))
            resolvedSpec = fromKind;
        if (resolvedSpec is not null && resolvedSpec.Dimension != dimension)
            throw new InvalidOperationException($"ISQ specification '{resolvedSpec.Id}' dimension disagrees with quantity dimension.");
        SiValue = siValue;
        RationalDimension = dimension;
        Unit = unit;
        Scale = scale;
        Spec = resolvedSpec;
        Kind = kind ?? resolvedSpec?.ToKind();
    }

    private static UnitDefinition? ResolveCoherentSiUnit(RationalDimension dimension) =>
        Units.TryGetCoherentSiUnit(dimension, out var si) ? si : null;

    /// <summary>Creates a quantity from a numeric value and display unit.</summary>
    public static Quantity Of(
        double value,
        UnitDefinition unit,
        QuantityScale scale = QuantityScale.Absolute,
        QuantityKind? kind = null,
        QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (!Enum.IsDefined(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
        double si = scale == QuantityScale.Relative && unit.IsAffine
            ? value * unit.Converter.Scale
            : unit.Converter.ToSi(value);
        var resolvedSpec = spec ?? (kind.HasValue && Isq.TryFromKind(kind.Value, out var kSpec) ? kSpec : unit.DefaultSpec);
        return new(si, unit.RationalDimension, unit, scale, kind ?? unit.DefaultKind, resolvedSpec);
    }

    /// <summary>Creates a quantity from a numeric value and algebraic <see cref="SymbolicUnit"/>.</summary>
    public static Quantity Of(
        double value,
        SymbolicUnit unit,
        QuantityScale scale = QuantityScale.Absolute,
        QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return Of(value, unit.ToUnitDefinition(), scale, (spec ?? unit.DefaultSpec)?.ToKind(), spec ?? unit.DefaultSpec);
    }

    /// <summary>Creates a quantity by resolving a unit symbol against <paramref name="registry"/> (defaults to <see cref="UnitRegistry.Standard"/>).</summary>
    public static Quantity Of(
        double value,
        string unitSymbol,
        UnitRegistry? registry = null,
        QuantityScale scale = QuantityScale.Absolute,
        QuantityKind? kind = null,
        QuantitySpec? spec = null)
    {
        var reg = registry ?? UnitRegistry.Standard;
        return Of(value, reg.Resolve(unitSymbol), scale, kind, spec);
    }

    /// <summary>Creates a relative interval quantity (for example <c>Δ10 °C</c>).</summary>
    public static Quantity Delta(double value, UnitDefinition unit, QuantityKind? kind = null) =>
        Of(value, unit, QuantityScale.Relative, kind);

    /// <summary>Parses a quantity string (such as <c>"12.5 kΩ"</c>, <c>"25 °C"</c>, or <c>"100mV"</c>) using <see cref="UnitRegistry.Standard"/>.</summary>
    public static Quantity Parse(string text, UnitRegistry? registry = null) =>
        new QuantityTextFormat(registry ?? UnitRegistry.Standard).Parse(text);

    /// <summary>Converts this quantity to a compatible target display unit, preserving scale and semantic identity.</summary>
    public Quantity In(UnitDefinition target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.RationalDimension != RationalDimension)
            throw new InvalidOperationException("Incompatible dimensions.");
        return new(SiValue, RationalDimension, target, Scale, Kind, Spec);
    }

    /// <summary>Converts this quantity to a compatible target symbolic unit.</summary>
    public Quantity In(SymbolicUnit target) => In(target.ToUnitDefinition());

    /// <summary>Converts this quantity to a compatible target display unit symbol.</summary>
    public Quantity In(string unitSymbol, UnitRegistry? registry = null) =>
        In((registry ?? UnitRegistry.Standard).Resolve(unitSymbol));

    /// <summary>Extracts the numeric magnitude in <paramref name="target"/> without allocating a display wrapper.</summary>
    public double AsValueIn(UnitDefinition target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return target.FromQuantity(this);
    }

    /// <summary>Extracts the numeric magnitude in <paramref name="unitSymbol"/>.</summary>
    public double AsValueIn(string unitSymbol, UnitRegistry? registry = null) =>
        AsValueIn((registry ?? UnitRegistry.Standard).Resolve(unitSymbol));

    /// <summary>Drops this quantity to a raw <see cref="double"/> expressed explicitly in <paramref name="target"/>.</summary>
    public double ValueIn(UnitDefinition target) => AsValueIn(target);

    /// <summary>Drops this quantity to a raw <see cref="double"/> expressed explicitly in <paramref name="unitSymbol"/>.</summary>
    public double ValueIn(string unitSymbol, UnitRegistry? registry = null) => AsValueIn(unitSymbol, registry);

    /// <summary>Reconstructs a <see cref="Quantity"/> from a raw value expressed in <paramref name="unit"/>.</summary>
    public static Quantity FromValueIn(double value, UnitDefinition unit) => Of(value, unit);

    /// <summary>Reconstructs a <see cref="Quantity"/> from a raw value expressed in <paramref name="unitSymbol"/>.</summary>
    public static Quantity FromValueIn(double value, string unitSymbol, UnitRegistry? registry = null) =>
        Of(value, unitSymbol, registry);

    /// <summary>Reconstructs a <see cref="Quantity"/> from a coherent SI scalar paired with its display <paramref name="unit"/>.</summary>
    public static Quantity FromSiValue(double siValue, UnitDefinition unit, QuantityScale scale = QuantityScale.Absolute, QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        var resolvedKind = spec?.ToKind() ?? unit.DefaultKind;
        var resolvedSpec = spec ?? (resolvedKind.HasValue && Isq.TryFromKind(resolvedKind.Value, out var kSpec) ? kSpec : unit.DefaultSpec);
        return new(siValue, unit.RationalDimension, unit, scale, resolvedKind, resolvedSpec);
    }

    /// <summary>Deconstructs this quantity into its display numeric value and unit symbol.</summary>
    public void Deconstruct(out double value, out string unitSymbol)
    {
        value = Value;
        unitSymbol = UnitSymbol;
    }

    /// <summary>Deconstructs this quantity into its display numeric value, optional <see cref="UnitDefinition"/>, and optional <see cref="QuantitySpec"/>.</summary>
    public void Deconstruct(out double value, out UnitDefinition? unit, out QuantitySpec? spec)
    {
        value = Value;
        unit = Unit;
        spec = Spec;
    }

    /// <summary>Converts this quantity to a serializer-agnostic primitive <see cref="QuantityData"/> wire record.</summary>
    public QuantityData ToData(bool asciiSymbol = false)
    {
        string rawUnit = Unit is not null ? Unit.Symbol : UnitSymbol;
        string sym = asciiSymbol ? ToAsciiSymbol(rawUnit) : rawUnit;
        return new(Value, sym, Spec?.Id, Scale);
    }

    /// <summary>Reconstructs a validated <see cref="Quantity"/> from a serializer-agnostic <see cref="QuantityData"/> record.</summary>
    public static Quantity FromData(QuantityData data, UnitEnvironment? environment = null)
    {
        if (!double.IsFinite(data.Value))
            throw new FormatException("QuantityData value must be finite.");
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit).ToUnitDefinition();
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.RationalDimension);
        return Of(data.Value, unit, data.Scale, spec?.ToKind(), spec);
    }

    /// <summary>Converts this linear quantity to a generic <see cref="Quantity{T}"/> for generic math workflows.</summary>
    public Quantity<T> AsGeneric<T>() where T : INumber<T> => Quantity<T>.FromQuantity(this);

    /// <summary>Converts this linear quantity to <see cref="Quantity{T}"/> with <see cref="double"/> representation.</summary>
    public Quantity<double> AsGeneric() => Quantity<double>.FromQuantity(this);

    /// <summary>Returns this quantity expressed in the most readable engineering SI prefix (for example <c>0.0025 V</c> to <c>2.5 mV</c>).</summary>
    public Quantity ToEngineeringUnit() => UnitAutoScaler.Scale(this);

    /// <summary>Returns a copy tagged with the specified semantic kind.</summary>
    public Quantity WithKind(QuantityKind kind)
    {
        Isq.TryFromKind(kind, out var resolvedSpec);
        return new(SiValue, RationalDimension, Unit, Scale, kind, resolvedSpec);
    }

    /// <summary>Returns a copy tagged with the specified ISO 80000 ISQ specification.</summary>
    public Quantity WithSpec(QuantitySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return new(SiValue, RationalDimension, Unit, Scale, spec.ToKind(), spec);
    }

    /// <summary>Returns a copy marked as a relative interval.</summary>
    public Quantity ToRelative() => new(SiValue, RationalDimension, Unit, QuantityScale.Relative, Kind, Spec);

    /// <summary>Checks physical equivalence in coherent SI within an optional absolute tolerance.</summary>
    public bool IsEquivalentTo(Quantity other, double siTolerance = 0d)
    {
        if (siTolerance < 0 || !double.IsFinite(siTolerance))
            throw new ArgumentOutOfRangeException(nameof(siTolerance));
        if (RationalDimension != other.RationalDimension) return false;
        if (!AreKindsCompatible(this, other)) return false;
        if (IsAffineAbsolute(this) != IsAffineAbsolute(other)) return false;
        return Math.Abs(SiValue - other.SiValue) <= siTolerance;
    }

    /// <summary>Parses a quantity string using <see cref="UnitRegistry.Standard"/>.</summary>
    public static Quantity Parse(string s, IFormatProvider? provider) => Parse(s, (UnitRegistry?)null);

    /// <summary>Attempts to parse a quantity string without throwing.</summary>
    public static bool TryParse(string? s, IFormatProvider? provider, out Quantity result) =>
        TryParse(s, (UnitRegistry?)null, out result);

    /// <summary>Parses a character span containing a quantity literal using <see cref="UnitRegistry.Standard"/>.</summary>
    public static Quantity Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        Parse(s.ToString(), (UnitRegistry?)null);

    /// <summary>Attempts to parse a character span containing a quantity literal without throwing.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Quantity result) =>
        TryParse(s.IsEmpty ? null : s.ToString(), (UnitRegistry?)null, out result);

    /// <summary>Parses a UTF-8 byte span containing a quantity literal using <see cref="UnitRegistry.Standard"/>.</summary>
    public static Quantity Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
        Parse(Encoding.UTF8.GetString(utf8Text), (UnitRegistry?)null);

    /// <summary>Attempts to parse a UTF-8 byte span containing a quantity literal without throwing.</summary>
    public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out Quantity result) =>
        TryParse(utf8Text.IsEmpty ? null : Encoding.UTF8.GetString(utf8Text), (UnitRegistry?)null, out result);

    /// <summary>Attempts to parse a quantity string against an optional registry without throwing.</summary>
    public static bool TryParse(string? s, UnitRegistry? registry, out Quantity result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = default;
            return false;
        }
        try
        {
            result = Parse(s, registry);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            result = default;
            return false;
        }
    }

    /// <summary>Formats this quantity using an optional numeric format string and culture.</summary>
    public string Format(string? numberFormat = "G", IFormatProvider? provider = null, bool asciiOnly = false)
    {
        string num = Value.ToString(numberFormat, provider ?? CultureInfo.InvariantCulture);
        string sym = asciiOnly ? ToAsciiSymbol(UnitSymbol) : UnitSymbol;
        return string.IsNullOrEmpty(sym) || sym == "1" ? num : $"{num} {sym}";
    }

    /// <summary>Formats this quantity using automatic engineering prefix scaling.</summary>
    public string FormatEngineering(string? numberFormat = "G4", IFormatProvider? provider = null, bool asciiOnly = false) =>
        ToEngineeringUnit().Format(numberFormat, provider, asciiOnly);

    /// <summary>
    /// Formats this quantity for <see cref="IFormattable"/> interpolation.
    /// Supports <c>"eng"</c>, <c>"ascii"</c>, <c>"eng:ascii"</c>, <c>"eng:F2"</c>, or any standard numeric format string (e.g. <c>"G4"</c>, <c>"F3"</c>).
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrWhiteSpace(format))
            return Format("G", formatProvider ?? CultureInfo.InvariantCulture);
        if (format.Equals("eng", StringComparison.OrdinalIgnoreCase))
            return FormatEngineering("G4", formatProvider ?? CultureInfo.InvariantCulture);
        if (format.Equals("ascii", StringComparison.OrdinalIgnoreCase))
            return Format("G", formatProvider ?? CultureInfo.InvariantCulture, asciiOnly: true);
        if (format.Equals("eng:ascii", StringComparison.OrdinalIgnoreCase))
            return FormatEngineering("G4", formatProvider ?? CultureInfo.InvariantCulture, asciiOnly: true);
        if (format.StartsWith("eng:", StringComparison.OrdinalIgnoreCase))
            return FormatEngineering(format[4..], formatProvider ?? CultureInfo.InvariantCulture);
        return Format(format, formatProvider ?? CultureInfo.InvariantCulture);
    }

    /// <summary>Formats this quantity directly into <paramref name="destination"/> without allocating an intermediate string when possible.</summary>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        var q = this;
        ReadOnlySpan<char> numFmt = format.IsEmpty ? "G" : format;
        bool ascii = false;

        if (format.Equals("eng", StringComparison.OrdinalIgnoreCase))
        {
            q = ToEngineeringUnit();
            numFmt = "G4";
        }
        else if (format.Equals("ascii", StringComparison.OrdinalIgnoreCase))
        {
            ascii = true;
            numFmt = "G";
        }
        else if (format.Equals("eng:ascii", StringComparison.OrdinalIgnoreCase))
        {
            q = ToEngineeringUnit();
            ascii = true;
            numFmt = "G4";
        }
        else if (format.StartsWith("eng:", StringComparison.OrdinalIgnoreCase))
        {
            q = ToEngineeringUnit();
            numFmt = format[4..];
        }

        if (!q.Value.TryFormat(destination, out int numLen, numFmt, provider ?? CultureInfo.InvariantCulture))
        {
            charsWritten = 0;
            return false;
        }

        string sym = ascii ? ToAsciiSymbol(q.UnitSymbol) : q.UnitSymbol;
        if (string.IsNullOrEmpty(sym) || sym == "1")
        {
            charsWritten = numLen;
            return true;
        }

        int total = numLen + 1 + sym.Length;
        if (destination.Length < total)
        {
            charsWritten = 0;
            return false;
        }
        destination[numLen] = ' ';
        sym.AsSpan().CopyTo(destination[(numLen + 1)..]);
        charsWritten = total;
        return true;
    }

    /// <summary>Formats this quantity directly into a UTF-8 byte span without allocating an intermediate string.</summary>
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        Span<char> chars = stackalloc char[128];
        if (!TryFormat(chars, out int charLen, format, provider))
        {
            bytesWritten = 0;
            return false;
        }
        int needed = Encoding.UTF8.GetByteCount(chars[..charLen]);
        if (utf8Destination.Length < needed)
        {
            bytesWritten = 0;
            return false;
        }
        bytesWritten = Encoding.UTF8.GetBytes(chars[..charLen], utf8Destination);
        return true;
    }

    /// <summary>Returns an invariant-culture string combining magnitude and display unit.</summary>
    public override string ToString() => Format("G", CultureInfo.InvariantCulture);

    /// <summary>Compares compatible quantities by coherent SI magnitude.</summary>
    public int CompareTo(Quantity other)
    {
        RequireCompatible(this, other);
        if (IsAffineAbsolute(this) != IsAffineAbsolute(other))
            throw new InvalidOperationException("Cannot compare an absolute affine quantity with a relative interval.");
        return SiValue.CompareTo(other.SiValue);
    }

    /// <summary>Adds compatible quantities, enforcing affine and ISQ/semantic-kind rules.</summary>
    public static Quantity operator +(Quantity a, Quantity b)
    {
        var (kind, spec) = RequireCompatible(a, b);
        bool aAffineAbs = IsAffineAbsolute(a);
        bool bAffineAbs = IsAffineAbsolute(b);
        if (aAffineAbs && bAffineAbs)
            throw new InvalidOperationException("Absolute affine quantities cannot be added.");
        if (aAffineAbs) return new(a.SiValue + b.SiValue, a.RationalDimension, a.Unit, QuantityScale.Absolute, kind, spec);
        if (bAffineAbs) return new(a.SiValue + b.SiValue, a.RationalDimension, b.Unit, QuantityScale.Absolute, kind, spec);
        var scale = a.Scale == QuantityScale.Relative && b.Scale == QuantityScale.Relative ? QuantityScale.Relative : QuantityScale.Absolute;
        return new(a.SiValue + b.SiValue, a.RationalDimension, a.Unit ?? b.Unit, scale, kind, spec);
    }

    /// <summary>Subtracts compatible quantities; subtracting two absolute affine quantities produces a relative difference.</summary>
    public static Quantity operator -(Quantity a, Quantity b)
    {
        var (kind, spec) = RequireCompatible(a, b);
        bool aAffineAbs = IsAffineAbsolute(a);
        bool bAffineAbs = IsAffineAbsolute(b);
        if (aAffineAbs && bAffineAbs)
            return new(a.SiValue - b.SiValue, a.RationalDimension, a.Unit, QuantityScale.Relative, kind, spec);
        if (aAffineAbs && !bAffineAbs)
            return new(a.SiValue - b.SiValue, a.RationalDimension, a.Unit, QuantityScale.Absolute, kind, spec);
        if (!aAffineAbs && bAffineAbs)
            throw new InvalidOperationException("Cannot subtract an absolute affine quantity from a relative interval.");
        var scale = a.Scale == QuantityScale.Relative || b.Scale == QuantityScale.Relative ? QuantityScale.Relative : QuantityScale.Absolute;
        return new(a.SiValue - b.SiValue, a.RationalDimension, a.Unit ?? b.Unit, scale, kind, spec);
    }

    /// <summary>Unary plus.</summary>
    public static Quantity operator +(Quantity a) => a;

    /// <summary>Unary negation; rejects absolute affine quantities.</summary>
    public static Quantity operator -(Quantity a)
    {
        RequireLinearAlgebra(a);
        return new(-a.SiValue, a.RationalDimension, a.Unit, a.Scale, a.Kind, a.Spec);
    }

    /// <summary>Multiplies two linear quantities and adds their dimensional exponents.</summary>
    public static Quantity operator *(Quantity a, Quantity b)
    {
        RequireLinearAlgebra(a);
        RequireLinearAlgebra(b);
        var dim = a.RationalDimension + b.RationalDimension;
        Units.TryGetCoherentSiUnit(dim, out var siUnit);
        return new(a.SiValue * b.SiValue, dim, siUnit, QuantityScale.Absolute, null, null);
    }

    /// <summary>Divides two linear quantities and subtracts their dimensional exponents.</summary>
    public static Quantity operator /(Quantity a, Quantity b)
    {
        RequireLinearAlgebra(a);
        RequireLinearAlgebra(b);
        var dim = a.RationalDimension - b.RationalDimension;
        Units.TryGetCoherentSiUnit(dim, out var siUnit);
        return new(a.SiValue / b.SiValue, dim, siUnit, QuantityScale.Absolute, null, null);
    }

    /// <summary>Scales a linear quantity by a scalar factor, preserving its display unit.</summary>
    public static Quantity operator *(Quantity a, double scalar)
    {
        RequireLinearAlgebra(a);
        return new(a.SiValue * scalar, a.RationalDimension, a.Unit, a.Scale, a.Kind, a.Spec);
    }

    /// <summary>Scales a linear quantity by a scalar factor, preserving its display unit.</summary>
    public static Quantity operator *(double scalar, Quantity a) => a * scalar;

    /// <summary>Divides a linear quantity by a scalar divisor, preserving its display unit.</summary>
    public static Quantity operator /(Quantity a, double divisor)
    {
        RequireLinearAlgebra(a);
        return new(a.SiValue / divisor, a.RationalDimension, a.Unit, a.Scale, a.Kind, a.Spec);
    }

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Quantity a, Quantity b) => a.CompareTo(b) < 0;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Quantity a, Quantity b) => a.CompareTo(b) <= 0;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Quantity a, Quantity b) => a.CompareTo(b) > 0;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Quantity a, Quantity b) => a.CompareTo(b) >= 0;

    /// <summary>Raises a linear quantity to an exact rational dimensional power.</summary>
    public Quantity Pow(Rational power)
    {
        RequireLinearAlgebra(this);
        var dim = RationalDimension.Pow(power);
        Units.TryGetCoherentSiUnit(dim, out var siUnit);
        return new(Math.Pow(SiValue, power.ToDouble()), dim, siUnit);
    }

    /// <summary>Raises a linear quantity to an integer power.</summary>
    public Quantity Pow(int power) => Pow(new Rational(power, 1));

    /// <summary>Returns the square root of a linear quantity with exact rational dimensions.</summary>
    public Quantity Sqrt() => Pow(new Rational(1, 2));

    private static bool IsAffineAbsolute(Quantity q) =>
        q.Scale == QuantityScale.Absolute && q.Unit is not null && q.Unit.IsAffine;

    private static void RequireLinearAlgebra(Quantity q)
    {
        if (IsAffineAbsolute(q))
            throw new InvalidOperationException("Absolute affine quantities cannot participate in linear scaling or multiplicative algebra.");
    }

    private static bool AreKindsCompatible(Quantity a, Quantity b)
    {
        if (a.Spec is not null && b.Spec is not null)
            return QuantitySpec.CommonSpec(a.Spec, b.Spec) is not null;
        if (a.Kind.HasValue && b.Kind.HasValue)
            return a.Kind.Value == b.Kind.Value;
        return true;
    }

    private static (QuantityKind? Kind, QuantitySpec? Spec) RequireCompatible(Quantity a, Quantity b)
    {
        if (a.RationalDimension != b.RationalDimension)
            throw new InvalidOperationException("Incompatible dimensions.");
        if (a.Spec is not null && b.Spec is not null)
        {
            var common = QuantitySpec.CommonSpec(a.Spec, b.Spec);
            if (common is null)
                throw new InvalidOperationException($"Incompatible ISQ quantity kinds: '{a.Spec.Id}' vs '{b.Spec.Id}'.");
            QuantityKind? resolvedKind = common == a.Spec ? a.Kind : common == b.Spec ? b.Kind : common.ToKind();
            return (resolvedKind, common);
        }
        if (a.Kind.HasValue && b.Kind.HasValue && a.Kind.Value != b.Kind.Value)
            throw new InvalidOperationException($"Incompatible semantic kinds: '{a.Kind}' vs '{b.Kind}'.");
        return (a.Kind ?? b.Kind, a.Spec ?? b.Spec);
    }

    private static string ToAsciiSymbol(string symbol) =>
        symbol.Replace("µ", "u", StringComparison.Ordinal)
              .Replace("Ω", "Ohm", StringComparison.Ordinal)
              .Replace("°", "deg", StringComparison.Ordinal)
              .Replace("Δ", "d", StringComparison.Ordinal)
              .Replace("·", "*", StringComparison.Ordinal);
}

/// <summary>Convenience coherent SI quantity factories.</summary>
public static class SiQuantities
{
    /// <summary>Creates voltage in volts.</summary>
    public static Quantity Volts(double value) => Quantity.Of(value, Units.Volt);

    /// <summary>Creates resistance in ohms.</summary>
    public static Quantity Ohms(double value) => Quantity.Of(value, Units.Ohm);

    /// <summary>Creates current in amperes.</summary>
    public static Quantity Amperes(double value) => Quantity.Of(value, Units.Ampere);

    /// <summary>Creates power in watts.</summary>
    public static Quantity Watts(double value) => Quantity.Of(value, Units.Watt);

    /// <summary>Creates frequency in hertz.</summary>
    public static Quantity Hertz(double value) => Quantity.Of(value, Units.Hertz);

    /// <summary>Creates duration in seconds.</summary>
    public static Quantity Seconds(double value) => Quantity.Of(value, Units.Second);

    /// <summary>Creates length in meters.</summary>
    public static Quantity Meters(double value) => Quantity.Of(value, Units.Meter);

    /// <summary>Creates mass in kilograms.</summary>
    public static Quantity Kilograms(double value) => Quantity.Of(value, Units.Kilogram);

    /// <summary>Creates thermodynamic temperature in kelvin.</summary>
    public static Quantity Kelvin(double value) => Quantity.Of(value, Units.Kelvin);
}
