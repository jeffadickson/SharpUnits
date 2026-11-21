namespace SharpUnits;

/// <summary>Unit converter supporting linear and affine transformations to coherent SI.</summary>
/// <remarks>Absolute affine units such as Celsius or Fahrenheit must not be multiplied directly as linear unit expressions.</remarks>
public readonly record struct UnitConversion
{
    /// <summary>Multiplicative factor to coherent SI.</summary>
    public double Scale { get; }

    /// <summary>Additive offset in coherent SI units.</summary>
    public double Offset { get; }

    /// <summary>Creates a converter; scale must be finite and nonzero, offset finite.</summary>
    public UnitConversion(double scale, double offset)
    {
        if (!double.IsFinite(scale) || scale == 0 || !double.IsFinite(offset))
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be finite and nonzero; offset must be finite.");
        Scale = scale;
        Offset = offset;
    }

    /// <summary>Converts a value to the coherent SI representation.</summary>
    public double ToSi(double value)
    {
        double result = value * Scale + Offset;
        if (double.IsFinite(value) && !double.IsFinite(result))
            throw new OverflowException("Conversion to SI overflowed.");
        return result;
    }

    /// <summary>Converts an SI value back to the source unit.</summary>
    public double FromSi(double siValue)
    {
        double result = (siValue - Offset) / Scale;
        if (double.IsFinite(siValue) && !double.IsFinite(result))
            throw new OverflowException("Conversion from SI overflowed.");
        return result;
    }

    /// <summary>True if the converter leaves every finite value unchanged.</summary>
    public bool IsIdentity => Scale == 1d && Offset == 0d;

    /// <summary>True if the conversion has no additive offset.</summary>
    public bool IsLinear => Offset == 0d;

    /// <summary>Returns the inverse affine mapping, from SI back to the original unit.</summary>
    public UnitConversion Inverse() => new(1d / Scale, -Offset / Scale);

    /// <summary>Composes this mapping with a subsequent affine mapping.</summary>
    /// <remarks>The returned mapping applies this conversion first, then <paramref name="next"/>.</remarks>
    public UnitConversion Then(UnitConversion next) =>
        new(Scale * next.Scale, Offset * next.Scale + next.Offset);

    /// <summary>Identity conversion.</summary>
    public static UnitConversion Identity => new(1d, 0d);

    /// <summary>Linear SI scale conversion.</summary>
    public static UnitConversion Linear(double scale) => new(scale, 0d);

    /// <summary>Absolute degrees Celsius to kelvin.</summary>
    public static UnitConversion Celsius => new(1d, 273.15d);

    /// <summary>Absolute degrees Fahrenheit to kelvin.</summary>
    public static UnitConversion Fahrenheit => new(5d / 9d, 459.67d * 5d / 9d);

    /// <summary>Absolute degrees Rankine to kelvin.</summary>
    public static UnitConversion Rankine => new(5d / 9d, 0d);
}

/// <summary>Describes a named unit with canonical rational dimensions, SI conversion, optional ISQ spec, and symbolic unit algebra.</summary>
public sealed record UnitDefinition
{
    /// <summary>Unit symbol (e.g. <c>V</c>, <c>kΩ</c>, <c>°C</c>).</summary>
    public string Symbol { get; }

    /// <summary>Human-readable unit name (defaults to <see cref="Symbol"/>).</summary>
    public string Name { get; }

    /// <summary>Canonical rational dimension vector.</summary>
    public RationalDimension RationalDimension { get; }

    /// <summary>Integer dimension projection; only valid for integral exponents.</summary>
    public Dimension Dimension => RationalDimension.ToIntegerDimension();

    /// <summary>Conversion to coherent SI.</summary>
    public UnitConversion Converter { get; }

    /// <summary>True when SI decimal prefixes may be applied automatically to this unit.</summary>
    public bool AllowPrefixes { get; }

    /// <summary>Optional default semantic quantity kind associated with this unit.</summary>
    public QuantityKind? DefaultKind { get; }

    /// <summary>Optional default ISO 80000 ISQ specification associated with this unit.</summary>
    public QuantitySpec? DefaultSpec { get; }

    /// <summary>True when this unit has an additive offset (such as Celsius or Fahrenheit).</summary>
    public bool IsAffine => !Converter.IsLinear;

    /// <summary>Constructs from integer dimensions.</summary>
    public UnitDefinition(
        string symbol,
        Dimension dimension,
        UnitConversion converter,
        string? name = null,
        bool allowPrefixes = false,
        QuantityKind? defaultKind = null,
        QuantitySpec? defaultSpec = null)
        : this(symbol, RationalDimension.From(dimension), converter, name, allowPrefixes, defaultKind, defaultSpec)
    {
    }

    /// <summary>Constructs from canonical rational dimensions.</summary>
    public UnitDefinition(
        string symbol,
        RationalDimension dimension,
        UnitConversion converter,
        string? name = null,
        bool allowPrefixes = false,
        QuantityKind? defaultKind = null,
        QuantitySpec? defaultSpec = null)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Unit symbol required.", nameof(symbol));
        if (allowPrefixes && !converter.IsLinear)
            throw new ArgumentException("Affine units cannot allow SI prefixes.", nameof(allowPrefixes));
        var resolvedSpec = defaultSpec;
        if (resolvedSpec is null && defaultKind.HasValue && Isq.TryFromKind(defaultKind.Value, out var inferredSpec))
            resolvedSpec = inferredSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != dimension)
            throw new InvalidOperationException($"Default specification '{resolvedSpec.Id}' disagrees with unit dimension.");
        Symbol = symbol;
        Name = string.IsNullOrWhiteSpace(name) ? symbol : name;
        RationalDimension = dimension;
        Converter = converter;
        AllowPrefixes = allowPrefixes;
        DefaultSpec = resolvedSpec;
        DefaultKind = defaultKind ?? resolvedSpec?.ToKind();
    }

    /// <summary>Converts a value into a unified quantity retaining this display unit.</summary>
    public Quantity ToQuantity(double value, QuantityScale scale = QuantityScale.Absolute, QuantityKind? kind = null) =>
        Quantity.Of(value, this, scale, kind ?? DefaultKind);

    /// <summary>Converts a compatible quantity into a numeric value in this unit.</summary>
    public double FromQuantity(Quantity quantity)
    {
        if (quantity.RationalDimension != RationalDimension)
            throw new InvalidOperationException("Incompatible dimensions.");
        if (quantity.Scale == QuantityScale.Relative && IsAffine)
            return quantity.SiValue / Converter.Scale;
        return Converter.FromSi(quantity.SiValue);
    }

    /// <summary>Multiplies two linear units into a symbolic unit expression (for example <c>Units.Newton * Units.Meter</c>).</summary>
    public static SymbolicUnit operator *(UnitDefinition a, UnitDefinition b) => SymbolicUnit.From(a) * SymbolicUnit.From(b);

    /// <summary>Multiplies a unit by a symbolic unit expression.</summary>
    public static SymbolicUnit operator *(UnitDefinition a, SymbolicUnit b) => SymbolicUnit.From(a) * b;

    /// <summary>Multiplies a symbolic unit expression by a unit.</summary>
    public static SymbolicUnit operator *(SymbolicUnit a, UnitDefinition b) => a * SymbolicUnit.From(b);

    /// <summary>Divides two linear units into a symbolic unit expression (for example <c>Units.Meter / Units.Second</c>).</summary>
    public static SymbolicUnit operator /(UnitDefinition a, UnitDefinition b) => SymbolicUnit.From(a) / SymbolicUnit.From(b);

    /// <summary>Divides a unit by a symbolic unit expression.</summary>
    public static SymbolicUnit operator /(UnitDefinition a, SymbolicUnit b) => SymbolicUnit.From(a) / b;

    /// <summary>Divides a symbolic unit expression by a unit.</summary>
    public static SymbolicUnit operator /(SymbolicUnit a, UnitDefinition b) => a / SymbolicUnit.From(b);

    /// <summary>Raises this linear unit to an integer power (for example <c>Units.Second.Pow(2)</c>).</summary>
    public SymbolicUnit Pow(int exponent) => SymbolicUnit.From(this).Pow(exponent);

    /// <summary>Raises this linear unit to an exact rational power.</summary>
    public SymbolicUnit Pow(Rational exponent) => SymbolicUnit.From(this).Pow(exponent);

    /// <summary>Returns the positive integer-degree root of this linear unit (for example <c>Units.Hertz.Root(2)</c>).</summary>
    public SymbolicUnit Root(int degree) => SymbolicUnit.From(this).Root(degree);

    /// <summary>Returns the multiplicative inverse of this linear unit.</summary>
    public SymbolicUnit Inverse() => SymbolicUnit.From(this).Inverse();

    /// <summary>Returns the unit symbol.</summary>
    public override string ToString() => Symbol;
}
