using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Generic physical quantity storing magnitude in its own <see cref="SymbolicUnit"/> using .NET generic math (<see cref="INumber{TSelf}"/>).</summary>
/// <typeparam name="T">Numeric representation type (for example <see cref="double"/>, <see cref="float"/>, <see cref="decimal"/>, <see cref="int"/>, <see cref="long"/>).</typeparam>
/// <remarks>
/// Inspired by C++ <c>mp-units</c>:
/// <list type="bullet">
/// <item>Values are stored directly in <see cref="Unit"/> (zero round-trip drift for <see cref="decimal"/> and integral representations).</item>
/// <item>Additive operations enforce ISO 80000 ISQ <see cref="QuantitySpec"/> compatibility and automatically select the lossless finer common unit.</item>
/// <item>Multiplicative operations preserve algebraic <see cref="SymbolicUnit"/> structure (for example <c>m/s</c>, <c>nV/Hz^(1/2)</c>).</item>
/// <item>Affine points (such as Celsius or gauge pressure) belong in <see cref="QuantityPoint{T}"/>.</item>
/// </list>
/// </remarks>
public readonly record struct Quantity<T> :
    IComparable<Quantity<T>>,
    IFormattable,
    IComparisonOperators<Quantity<T>, Quantity<T>, bool>,
    IAdditionOperators<Quantity<T>, Quantity<T>, Quantity<T>>,
    ISubtractionOperators<Quantity<T>, Quantity<T>, Quantity<T>>,
    IUnaryNegationOperators<Quantity<T>, Quantity<T>>,
    IUnaryPlusOperators<Quantity<T>, Quantity<T>>,
    IMultiplyOperators<Quantity<T>, T, Quantity<T>>,
    IDivisionOperators<Quantity<T>, T, Quantity<T>>
    where T : INumber<T>
{
    /// <summary>Numerical magnitude expressed in <see cref="Unit"/>.</summary>
    public T Value { get; }

    /// <summary>Algebraic symbolic unit of this quantity.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Optional ISO 80000 ISQ quantity specification.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Canonical exact rational dimension of this quantity.</summary>
    public RationalDimension Dimension => Unit.Dimension;

    /// <summary>Coherent SI magnitude in double precision.</summary>
    public double SiValue => double.CreateChecked(Value) * Unit.ScaleToSi;

    /// <summary>Constructs a validated linear quantity in <paramref name="unit"/>.</summary>
    public Quantity(T value, SymbolicUnit unit, QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.IsAffine)
            throw new InvalidOperationException("Affine units represent points, not displacements; use QuantityPoint<T> or a linear unit.");
        var resolvedSpec = spec ?? unit.DefaultSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Specification '{resolvedSpec.Id}' dimension disagrees with unit '{unit.Symbol}'.");
        Value = value;
        Unit = unit;
        Spec = resolvedSpec;
    }

    /// <summary>Creates a quantity from a numeric magnitude and symbolic unit.</summary>
    public static Quantity<T> Of(T value, SymbolicUnit unit, QuantitySpec? spec = null) => new(value, unit, spec);

    /// <summary>Creates a quantity by resolving a unit symbol against <paramref name="registry"/> (defaults to <see cref="UnitRegistry.Standard"/>).</summary>
    public static Quantity<T> Of(T value, string unitSymbol, UnitRegistry? registry = null, QuantitySpec? spec = null)
    {
        var reg = registry ?? UnitRegistry.Standard;
        return new(value, SymbolicUnit.From(reg.Resolve(unitSymbol)), spec);
    }

    /// <summary>Creates a dimensionless quantity.</summary>
    public static Quantity<T> Dimensionless(T value, QuantitySpec? spec = null) =>
        new(value, SymbolicUnit.One, spec ?? Isq.Dimensionless);

    /// <summary>Converts this quantity to a compatible target unit; rejects lossy truncation for integral representations.</summary>
    public Quantity<T> In(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine)
            throw new InvalidOperationException("Cannot convert a linear quantity to an affine unit; use QuantityPoint<T>.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        T converted = ConvertScale(Value, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        return new(converted, target, Spec);
    }

    /// <summary>Converts this quantity to a compatible target unit symbol.</summary>
    public Quantity<T> In(string unitSymbol, UnitRegistry? registry = null) =>
        In(SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)));

    /// <summary>Converts this quantity to <paramref name="target"/>, allowing truncation when <typeparamref name="T"/> is an integer type.</summary>
    public Quantity<T> InTruncating(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine)
            throw new InvalidOperationException("Cannot convert a linear quantity to an affine unit.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        T converted = ConvertScale(Value, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: true);
        return new(converted, target, Spec);
    }

    /// <summary>Converts this quantity to its canonical coherent SI unit.</summary>
    public Quantity<T> ToCoherentSi() => In(Unit.ToCoherentSi());

    /// <summary>Drops this quantity to a raw <typeparamref name="T"/> scalar expressed explicitly in <paramref name="target"/>.</summary>
    public T ValueIn(SymbolicUnit target) => In(target).Value;

    /// <summary>Drops this quantity to a raw <typeparamref name="T"/> scalar expressed explicitly in <paramref name="unitSymbol"/>.</summary>
    public T ValueIn(string unitSymbol, UnitRegistry? registry = null) => In(unitSymbol, registry).Value;

    /// <summary>Reconstructs a <see cref="Quantity{T}"/> from a raw scalar expressed in <paramref name="unit"/>.</summary>
    public static Quantity<T> FromValueIn(T value, SymbolicUnit unit, QuantitySpec? spec = null) => Of(value, unit, spec);

    /// <summary>Reconstructs a <see cref="Quantity{T}"/> from a raw scalar expressed in <paramref name="unitSymbol"/>.</summary>
    public static Quantity<T> FromValueIn(T value, string unitSymbol, UnitRegistry? registry = null, QuantitySpec? spec = null) =>
        Of(value, unitSymbol, registry, spec);

    /// <summary>Deconstructs this quantity into its value and <see cref="SymbolicUnit"/>.</summary>
    public void Deconstruct(out T value, out SymbolicUnit unit)
    {
        value = Value;
        unit = Unit;
    }

    /// <summary>Deconstructs this quantity into its value, <see cref="SymbolicUnit"/>, and optional <see cref="QuantitySpec"/>.</summary>
    public void Deconstruct(out T value, out SymbolicUnit unit, out QuantitySpec? spec)
    {
        value = Value;
        unit = Unit;
        spec = Spec;
    }

    /// <summary>Converts this quantity to a serializer-agnostic primitive <see cref="QuantityData{T}"/> wire record.</summary>
    public QuantityData<T> ToData(bool asciiSymbol = false) =>
        new(Value, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Spec?.Id);

    /// <summary>Reconstructs a validated <see cref="Quantity{T}"/> from a serializer-agnostic <see cref="QuantityData{T}"/> record.</summary>
    public static Quantity<T> FromData(QuantityData<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        return Of(data.Value, unit, spec);
    }

    /// <summary>Explicitly casts this quantity to a compatible ISQ specification within the same kind tree (for example <see cref="Isq.Length"/> to <see cref="Isq.Width"/>).</summary>
    public Quantity<T> AsSpec(QuantitySpec target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Dimension != Dimension)
            throw new InvalidOperationException($"Target specification '{target.Id}' has incompatible dimension.");
        if (Spec is not null && !Spec.IsExplicitlyConvertibleTo(target))
            throw new InvalidOperationException($"Cannot convert quantity kind '{Spec.Id}' to incompatible kind '{target.Id}'.");
        return new(Value, Unit, target);
    }

    /// <summary>Converts the underlying numeric representation (for example <c>double</c> to <c>float</c> for GPU plotting or <c>int</c> to <c>decimal</c>).</summary>
    public Quantity<TOther> As<TOther>() where TOther : INumber<TOther> =>
        new(TOther.CreateChecked(Value), Unit, Spec);

    /// <summary>Converts to the non-generic <see cref="Quantity"/> struct in double precision.</summary>
    public Quantity ToQuantity() =>
        new(SiValue, Dimension, Unit.ToUnitDefinition(), QuantityScale.Absolute, Spec?.ToKind(), Spec);

    /// <summary>Creates a <see cref="Quantity{T}"/> from a non-generic <see cref="Quantity"/>.</summary>
    public static Quantity<T> FromQuantity(Quantity quantity)
    {
        var unit = quantity.Unit is not null ? SymbolicUnit.From(quantity.Unit) : SymbolicUnit.From(Units.One).ToCoherentSi();
        if (quantity.Unit is null)
        {
            unit = new UnitDefinition(quantity.UnitSymbol, quantity.RationalDimension, UnitConversion.Identity);
        }
        if (unit.IsAffine)
            throw new InvalidOperationException("Affine quantities convert to QuantityPoint<T>, not linear Quantity<T>.");
        return new(T.CreateChecked(quantity.Value), unit, quantity.Spec);
    }

    /// <summary>Checks physical equivalence within an optional coherent-SI tolerance.</summary>
    public bool IsEquivalentTo(Quantity<T> other, double siTolerance = 0d)
    {
        if (siTolerance < 0 || !double.IsFinite(siTolerance))
            throw new ArgumentOutOfRangeException(nameof(siTolerance));
        if (Dimension != other.Dimension) return false;
        if (Spec is not null && other.Spec is not null && QuantitySpec.CommonSpec(Spec, other.Spec) is null)
            return false;
        return Math.Abs(SiValue - other.SiValue) <= siTolerance;
    }

    /// <summary>Adds compatible quantities, promoting to their common ISQ specification and lossless finer unit.</summary>
    public static Quantity<T> operator +(Quantity<T> a, Quantity<T> b)
    {
        var commonSpec = RequireAdditiveCompatible(a, b);
        var commonUnit = SymbolicUnit.CommonUnit(a.Unit, b.Unit);
        T left = a.Unit.Equals(commonUnit) ? a.Value : a.In(commonUnit).Value;
        T right = b.Unit.Equals(commonUnit) ? b.Value : b.In(commonUnit).Value;
        return new(left + right, commonUnit, commonSpec);
    }

    /// <summary>Subtracts compatible quantities, promoting to their common ISQ specification and lossless finer unit.</summary>
    public static Quantity<T> operator -(Quantity<T> a, Quantity<T> b)
    {
        var commonSpec = RequireAdditiveCompatible(a, b);
        var commonUnit = SymbolicUnit.CommonUnit(a.Unit, b.Unit);
        T left = a.Unit.Equals(commonUnit) ? a.Value : a.In(commonUnit).Value;
        T right = b.Unit.Equals(commonUnit) ? b.Value : b.In(commonUnit).Value;
        return new(left - right, commonUnit, commonSpec);
    }

    /// <summary>Unary plus.</summary>
    public static Quantity<T> operator +(Quantity<T> value) => value;

    /// <summary>Unary negation.</summary>
    public static Quantity<T> operator -(Quantity<T> value) => new(-value.Value, value.Unit, value.Spec);

    /// <summary>Multiplies two quantities, preserving their algebraic symbolic unit product.</summary>
    public static Quantity<T> operator *(Quantity<T> a, Quantity<T> b) =>
        new(a.Value * b.Value, a.Unit * b.Unit, null);

    /// <summary>Divides two quantities, preserving their algebraic symbolic unit quotient.</summary>
    public static Quantity<T> operator /(Quantity<T> a, Quantity<T> b) =>
        new(a.Value / b.Value, a.Unit / b.Unit, null);

    /// <summary>Scales a quantity by a scalar factor, preserving unit and ISQ specification.</summary>
    public static Quantity<T> operator *(Quantity<T> a, T scalar) => new(a.Value * scalar, a.Unit, a.Spec);

    /// <summary>Scales a quantity by a scalar factor, preserving unit and ISQ specification.</summary>
    public static Quantity<T> operator *(T scalar, Quantity<T> a) => new(scalar * a.Value, a.Unit, a.Spec);

    /// <summary>Divides a quantity by a scalar divisor, preserving unit and ISQ specification.</summary>
    public static Quantity<T> operator /(Quantity<T> a, T divisor) => new(a.Value / divisor, a.Unit, a.Spec);

    /// <summary>Compares compatible quantities.</summary>
    public int CompareTo(Quantity<T> other)
    {
        RequireAdditiveCompatible(this, other);
        if (Unit.Equals(other.Unit) || Unit.ScaleToSi == other.Unit.ScaleToSi)
            return Value.CompareTo(other.Value);
        var commonUnit = SymbolicUnit.CommonUnit(Unit, other.Unit);
        T left = Unit.Equals(commonUnit) ? Value : In(commonUnit).Value;
        T right = other.Unit.Equals(commonUnit) ? other.Value : other.In(commonUnit).Value;
        return left.CompareTo(right);
    }

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Quantity<T> left, Quantity<T> right) => left.CompareTo(right) < 0;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Quantity<T> left, Quantity<T> right) => left.CompareTo(right) <= 0;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Quantity<T> left, Quantity<T> right) => left.CompareTo(right) > 0;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Quantity<T> left, Quantity<T> right) => left.CompareTo(right) >= 0;

    /// <summary>Formats magnitude and symbolic unit.</summary>
    public string Format(string? numberFormat = "G", IFormatProvider? provider = null, bool asciiOnly = false)
    {
        string num = Value.ToString(numberFormat, provider ?? CultureInfo.InvariantCulture);
        string sym = asciiOnly ? Unit.AsciiSymbol : Unit.Symbol;
        return sym == "1" ? num : $"{num} {sym}";
    }

    /// <summary>Formats this quantity for <see cref="IFormattable"/> interpolation (supports <c>"ascii"</c> or standard numeric formats).</summary>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (string.IsNullOrWhiteSpace(format))
            return Format("G", formatProvider ?? CultureInfo.InvariantCulture);
        if (format.Equals("ascii", StringComparison.OrdinalIgnoreCase))
            return Format("G", formatProvider ?? CultureInfo.InvariantCulture, asciiOnly: true);
        return Format(format, formatProvider ?? CultureInfo.InvariantCulture);
    }

    /// <summary>Returns an invariant-culture formatted representation.</summary>
    public override string ToString() => Format("G", CultureInfo.InvariantCulture);

    private static QuantitySpec? RequireAdditiveCompatible(Quantity<T> a, Quantity<T> b)
    {
        if (a.Dimension != b.Dimension)
            throw new InvalidOperationException($"Incompatible dimensions: '{a.Unit.Symbol}' vs '{b.Unit.Symbol}'.");
        if (a.Spec is null && b.Spec is null) return null;
        var common = QuantitySpec.CommonSpec(a.Spec, b.Spec);
        if (common is null)
            throw new InvalidOperationException($"Incompatible ISQ quantity kinds: '{a.Spec?.Id}' vs '{b.Spec?.Id}'.");
        return common;
    }

    internal static T ConvertScale(T value, double sourceScale, double targetScale, bool allowTruncation)
    {
        if (sourceScale == targetScale) return value;

        if (typeof(T) == typeof(decimal))
        {
            decimal dVal = (decimal)(object)value;
            decimal dSrc = decimal.CreateChecked(sourceScale);
            decimal dDst = decimal.CreateChecked(targetScale);
            decimal res = (dVal * dSrc) / dDst;
            return (T)(object)res;
        }
        if (typeof(T) == typeof(double))
        {
            double dVal = (double)(object)value;
            return (T)(object)(dVal * (sourceScale / targetScale));
        }
        if (typeof(T) == typeof(float))
        {
            float fVal = (float)(object)value;
            return (T)(object)(float)(fVal * (sourceScale / targetScale));
        }
        if (typeof(T) == typeof(Half))
        {
            double hVal = (double)(Half)(object)value;
            return (T)(object)(Half)(hVal * (sourceScale / targetScale));
        }

        // Integral or custom INumber<T>: preserve exactness unless truncation was explicitly requested.
        double ratio = sourceScale / targetScale;
        if (ratio >= 1d && Math.Abs(ratio - Math.Round(ratio)) <= 1e-12 * ratio)
        {
            return value * T.CreateChecked(Math.Round(ratio));
        }
        double inv = targetScale / sourceScale;
        if (inv >= 1d && Math.Abs(inv - Math.Round(inv)) <= 1e-12 * inv)
        {
            T divisor = T.CreateChecked(Math.Round(inv));
            T quotient = value / divisor;
            if (!allowTruncation && quotient * divisor != value)
                throw new InvalidOperationException($"Lossy integral unit conversion from scale {sourceScale} to {targetScale}; use InTruncating or As<double>().");
            return quotient;
        }
        if (!allowTruncation)
            throw new InvalidOperationException($"Non-integral unit conversion ratio ({ratio}) for integral representation {typeof(T).Name}; use InTruncating or As<double>().");
        return T.CreateChecked(double.CreateChecked(value) * ratio);
    }
}

/// <summary>Affine physical quantity point anchored to a <see cref="PointOrigin"/> (for example °C, °F, gauge pressure, altitude).</summary>
/// <typeparam name="T">Numeric representation type (<see cref="INumber{TSelf}"/>).</typeparam>
/// <remarks>
/// Enforces affine-space algebra at compile time:
/// <list type="bullet">
/// <item><c>point - point</c> produces a displacement <see cref="Quantity{T}"/> (even across different origins of the same dimension).</item>
/// <item><c>point + displacement</c> and <c>point - displacement</c> produce a shifted <see cref="QuantityPoint{T}"/>.</item>
/// <item><c>point + point</c> and <c>point * scalar</c> do not exist.</item>
/// </list>
/// </remarks>
public readonly record struct QuantityPoint<T> :
    IComparable<QuantityPoint<T>>,
    IComparisonOperators<QuantityPoint<T>, QuantityPoint<T>, bool>,
    ISubtractionOperators<QuantityPoint<T>, QuantityPoint<T>, Quantity<T>>,
    IAdditionOperators<QuantityPoint<T>, Quantity<T>, QuantityPoint<T>>,
    ISubtractionOperators<QuantityPoint<T>, Quantity<T>, QuantityPoint<T>>
    where T : INumber<T>
{
    /// <summary>Linear displacement from <see cref="Origin"/>.</summary>
    public Quantity<T> Displacement { get; }

    /// <summary>Reference affine origin.</summary>
    public PointOrigin Origin { get; }

    /// <summary>Numeric value relative to <see cref="Origin"/> in <see cref="Unit"/>.</summary>
    public T Value => Displacement.Value;

    /// <summary>Linear unit measuring the displacement from <see cref="Origin"/>.</summary>
    public SymbolicUnit Unit => Displacement.Unit;

    /// <summary>Absolute position relative to the dimension's natural zero origin in coherent SI units.</summary>
    public double AbsoluteSiValue => Origin.OffsetFromBaseSi + Displacement.SiValue;

    /// <summary>Constructs an affine point from a displacement and compatible origin.</summary>
    public QuantityPoint(Quantity<T> displacement, PointOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        if (displacement.Dimension != origin.Dimension)
            throw new InvalidOperationException($"Displacement dimension disagrees with origin '{origin.Name}'.");
        if (displacement.Spec is not null && origin.Spec is not null &&
            QuantitySpec.CommonSpec(displacement.Spec, origin.Spec) is null)
            throw new InvalidOperationException($"Displacement kind '{displacement.Spec.Id}' disagrees with origin kind '{origin.Spec.Id}'.");
        Displacement = displacement;
        Origin = origin;
    }

    /// <summary>Creates an affine point at <paramref name="displacement"/> in <paramref name="unit"/> relative to <paramref name="origin"/>.</summary>
    public static QuantityPoint<T> At(T displacement, SymbolicUnit unit, PointOrigin origin) =>
        new(Quantity<T>.Of(displacement, unit, origin.Spec), origin);

    /// <summary>Creates a thermodynamic temperature point in kelvin (origin: <see cref="PointOrigins.AbsoluteZero"/>).</summary>
    public static QuantityPoint<T> FromKelvin(T kelvin) =>
        At(kelvin, Units.Kelvin, PointOrigins.AbsoluteZero);

    /// <summary>Creates a Celsius temperature point (origin: <see cref="PointOrigins.IcePoint"/>, stored without offset drift).</summary>
    public static QuantityPoint<T> FromCelsius(T celsius) =>
        At(celsius, Units.Kelvin, PointOrigins.IcePoint);

    /// <summary>Creates a Fahrenheit temperature point (origin: <see cref="PointOrigins.FahrenheitZero"/>, stored without offset drift).</summary>
    public static QuantityPoint<T> FromFahrenheit(T fahrenheit) =>
        At(fahrenheit, Units.Rankine, PointOrigins.FahrenheitZero);

    /// <summary>Creates a gauge pressure point relative to <see cref="PointOrigins.StandardAtmosphere"/>.</summary>
    public static QuantityPoint<T> FromGaugePressure(T value, SymbolicUnit pressureUnit) =>
        At(value, pressureUnit, PointOrigins.StandardAtmosphere);

    /// <summary>Creates an absolute pressure point relative to <see cref="PointOrigins.Vacuum"/>.</summary>
    public static QuantityPoint<T> FromAbsolutePressure(T value, SymbolicUnit pressureUnit) =>
        At(value, pressureUnit, PointOrigins.Vacuum);

    /// <summary>Re-expresses the displacement unit while keeping the same <see cref="Origin"/>.</summary>
    public QuantityPoint<T> In(SymbolicUnit targetLinearUnit) =>
        new(Displacement.In(targetLinearUnit), Origin);

    /// <summary>Re-anchors this point to a different compatible <see cref="PointOrigin"/> (for example gauge to absolute pressure, or °C to K).</summary>
    public QuantityPoint<T> RelativeTo(PointOrigin newOrigin)
    {
        ArgumentNullException.ThrowIfNull(newOrigin);
        if (newOrigin.Dimension != Origin.Dimension)
            throw new InvalidOperationException($"Incompatible origin dimensions: '{Origin.Name}' vs '{newOrigin.Name}'.");
        if (Origin == newOrigin) return this;
        double deltaOriginSi = Origin.OffsetFromBaseSi - newOrigin.OffsetFromBaseSi;
        T shiftInCurrentUnit = Quantity<T>.ConvertScale(
            FromDouble(deltaOriginSi),
            1d,
            Displacement.Unit.ScaleToSi,
            allowTruncation: false);
        return new(Quantity<T>.Of(Displacement.Value + shiftInCurrentUnit, Displacement.Unit, newOrigin.Spec ?? Displacement.Spec), newOrigin);
    }

    /// <summary>Converts to degrees Celsius (relative to <see cref="PointOrigins.IcePoint"/> in kelvin increments).</summary>
    public QuantityPoint<T> ToCelsius() => In(Units.Kelvin).RelativeTo(PointOrigins.IcePoint);

    /// <summary>Converts to kelvin (relative to <see cref="PointOrigins.AbsoluteZero"/>).</summary>
    public QuantityPoint<T> ToKelvin() => In(Units.Kelvin).RelativeTo(PointOrigins.AbsoluteZero);

    /// <summary>Converts to degrees Fahrenheit (relative to <see cref="PointOrigins.FahrenheitZero"/> in Rankine increments).</summary>
    public QuantityPoint<T> ToFahrenheit() => RelativeTo(PointOrigins.FahrenheitZero).In(Units.Rankine);

    /// <summary>Converts the underlying numeric representation.</summary>
    public QuantityPoint<TOther> As<TOther>() where TOther : INumber<TOther> =>
        new(Displacement.As<TOther>(), Origin);

    /// <summary>Drops this affine point to a raw <typeparamref name="T"/> scalar in <paramref name="targetLinearUnit"/> (optionally re-anchored to <paramref name="relativeTo"/>).</summary>
    public T ValueIn(SymbolicUnit targetLinearUnit, PointOrigin? relativeTo = null)
    {
        var p = relativeTo is not null ? RelativeTo(relativeTo) : this;
        return p.In(targetLinearUnit).Value;
    }

    /// <summary>Deconstructs this affine point into its numeric value, unit, and origin.</summary>
    public void Deconstruct(out T value, out SymbolicUnit unit, out PointOrigin origin)
    {
        value = Value;
        unit = Unit;
        origin = Origin;
    }

    /// <summary>Converts this affine point to a serializer-agnostic primitive <see cref="QuantityPointData{T}"/> wire record.</summary>
    public QuantityPointData<T> ToData(bool asciiSymbol = false) =>
        new(Value, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Origin.Id, Displacement.Spec?.Id);

    /// <summary>Reconstructs a validated <see cref="QuantityPoint{T}"/> from a serializer-agnostic <see cref="QuantityPointData{T}"/> record.</summary>
    public static QuantityPoint<T> FromData(QuantityPointData<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        var origin = env.ResolveOrigin(data.Origin);
        QuantitySpec? spec = origin.Spec;
        if (!string.IsNullOrWhiteSpace(data.Spec))
        {
            if (!Isq.TryGetById(data.Spec, out spec))
                spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        }
        return new(Quantity<T>.Of(data.Value, unit, spec), origin);
    }

    /// <summary>Affine midpoint <c>a + (b - a) / 2</c>.</summary>
    public static QuantityPoint<T> Midpoint(QuantityPoint<T> a, QuantityPoint<T> b)
    {
        T two = T.One + T.One;
        return a + ((b - a) / two);
    }

    /// <summary>Affine linear interpolation <c>a + (b - a) * t</c>.</summary>
    public static QuantityPoint<T> Lerp(QuantityPoint<T> a, QuantityPoint<T> b, T t) =>
        a + ((b - a) * t);

    /// <summary>Shifts a point forward by a displacement.</summary>
    public static QuantityPoint<T> operator +(QuantityPoint<T> point, Quantity<T> delta) =>
        new(point.Displacement + delta, point.Origin);

    /// <summary>Shifts a point forward by a displacement.</summary>
    public static QuantityPoint<T> operator +(Quantity<T> delta, QuantityPoint<T> point) =>
        point + delta;

    /// <summary>Shifts a point backward by a displacement.</summary>
    public static QuantityPoint<T> operator -(QuantityPoint<T> point, Quantity<T> delta) =>
        new(point.Displacement - delta, point.Origin);

    /// <summary>Displacement between two affine points (works across different compatible origins).</summary>
    public static Quantity<T> operator -(QuantityPoint<T> a, QuantityPoint<T> b)
    {
        if (a.Origin.Dimension != b.Origin.Dimension)
            throw new InvalidOperationException("Cannot subtract points of different dimensions.");
        if (a.Origin == b.Origin)
            return a.Displacement - b.Displacement;
        var bAligned = b.RelativeTo(a.Origin);
        return a.Displacement - bAligned.Displacement;
    }

    /// <summary>Compares compatible affine points even across different origins.</summary>
    public int CompareTo(QuantityPoint<T> other)
    {
        if (Origin.Dimension != other.Origin.Dimension)
            throw new InvalidOperationException("Cannot compare points of different dimensions.");
        if (Origin == other.Origin)
            return Displacement.CompareTo(other.Displacement);
        return Displacement.CompareTo(other.RelativeTo(Origin).Displacement);
    }

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(QuantityPoint<T> left, QuantityPoint<T> right) => left.CompareTo(right) < 0;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(QuantityPoint<T> left, QuantityPoint<T> right) => left.CompareTo(right) <= 0;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(QuantityPoint<T> left, QuantityPoint<T> right) => left.CompareTo(right) > 0;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(QuantityPoint<T> left, QuantityPoint<T> right) => left.CompareTo(right) >= 0;

    /// <summary>Returns a readable representation including origin context when non-zero.</summary>
    public override string ToString()
    {
        if (Origin == PointOrigins.IcePoint && Unit.Equals(SymbolicUnit.From(Units.Kelvin)))
            return $"{Value.ToString("G", CultureInfo.InvariantCulture)} °C";
        if (Origin == PointOrigins.FahrenheitZero && Unit.Equals(SymbolicUnit.From(Units.Rankine)))
            return $"{Value.ToString("G", CultureInfo.InvariantCulture)} °F";
        return Origin.OffsetFromBaseSi == 0d
            ? Displacement.ToString()
            : $"{Displacement} (rel. {Origin.Name})";
    }

    private static T FromDouble(double value)
    {
        if (typeof(T) == typeof(decimal)) return (T)(object)decimal.CreateChecked(value);
        return T.CreateChecked(value);
    }
}

/// <summary>Generic numerical algorithms for <see cref="Quantity{T}"/> and <see cref="QuantityPoint{T}"/> (plotting, statistics, clamping).</summary>
public static class QuantityMath
{
    /// <summary>Returns the absolute value of a quantity.</summary>
    public static Quantity<T> Abs<T>(Quantity<T> q) where T : INumber<T> =>
        new(T.Abs(q.Value), q.Unit, q.Spec);

    /// <summary>Returns the smaller of two compatible quantities.</summary>
    public static Quantity<T> Min<T>(Quantity<T> a, Quantity<T> b) where T : INumber<T> =>
        a <= b ? a : b;

    /// <summary>Returns the larger of two compatible quantities.</summary>
    public static Quantity<T> Max<T>(Quantity<T> a, Quantity<T> b) where T : INumber<T> =>
        a >= b ? a : b;

    /// <summary>Clamps <paramref name="value"/> to the inclusive range <c>[min, max]</c>.</summary>
    public static Quantity<T> Clamp<T>(Quantity<T> value, Quantity<T> min, Quantity<T> max) where T : INumber<T>
    {
        if (min > max) throw new ArgumentException("Minimum cannot exceed maximum.", nameof(min));
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    /// <summary>Linearly interpolates between two compatible quantities.</summary>
    public static Quantity<T> Lerp<T>(Quantity<T> a, Quantity<T> b, T t) where T : INumber<T> =>
        a + ((b - a) * t);

    /// <summary>Computes the sum of a non-empty span of compatible quantities.</summary>
    public static Quantity<T> Sum<T>(ReadOnlySpan<Quantity<T>> values) where T : INumber<T>
    {
        if (values.IsEmpty) throw new ArgumentException("Span cannot be empty.", nameof(values));
        var total = values[0];
        for (int i = 1; i < values.Length; i++) total += values[i];
        return total;
    }

    /// <summary>Computes the arithmetic mean of a non-empty span of compatible quantities.</summary>
    public static Quantity<T> Mean<T>(ReadOnlySpan<Quantity<T>> values) where T : INumber<T>
    {
        var total = Sum(values);
        return total / T.CreateChecked(values.Length);
    }

    /// <summary>Computes the minimum and maximum of a non-empty span of compatible quantities (ideal for plot axis auto-ranging).</summary>
    public static (Quantity<T> Min, Quantity<T> Max) MinMax<T>(ReadOnlySpan<Quantity<T>> values) where T : INumber<T>
    {
        if (values.IsEmpty) throw new ArgumentException("Span cannot be empty.", nameof(values));
        var min = values[0];
        var max = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }
        return (min, max);
    }
}
