using System.Globalization;
using System.Numerics;

namespace SharpUnits;

/// <summary>Named reference frame / origin for 2-D and 3-D affine physical points (for example World, ENU, NED, Body, or a sensor frame).</summary>
public sealed record SpatialOrigin
{
    /// <summary>Stable unique frame identifier.</summary>
    public string Id { get; }

    /// <summary>Human-readable frame name.</summary>
    public string Name { get; }

    /// <summary>Constructs a validated spatial reference frame.</summary>
    public SpatialOrigin(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Frame ID required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Frame name required.", nameof(name));
        Id = id;
        Name = name;
    }

    /// <summary>Returns the frame name.</summary>
    public override string ToString() => Name;
}

/// <summary>Standard spatial reference frames for 2-D and 3-D physical geometry.</summary>
public static class SpatialOrigins
{
    /// <summary>Default world / laboratory origin frame.</summary>
    public static SpatialOrigin World { get; } = new("frame.world", "world");

    /// <summary>East-North-Up (ENU) local tangent frame.</summary>
    public static SpatialOrigin Enu { get; } = new("frame.enu", "ENU");

    /// <summary>North-East-Down (NED) aerospace navigation frame.</summary>
    public static SpatialOrigin Ned { get; } = new("frame.ned", "NED");

    /// <summary>Vehicle / instrument body-fixed frame.</summary>
    public static SpatialOrigin Body { get; } = new("frame.body", "body");

    /// <summary>Resolves a well-known frame by ID or name, or creates a custom frame with the supplied identifier.</summary>
    public static SpatialOrigin ResolveOrCreate(string idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName)) return World;
        return idOrName.Trim().ToLowerInvariant() switch
        {
            "world" or "frame.world" => World,
            "enu" or "frame.enu" => Enu,
            "ned" or "frame.ned" => Ned,
            "body" or "frame.body" => Body,
            _ => new SpatialOrigin(idOrName.Trim(), idOrName.Trim())
        };
    }
}

/// <summary>
/// 2-D physical vector pairing <c>(X, Y)</c> components with a single <see cref="SymbolicUnit"/> and optional ISO 80000 <see cref="QuantitySpec"/>.
/// </summary>
/// <typeparam name="T">Numeric representation type (<see cref="INumber{TSelf}"/>).</typeparam>
public readonly record struct QuantityVector2<T> :
    IAdditionOperators<QuantityVector2<T>, QuantityVector2<T>, QuantityVector2<T>>,
    ISubtractionOperators<QuantityVector2<T>, QuantityVector2<T>, QuantityVector2<T>>,
    IUnaryNegationOperators<QuantityVector2<T>, QuantityVector2<T>>,
    IUnaryPlusOperators<QuantityVector2<T>, QuantityVector2<T>>,
    IMultiplyOperators<QuantityVector2<T>, T, QuantityVector2<T>>,
    IDivisionOperators<QuantityVector2<T>, T, QuantityVector2<T>>
    where T : INumber<T>
{
    /// <summary>X component expressed in <see cref="Unit"/>.</summary>
    public T X { get; }

    /// <summary>Y component expressed in <see cref="Unit"/>.</summary>
    public T Y { get; }

    /// <summary>Shared algebraic symbolic unit of both components.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Optional ISO 80000 ISQ specification.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Canonical exact rational dimension.</summary>
    public RationalDimension Dimension => Unit.Dimension;

    /// <summary>X component as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> XQuantity => new(X, Unit, Spec);

    /// <summary>Y component as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> YQuantity => new(Y, Unit, Spec);

    /// <summary>Constructs a validated linear 2-D physical vector.</summary>
    public QuantityVector2(T x, T y, SymbolicUnit unit, QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.IsAffine)
            throw new InvalidOperationException("Physical vectors require a linear unit.");
        var resolvedSpec = spec ?? unit.DefaultSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Specification '{resolvedSpec.Id}' dimension disagrees with unit '{unit.Symbol}'.");
        X = x;
        Y = y;
        Unit = unit;
        Spec = resolvedSpec;
    }

    /// <summary>Creates a 2-D physical vector in <paramref name="unit"/>.</summary>
    public static QuantityVector2<T> Of(T x, T y, SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(x, y, unit, spec);

    /// <summary>Creates a 2-D physical vector by resolving <paramref name="unitSymbol"/>.</summary>
    public static QuantityVector2<T> Of(T x, T y, string unitSymbol, UnitRegistry? registry = null, QuantitySpec? spec = null) =>
        new(x, y, SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)), spec);

    /// <summary>Creates a zero 2-D physical vector in <paramref name="unit"/>.</summary>
    public static QuantityVector2<T> Zero(SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(T.Zero, T.Zero, unit, spec);

    /// <summary>Creates a 2-D physical vector from two compatible component quantities, converting to their finer common unit.</summary>
    public static QuantityVector2<T> FromQuantities(Quantity<T> x, Quantity<T> y)
    {
        var (commonUnit, commonSpec) = ResolvePairMetadata(x.Unit, x.Spec, y.Unit, y.Spec);
        return new(x.In(commonUnit).Value, y.In(commonUnit).Value, commonUnit, commonSpec);
    }

    /// <summary>Creates a 2-D physical vector from polar magnitude and plane angle.</summary>
    public static QuantityVector2<double> FromPolar(Quantity<double> magnitude, Quantity<double> angle)
    {
        double rad = angle.In(Units.Radian).Value;
        return new(magnitude.Value * Math.Cos(rad), magnitude.Value * Math.Sin(rad), magnitude.Unit, magnitude.Spec);
    }

    /// <summary>Lifts a raw <see cref="Vector2"/> into a unit-safe <see cref="QuantityVector2{T}"/>.</summary>
    public static QuantityVector2<T> FromVector2(Vector2 raw, SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(T.CreateChecked(raw.X), T.CreateChecked(raw.Y), unit, spec);

    /// <summary>Converts both components to a compatible linear target unit.</summary>
    public QuantityVector2<T> In(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine) throw new InvalidOperationException("Target unit must be linear.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        if (Unit.Equals(target)) return this;
        T nx = Quantity<T>.ConvertScale(X, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        T ny = Quantity<T>.ConvertScale(Y, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        return new(nx, ny, target, Spec);
    }

    /// <summary>Converts both components to <paramref name="unitSymbol"/>.</summary>
    public QuantityVector2<T> In(string unitSymbol, UnitRegistry? registry = null) =>
        In(SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)));

    /// <summary>Converts the numeric representation type (for example <c>double</c> to <c>float</c>).</summary>
    public QuantityVector2<TOther> As<TOther>() where TOther : INumber<TOther> =>
        new(TOther.CreateChecked(X), TOther.CreateChecked(Y), Unit, Spec);

    /// <summary>Drops this vector to a raw <see cref="Vector2"/> in its current <see cref="Unit"/>.</summary>
    public Vector2 ToVector2() => new(float.CreateChecked(X), float.CreateChecked(Y));

    /// <summary>Drops this vector to a raw <see cref="Vector2"/> explicitly converted into <paramref name="target"/>.</summary>
    public Vector2 ToVector2In(SymbolicUnit target) => In(target).ToVector2();

    /// <summary>Drops this vector to a raw <see cref="Vector2"/> explicitly converted into <paramref name="unitSymbol"/>.</summary>
    public Vector2 ToVector2In(string unitSymbol, UnitRegistry? registry = null) => In(unitSymbol, registry).ToVector2();

    /// <summary>Deconstructs into component quantities.</summary>
    public void Deconstruct(out Quantity<T> x, out Quantity<T> y)
    {
        x = XQuantity;
        y = YQuantity;
    }

    /// <summary>Deconstructs into raw components and shared <see cref="SymbolicUnit"/>.</summary>
    public void Deconstruct(out T x, out T y, out SymbolicUnit unit)
    {
        x = X;
        y = Y;
        unit = Unit;
    }

    /// <summary>Converts this vector to a serializer-agnostic primitive <see cref="QuantityVector2Data{T}"/> wire record.</summary>
    public QuantityVector2Data<T> ToData(bool asciiSymbol = false) =>
        new(X, Y, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Spec?.Id);

    /// <summary>Reconstructs a validated <see cref="QuantityVector2{T}"/> from a primitive <see cref="QuantityVector2Data{T}"/> record.</summary>
    public static QuantityVector2<T> FromData(QuantityVector2Data<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        return new(data.X, data.Y, unit, spec);
    }

    /// <summary>Euclidean vector magnitude <c>√(X² + Y²)</c> preserving <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public Quantity<double> Magnitude() =>
        Quantity<double>.Of(double.Hypot(double.CreateChecked(X), double.CreateChecked(Y)), Unit, Spec);

    /// <summary>Squared magnitude <c>X² + Y²</c> with squared symbolic unit (<c>Unit²</c>).</summary>
    public Quantity<T> MagnitudeSquared() =>
        Quantity<T>.Of((X * X) + (Y * Y), Unit.Pow(2));

    /// <summary>Polar azimuth angle <c>atan2(Y, X)</c> in radians.</summary>
    public Quantity<double> Angle() =>
        Quantity<double>.Of(Math.Atan2(double.CreateChecked(Y), double.CreateChecked(X)), Units.Radian, Isq.AngularMeasure);

    /// <summary>Returns a dimensionless unit direction vector (<c>|v| = 1</c>).</summary>
    public QuantityVector2<double> Normalize()
    {
        double mag = double.Hypot(double.CreateChecked(X), double.CreateChecked(Y));
        if (mag == 0d) throw new InvalidOperationException("Cannot normalize a zero-length physical vector.");
        return new(double.CreateChecked(X) / mag, double.CreateChecked(Y) / mag, SymbolicUnit.One, Isq.Dimensionless);
    }

    /// <summary>Returns the raw normalized <see cref="Vector2"/> direction.</summary>
    public Vector2 Direction() => Normalize().ToVector2();

    /// <summary>Rotates this vector counter-clockwise by <paramref name="angle"/>, preserving <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public QuantityVector2<double> Rotate(Quantity<double> angle)
    {
        double rad = angle.In(Units.Radian).Value;
        double c = Math.Cos(rad);
        double s = Math.Sin(rad);
        double dx = double.CreateChecked(X);
        double dy = double.CreateChecked(Y);
        return new((dx * c) - (dy * s), (dx * s) + (dy * c), Unit, Spec);
    }

    /// <summary>Dot product multiplying symbolic units (for example <c>Force · Displacement → N·m</c>).</summary>
    public static Quantity<T> Dot(QuantityVector2<T> a, QuantityVector2<T> b) =>
        Quantity<T>.Of((a.X * b.X) + (a.Y * b.Y), a.Unit * b.Unit);

    /// <summary>2-D perpendicular cross product <c>a.X * b.Y - a.Y * b.X</c> (infers <see cref="Isq.Torque"/> for <c>Length × Force</c>).</summary>
    public static Quantity<T> CrossZ(QuantityVector2<T> a, QuantityVector2<T> b)
    {
        var resultUnit = a.Unit * b.Unit;
        QuantitySpec? spec = InferTorqueSpec(a.Dimension, b.Dimension);
        return Quantity<T>.Of((a.X * b.Y) - (a.Y * b.X), resultUnit, spec);
    }

    /// <summary>Angle between two non-zero 2-D physical vectors in radians.</summary>
    public static Quantity<double> AngleBetween(QuantityVector2<T> a, QuantityVector2<T> b)
    {
        var na = a.Normalize();
        var nb = b.Normalize();
        double cos = Math.Clamp((na.X * nb.X) + (na.Y * nb.Y), -1d, 1d);
        return Quantity<double>.Of(Math.Acos(cos), Units.Radian, Isq.AngularMeasure);
    }

    /// <summary>Projects this vector onto the direction of <paramref name="direction"/>, preserving <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public QuantityVector2<double> ProjectOnto<TDir>(QuantityVector2<TDir> direction) where TDir : INumber<TDir>
    {
        var unitDir = direction.Normalize();
        double proj = (double.CreateChecked(X) * unitDir.X) + (double.CreateChecked(Y) * unitDir.Y);
        return new(proj * unitDir.X, proj * unitDir.Y, Unit, Spec);
    }

    /// <summary>Adds two compatible 2-D physical vectors in their finer common unit.</summary>
    public static QuantityVector2<T> operator +(QuantityVector2<T> a, QuantityVector2<T> b)
    {
        var (commonUnit, commonSpec) = ResolvePairMetadata(a.Unit, a.Spec, b.Unit, b.Spec);
        var aa = a.In(commonUnit);
        var bb = b.In(commonUnit);
        return new(aa.X + bb.X, aa.Y + bb.Y, commonUnit, commonSpec);
    }

    /// <summary>Subtracts two compatible 2-D physical vectors in their finer common unit.</summary>
    public static QuantityVector2<T> operator -(QuantityVector2<T> a, QuantityVector2<T> b)
    {
        var (commonUnit, commonSpec) = ResolvePairMetadata(a.Unit, a.Spec, b.Unit, b.Spec);
        var aa = a.In(commonUnit);
        var bb = b.In(commonUnit);
        return new(aa.X - bb.X, aa.Y - bb.Y, commonUnit, commonSpec);
    }

    /// <summary>Unary plus.</summary>
    public static QuantityVector2<T> operator +(QuantityVector2<T> v) => v;

    /// <summary>Unary negation.</summary>
    public static QuantityVector2<T> operator -(QuantityVector2<T> v) => new(-v.X, -v.Y, v.Unit, v.Spec);

    /// <summary>Scales by a dimensionless scalar.</summary>
    public static QuantityVector2<T> operator *(QuantityVector2<T> v, T scalar) => new(v.X * scalar, v.Y * scalar, v.Unit, v.Spec);

    /// <summary>Scales by a dimensionless scalar.</summary>
    public static QuantityVector2<T> operator *(T scalar, QuantityVector2<T> v) => v * scalar;

    /// <summary>Divides by a dimensionless scalar.</summary>
    public static QuantityVector2<T> operator /(QuantityVector2<T> v, T divisor) => new(v.X / divisor, v.Y / divisor, v.Unit, v.Spec);

    /// <summary>Multiplies a 2-D physical vector by a physical scalar quantity (for example <c>Mass * Acceleration → Force</c>).</summary>
    public static QuantityVector2<T> operator *(QuantityVector2<T> v, Quantity<T> scalar) =>
        new(v.X * scalar.Value, v.Y * scalar.Value, v.Unit * scalar.Unit);

    /// <summary>Multiplies a physical scalar quantity by a 2-D physical vector.</summary>
    public static QuantityVector2<T> operator *(Quantity<T> scalar, QuantityVector2<T> v) => v * scalar;

    /// <summary>Divides a 2-D physical vector by a physical scalar quantity (for example <c>Displacement / Duration → Velocity</c>).</summary>
    public static QuantityVector2<T> operator /(QuantityVector2<T> v, Quantity<T> divisor) =>
        new(v.X / divisor.Value, v.Y / divisor.Value, v.Unit / divisor.Unit);

    /// <summary>Formats as <c>(X, Y) unit</c>.</summary>
    public override string ToString() =>
        $"({X.ToString("G", CultureInfo.InvariantCulture)}, {Y.ToString("G", CultureInfo.InvariantCulture)}) {Unit.Symbol}";

    internal static (SymbolicUnit Unit, QuantitySpec? Spec) ResolvePairMetadata(
        SymbolicUnit aUnit, QuantitySpec? aSpec, SymbolicUnit bUnit, QuantitySpec? bSpec)
    {
        if (!aUnit.IsCompatibleWith(bUnit))
            throw new InvalidOperationException($"Incompatible dimensions: '{aUnit.Symbol}' vs '{bUnit.Symbol}'.");
        QuantitySpec? commonSpec = aSpec ?? bSpec;
        if (aSpec is not null && bSpec is not null)
        {
            commonSpec = QuantitySpec.CommonSpec(aSpec, bSpec)
                ?? throw new InvalidOperationException($"Incompatible ISQ quantity kinds: '{aSpec.Id}' vs '{bSpec.Id}'.");
        }
        return (SymbolicUnit.CommonUnit(aUnit, bUnit), commonSpec);
    }

    internal static QuantitySpec? InferTorqueSpec(RationalDimension aDim, RationalDimension bDim) =>
        aDim == RationalDimension.LengthDimension && bDim == RationalDimension.Force ? Isq.Torque : null;
}

/// <summary>
/// 3-D physical vector pairing <c>(X, Y, Z)</c> components with a single <see cref="SymbolicUnit"/> and optional ISO 80000 <see cref="QuantitySpec"/>.
/// Supports dimension-safe <see cref="Dot"/>, <see cref="Cross"/> (automatically inferring <see cref="Isq.Torque"/> for <c>r × F</c>),
/// <see cref="Rotate"/> by <see cref="Quaternion"/>, and zero-ceremony drop-down to <see cref="Vector3"/>.
/// </summary>
/// <typeparam name="T">Numeric representation type (<see cref="INumber{TSelf}"/>).</typeparam>
public readonly record struct QuantityVector3<T> :
    IAdditionOperators<QuantityVector3<T>, QuantityVector3<T>, QuantityVector3<T>>,
    ISubtractionOperators<QuantityVector3<T>, QuantityVector3<T>, QuantityVector3<T>>,
    IUnaryNegationOperators<QuantityVector3<T>, QuantityVector3<T>>,
    IUnaryPlusOperators<QuantityVector3<T>, QuantityVector3<T>>,
    IMultiplyOperators<QuantityVector3<T>, T, QuantityVector3<T>>,
    IDivisionOperators<QuantityVector3<T>, T, QuantityVector3<T>>
    where T : INumber<T>
{
    /// <summary>X component expressed in <see cref="Unit"/>.</summary>
    public T X { get; }

    /// <summary>Y component expressed in <see cref="Unit"/>.</summary>
    public T Y { get; }

    /// <summary>Z component expressed in <see cref="Unit"/>.</summary>
    public T Z { get; }

    /// <summary>Shared algebraic symbolic unit of all three components.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Optional ISO 80000 ISQ specification.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Canonical exact rational dimension.</summary>
    public RationalDimension Dimension => Unit.Dimension;

    /// <summary>X component as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> XQuantity => new(X, Unit, Spec);

    /// <summary>Y component as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> YQuantity => new(Y, Unit, Spec);

    /// <summary>Z component as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> ZQuantity => new(Z, Unit, Spec);

    /// <summary>Constructs a validated linear 3-D physical vector.</summary>
    public QuantityVector3(T x, T y, T z, SymbolicUnit unit, QuantitySpec? spec = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.IsAffine)
            throw new InvalidOperationException("Physical vectors require a linear unit.");
        var resolvedSpec = spec ?? unit.DefaultSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Specification '{resolvedSpec.Id}' dimension disagrees with unit '{unit.Symbol}'.");
        X = x;
        Y = y;
        Z = z;
        Unit = unit;
        Spec = resolvedSpec;
    }

    /// <summary>Creates a 3-D physical vector in <paramref name="unit"/>.</summary>
    public static QuantityVector3<T> Of(T x, T y, T z, SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(x, y, z, unit, spec);

    /// <summary>Creates a 3-D physical vector by resolving <paramref name="unitSymbol"/>.</summary>
    public static QuantityVector3<T> Of(T x, T y, T z, string unitSymbol, UnitRegistry? registry = null, QuantitySpec? spec = null) =>
        new(x, y, z, SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)), spec);

    /// <summary>Creates a zero 3-D physical vector in <paramref name="unit"/>.</summary>
    public static QuantityVector3<T> Zero(SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(T.Zero, T.Zero, T.Zero, unit, spec);

    /// <summary>Creates a 3-D physical vector from three compatible component quantities in their finest common unit.</summary>
    public static QuantityVector3<T> FromQuantities(Quantity<T> x, Quantity<T> y, Quantity<T> z)
    {
        var (xyUnit, xySpec) = QuantityVector2<T>.ResolvePairMetadata(x.Unit, x.Spec, y.Unit, y.Spec);
        var (commonUnit, commonSpec) = QuantityVector2<T>.ResolvePairMetadata(xyUnit, xySpec, z.Unit, z.Spec);
        return new(x.In(commonUnit).Value, y.In(commonUnit).Value, z.In(commonUnit).Value, commonUnit, commonSpec);
    }

    /// <summary>Creates a 3-D physical vector from spherical coordinates (radius <paramref name="r"/>, polar inclination <paramref name="polar"/> from +Z, azimuth <paramref name="azimuth"/> in XY).</summary>
    public static QuantityVector3<double> FromSpherical(Quantity<double> r, Quantity<double> polar, Quantity<double> azimuth)
    {
        double theta = polar.In(Units.Radian).Value;
        double phi = azimuth.In(Units.Radian).Value;
        double sinT = Math.Sin(theta);
        return new(
            r.Value * sinT * Math.Cos(phi),
            r.Value * sinT * Math.Sin(phi),
            r.Value * Math.Cos(theta),
            r.Unit,
            r.Spec);
    }

    /// <summary>Lifts a raw <see cref="Vector3"/> into a unit-safe <see cref="QuantityVector3{T}"/>.</summary>
    public static QuantityVector3<T> FromVector3(Vector3 raw, SymbolicUnit unit, QuantitySpec? spec = null) =>
        new(T.CreateChecked(raw.X), T.CreateChecked(raw.Y), T.CreateChecked(raw.Z), unit, spec);

    /// <summary>Converts all three components to a compatible linear target unit.</summary>
    public QuantityVector3<T> In(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine) throw new InvalidOperationException("Target unit must be linear.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        if (Unit.Equals(target)) return this;
        T nx = Quantity<T>.ConvertScale(X, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        T ny = Quantity<T>.ConvertScale(Y, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        T nz = Quantity<T>.ConvertScale(Z, Unit.ScaleToSi, target.ScaleToSi, allowTruncation: false);
        return new(nx, ny, nz, target, Spec);
    }

    /// <summary>Converts all three components to <paramref name="unitSymbol"/>.</summary>
    public QuantityVector3<T> In(string unitSymbol, UnitRegistry? registry = null) =>
        In(SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)));

    /// <summary>Converts the underlying numeric representation type.</summary>
    public QuantityVector3<TOther> As<TOther>() where TOther : INumber<TOther> =>
        new(TOther.CreateChecked(X), TOther.CreateChecked(Y), TOther.CreateChecked(Z), Unit, Spec);

    /// <summary>Drops this vector to a raw <see cref="Vector3"/> in its current <see cref="Unit"/>.</summary>
    public Vector3 ToVector3() => new(float.CreateChecked(X), float.CreateChecked(Y), float.CreateChecked(Z));

    /// <summary>Drops this vector to a raw <see cref="Vector3"/> explicitly converted into <paramref name="target"/>.</summary>
    public Vector3 ToVector3In(SymbolicUnit target) => In(target).ToVector3();

    /// <summary>Drops this vector to a raw <see cref="Vector3"/> explicitly converted into <paramref name="unitSymbol"/>.</summary>
    public Vector3 ToVector3In(string unitSymbol, UnitRegistry? registry = null) => In(unitSymbol, registry).ToVector3();

    /// <summary>Deconstructs into component quantities.</summary>
    public void Deconstruct(out Quantity<T> x, out Quantity<T> y, out Quantity<T> z)
    {
        x = XQuantity;
        y = YQuantity;
        z = ZQuantity;
    }

    /// <summary>Deconstructs into raw components and shared <see cref="SymbolicUnit"/>.</summary>
    public void Deconstruct(out T x, out T y, out T z, out SymbolicUnit unit)
    {
        x = X;
        y = Y;
        z = Z;
        unit = Unit;
    }

    /// <summary>Converts this vector to a serializer-agnostic primitive <see cref="QuantityVector3Data{T}"/> wire record.</summary>
    public QuantityVector3Data<T> ToData(bool asciiSymbol = false) =>
        new(X, Y, Z, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Spec?.Id);

    /// <summary>Reconstructs a validated <see cref="QuantityVector3{T}"/> from a primitive <see cref="QuantityVector3Data{T}"/> record.</summary>
    public static QuantityVector3<T> FromData(QuantityVector3Data<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        return new(data.X, data.Y, data.Z, unit, spec);
    }

    /// <summary>Euclidean vector magnitude <c>√(X² + Y² + Z²)</c> preserving <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public Quantity<double> Magnitude()
    {
        double dx = double.CreateChecked(X);
        double dy = double.CreateChecked(Y);
        double dz = double.CreateChecked(Z);
        double mag = double.Hypot(double.Hypot(dx, dy), dz);
        return Quantity<double>.Of(mag, Unit, Spec);
    }

    /// <summary>Squared magnitude <c>X² + Y² + Z²</c> with squared symbolic unit (<c>Unit²</c>).</summary>
    public Quantity<T> MagnitudeSquared() =>
        Quantity<T>.Of((X * X) + (Y * Y) + (Z * Z), Unit.Pow(2));

    /// <summary>Returns a dimensionless unit direction vector (<c>|v| = 1</c>).</summary>
    public QuantityVector3<double> Normalize()
    {
        double mag = Magnitude().Value;
        if (mag == 0d) throw new InvalidOperationException("Cannot normalize a zero-length physical vector.");
        return new(
            double.CreateChecked(X) / mag,
            double.CreateChecked(Y) / mag,
            double.CreateChecked(Z) / mag,
            SymbolicUnit.One,
            Isq.Dimensionless);
    }

    /// <summary>Returns the raw normalized <see cref="Vector3"/> direction.</summary>
    public Vector3 Direction() => Normalize().ToVector3();

    /// <summary>Rotates this 3-D physical vector by a <see cref="Quaternion"/> (automatically normalized), preserving exact <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public QuantityVector3<double> Rotate(Quaternion rotation)
    {
        var q = Quaternion.Normalize(rotation);
        double qx = q.X, qy = q.Y, qz = q.Z, qw = q.W;
        double vx = double.CreateChecked(X);
        double vy = double.CreateChecked(Y);
        double vz = double.CreateChecked(Z);

        // v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + qw * v)
        double tx = 2d * ((qy * vz) - (qz * vy));
        double ty = 2d * ((qz * vx) - (qx * vz));
        double tz = 2d * ((qx * vy) - (qy * vx));

        double rx = vx + (qw * tx) + ((qy * tz) - (qz * ty));
        double ry = vy + (qw * ty) + ((qz * tx) - (qx * tz));
        double rz = vz + (qw * tz) + ((qx * ty) - (qy * tx));
        return new(rx, ry, rz, Unit, Spec);
    }

    /// <summary>Dot product multiplying symbolic units (for example <c>Force · Displacement → N·m</c>).</summary>
    public static Quantity<T> Dot(QuantityVector3<T> a, QuantityVector3<T> b) =>
        Quantity<T>.Of((a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z), a.Unit * b.Unit);

    /// <summary>
    /// 3-D cross product multiplying symbolic units.
    /// Automatically tags <see cref="Isq.Torque"/> when computing <c>Position/LeverArm × Force</c>.
    /// </summary>
    public static QuantityVector3<T> Cross(QuantityVector3<T> a, QuantityVector3<T> b)
    {
        var resultUnit = a.Unit * b.Unit;
        QuantitySpec? spec = QuantityVector2<T>.InferTorqueSpec(a.Dimension, b.Dimension);
        return new(
            (a.Y * b.Z) - (a.Z * b.Y),
            (a.Z * b.X) - (a.X * b.Z),
            (a.X * b.Y) - (a.Y * b.X),
            resultUnit,
            spec);
    }

    /// <summary>Angle between two non-zero 3-D physical vectors in radians.</summary>
    public static Quantity<double> AngleBetween(QuantityVector3<T> a, QuantityVector3<T> b)
    {
        var na = a.Normalize();
        var nb = b.Normalize();
        double cos = Math.Clamp((na.X * nb.X) + (na.Y * nb.Y) + (na.Z * nb.Z), -1d, 1d);
        return Quantity<double>.Of(Math.Acos(cos), Units.Radian, Isq.AngularMeasure);
    }

    /// <summary>Projects this vector onto the direction of <paramref name="direction"/>, preserving <see cref="Unit"/> and <see cref="Spec"/>.</summary>
    public QuantityVector3<double> ProjectOnto<TDir>(QuantityVector3<TDir> direction) where TDir : INumber<TDir>
    {
        var unitDir = direction.Normalize();
        double proj = (double.CreateChecked(X) * unitDir.X) + (double.CreateChecked(Y) * unitDir.Y) + (double.CreateChecked(Z) * unitDir.Z);
        return new(proj * unitDir.X, proj * unitDir.Y, proj * unitDir.Z, Unit, Spec);
    }

    /// <summary>Adds two compatible 3-D physical vectors in their finer common unit.</summary>
    public static QuantityVector3<T> operator +(QuantityVector3<T> a, QuantityVector3<T> b)
    {
        var (commonUnit, commonSpec) = QuantityVector2<T>.ResolvePairMetadata(a.Unit, a.Spec, b.Unit, b.Spec);
        var aa = a.In(commonUnit);
        var bb = b.In(commonUnit);
        return new(aa.X + bb.X, aa.Y + bb.Y, aa.Z + bb.Z, commonUnit, commonSpec);
    }

    /// <summary>Subtracts two compatible 3-D physical vectors in their finer common unit.</summary>
    public static QuantityVector3<T> operator -(QuantityVector3<T> a, QuantityVector3<T> b)
    {
        var (commonUnit, commonSpec) = QuantityVector2<T>.ResolvePairMetadata(a.Unit, a.Spec, b.Unit, b.Spec);
        var aa = a.In(commonUnit);
        var bb = b.In(commonUnit);
        return new(aa.X - bb.X, aa.Y - bb.Y, aa.Z - bb.Z, commonUnit, commonSpec);
    }

    /// <summary>Unary plus.</summary>
    public static QuantityVector3<T> operator +(QuantityVector3<T> v) => v;

    /// <summary>Unary negation.</summary>
    public static QuantityVector3<T> operator -(QuantityVector3<T> v) => new(-v.X, -v.Y, -v.Z, v.Unit, v.Spec);

    /// <summary>Scales by a dimensionless scalar.</summary>
    public static QuantityVector3<T> operator *(QuantityVector3<T> v, T scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar, v.Unit, v.Spec);

    /// <summary>Scales by a dimensionless scalar.</summary>
    public static QuantityVector3<T> operator *(T scalar, QuantityVector3<T> v) => v * scalar;

    /// <summary>Divides by a dimensionless scalar.</summary>
    public static QuantityVector3<T> operator /(QuantityVector3<T> v, T divisor) => new(v.X / divisor, v.Y / divisor, v.Z / divisor, v.Unit, v.Spec);

    /// <summary>Multiplies a 3-D physical vector by a physical scalar quantity (for example <c>Mass * Acceleration → Force</c>).</summary>
    public static QuantityVector3<T> operator *(QuantityVector3<T> v, Quantity<T> scalar) =>
        new(v.X * scalar.Value, v.Y * scalar.Value, v.Z * scalar.Value, v.Unit * scalar.Unit);

    /// <summary>Multiplies a physical scalar quantity by a 3-D physical vector.</summary>
    public static QuantityVector3<T> operator *(Quantity<T> scalar, QuantityVector3<T> v) => v * scalar;

    /// <summary>Divides a 3-D physical vector by a physical scalar quantity (for example <c>Displacement / Duration → Velocity</c>).</summary>
    public static QuantityVector3<T> operator /(QuantityVector3<T> v, Quantity<T> divisor) =>
        new(v.X / divisor.Value, v.Y / divisor.Value, v.Z / divisor.Value, v.Unit / divisor.Unit);

    /// <summary>Formats as <c>(X, Y, Z) unit</c>.</summary>
    public override string ToString() =>
        $"({X.ToString("G", CultureInfo.InvariantCulture)}, {Y.ToString("G", CultureInfo.InvariantCulture)}, {Z.ToString("G", CultureInfo.InvariantCulture)}) {Unit.Symbol}";
}

/// <summary>
/// Affine 2-D spatial point anchored to a <see cref="SpatialOrigin"/> frame.
/// Enforces affine geometry at compile time: <c>point - point → QuantityVector2&lt;T&gt;</c> and <c>point ± vector → QuantityPoint2&lt;T&gt;</c>,
/// while <c>point + point</c> and <c>point * scalar</c> do not compile.
/// </summary>
public readonly record struct QuantityPoint2<T> :
    ISubtractionOperators<QuantityPoint2<T>, QuantityPoint2<T>, QuantityVector2<T>>,
    IAdditionOperators<QuantityPoint2<T>, QuantityVector2<T>, QuantityPoint2<T>>,
    ISubtractionOperators<QuantityPoint2<T>, QuantityVector2<T>, QuantityPoint2<T>>
    where T : INumber<T>
{
    /// <summary>Position displacement vector from <see cref="Frame"/>.</summary>
    public QuantityVector2<T> Position { get; }

    /// <summary>Reference spatial frame.</summary>
    public SpatialOrigin Frame { get; }

    /// <summary>X coordinate in <see cref="Unit"/>.</summary>
    public T X => Position.X;

    /// <summary>Y coordinate in <see cref="Unit"/>.</summary>
    public T Y => Position.Y;

    /// <summary>Linear unit of coordinates.</summary>
    public SymbolicUnit Unit => Position.Unit;

    /// <summary>Constructs an affine 2-D spatial point in <paramref name="frame"/>.</summary>
    public QuantityPoint2(QuantityVector2<T> position, SpatialOrigin? frame = null)
    {
        Position = position;
        Frame = frame ?? SpatialOrigins.World;
    }

    /// <summary>Creates a 2-D spatial point at <c>(x, y)</c> in <paramref name="unit"/> relative to <paramref name="frame"/>.</summary>
    public static QuantityPoint2<T> At(T x, T y, SymbolicUnit unit, SpatialOrigin? frame = null, QuantitySpec? spec = null) =>
        new(new QuantityVector2<T>(x, y, unit, spec), frame ?? SpatialOrigins.World);

    /// <summary>Re-expresses this point in <paramref name="targetUnit"/> while preserving <see cref="Frame"/>.</summary>
    public QuantityPoint2<T> In(SymbolicUnit targetUnit) => new(Position.In(targetUnit), Frame);

    /// <summary>Drops coordinates to a raw <see cref="Vector2"/> explicitly converted into <paramref name="targetUnit"/>.</summary>
    public Vector2 ToVector2In(SymbolicUnit targetUnit) => Position.ToVector2In(targetUnit);

    /// <summary>Deconstructs into coordinates, unit, and frame.</summary>
    public void Deconstruct(out T x, out T y, out SymbolicUnit unit, out SpatialOrigin frame)
    {
        x = X;
        y = Y;
        unit = Unit;
        frame = Frame;
    }

    /// <summary>Converts to a serializer-agnostic primitive <see cref="QuantityPoint2Data{T}"/> wire record.</summary>
    public QuantityPoint2Data<T> ToData(bool asciiSymbol = false) =>
        new(X, Y, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Frame.Id, Position.Spec?.Id);

    /// <summary>Reconstructs from a primitive <see cref="QuantityPoint2Data{T}"/> wire record.</summary>
    public static QuantityPoint2<T> FromData(QuantityPoint2Data<T> data, UnitEnvironment? environment = null)
    {
        var vec = QuantityVector2<T>.FromData(new QuantityVector2Data<T>(data.X, data.Y, data.Unit, data.Spec), environment);
        return new(vec, SpatialOrigins.ResolveOrCreate(data.Frame));
    }

    /// <summary>Euclidean distance between two points in the same <see cref="SpatialOrigin"/> frame.</summary>
    public static Quantity<double> Distance(QuantityPoint2<T> a, QuantityPoint2<T> b) => (b - a).Magnitude();

    /// <summary>Affine midpoint <c>a + (b - a) / 2</c>.</summary>
    public static QuantityPoint2<T> Midpoint(QuantityPoint2<T> a, QuantityPoint2<T> b) =>
        a + ((b - a) / (T.One + T.One));

    /// <summary>Affine linear interpolation <c>a + (b - a) * t</c>.</summary>
    public static QuantityPoint2<T> Lerp(QuantityPoint2<T> a, QuantityPoint2<T> b, T t) =>
        a + ((b - a) * t);

    /// <summary>Displaces a point by a compatible 2-D vector.</summary>
    public static QuantityPoint2<T> operator +(QuantityPoint2<T> point, QuantityVector2<T> delta) =>
        new(point.Position + delta, point.Frame);

    /// <summary>Displaces a point by a compatible 2-D vector.</summary>
    public static QuantityPoint2<T> operator -(QuantityPoint2<T> point, QuantityVector2<T> delta) =>
        new(point.Position - delta, point.Frame);

    /// <summary>Displacement vector from <paramref name="b"/> to <paramref name="a"/> (requires identical <see cref="Frame"/>).</summary>
    public static QuantityVector2<T> operator -(QuantityPoint2<T> a, QuantityPoint2<T> b)
    {
        if (a.Frame != b.Frame)
            throw new InvalidOperationException($"Cannot subtract spatial points in different frames: '{a.Frame.Name}' vs '{b.Frame.Name}'.");
        return a.Position - b.Position;
    }

    /// <summary>Formats as <c>(X, Y) unit @ frame</c>.</summary>
    public override string ToString() => $"{Position} @ {Frame.Name}";
}

/// <summary>
/// Affine 3-D spatial point anchored to a <see cref="SpatialOrigin"/> frame.
/// Enforces affine geometry at compile time: <c>point - point → QuantityVector3&lt;T&gt;</c> and <c>point ± vector → QuantityPoint3&lt;T&gt;</c>,
/// while <c>point + point</c> and <c>point * scalar</c> do not compile.
/// </summary>
public readonly record struct QuantityPoint3<T> :
    ISubtractionOperators<QuantityPoint3<T>, QuantityPoint3<T>, QuantityVector3<T>>,
    IAdditionOperators<QuantityPoint3<T>, QuantityVector3<T>, QuantityPoint3<T>>,
    ISubtractionOperators<QuantityPoint3<T>, QuantityVector3<T>, QuantityPoint3<T>>
    where T : INumber<T>
{
    /// <summary>Position displacement vector from <see cref="Frame"/>.</summary>
    public QuantityVector3<T> Position { get; }

    /// <summary>Reference spatial frame.</summary>
    public SpatialOrigin Frame { get; }

    /// <summary>X coordinate in <see cref="Unit"/>.</summary>
    public T X => Position.X;

    /// <summary>Y coordinate in <see cref="Unit"/>.</summary>
    public T Y => Position.Y;

    /// <summary>Z coordinate in <see cref="Unit"/>.</summary>
    public T Z => Position.Z;

    /// <summary>Linear unit of coordinates.</summary>
    public SymbolicUnit Unit => Position.Unit;

    /// <summary>Constructs an affine 3-D spatial point in <paramref name="frame"/>.</summary>
    public QuantityPoint3(QuantityVector3<T> position, SpatialOrigin? frame = null)
    {
        Position = position;
        Frame = frame ?? SpatialOrigins.World;
    }

    /// <summary>Creates a 3-D spatial point at <c>(x, y, z)</c> in <paramref name="unit"/> relative to <paramref name="frame"/>.</summary>
    public static QuantityPoint3<T> At(T x, T y, T z, SymbolicUnit unit, SpatialOrigin? frame = null, QuantitySpec? spec = null) =>
        new(new QuantityVector3<T>(x, y, z, unit, spec), frame ?? SpatialOrigins.World);

    /// <summary>Re-expresses this point in <paramref name="targetUnit"/> while preserving <see cref="Frame"/>.</summary>
    public QuantityPoint3<T> In(SymbolicUnit targetUnit) => new(Position.In(targetUnit), Frame);

    /// <summary>Drops coordinates to a raw <see cref="Vector3"/> explicitly converted into <paramref name="targetUnit"/>.</summary>
    public Vector3 ToVector3In(SymbolicUnit targetUnit) => Position.ToVector3In(targetUnit);

    /// <summary>Deconstructs into coordinates, unit, and frame.</summary>
    public void Deconstruct(out T x, out T y, out T z, out SymbolicUnit unit, out SpatialOrigin frame)
    {
        x = X;
        y = Y;
        z = Z;
        unit = Unit;
        frame = Frame;
    }

    /// <summary>Converts to a serializer-agnostic primitive <see cref="QuantityPoint3Data{T}"/> wire record.</summary>
    public QuantityPoint3Data<T> ToData(bool asciiSymbol = false) =>
        new(X, Y, Z, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Frame.Id, Position.Spec?.Id);

    /// <summary>Reconstructs from a primitive <see cref="QuantityPoint3Data{T}"/> wire record.</summary>
    public static QuantityPoint3<T> FromData(QuantityPoint3Data<T> data, UnitEnvironment? environment = null)
    {
        var vec = QuantityVector3<T>.FromData(new QuantityVector3Data<T>(data.X, data.Y, data.Z, data.Unit, data.Spec), environment);
        return new(vec, SpatialOrigins.ResolveOrCreate(data.Frame));
    }

    /// <summary>Euclidean distance between two points in the same <see cref="SpatialOrigin"/> frame.</summary>
    public static Quantity<double> Distance(QuantityPoint3<T> a, QuantityPoint3<T> b) => (b - a).Magnitude();

    /// <summary>Affine midpoint <c>a + (b - a) / 2</c>.</summary>
    public static QuantityPoint3<T> Midpoint(QuantityPoint3<T> a, QuantityPoint3<T> b) =>
        a + ((b - a) / (T.One + T.One));

    /// <summary>Affine linear interpolation <c>a + (b - a) * t</c>.</summary>
    public static QuantityPoint3<T> Lerp(QuantityPoint3<T> a, QuantityPoint3<T> b, T t) =>
        a + ((b - a) * t);

    /// <summary>Displaces a point by a compatible 3-D vector.</summary>
    public static QuantityPoint3<T> operator +(QuantityPoint3<T> point, QuantityVector3<T> delta) =>
        new(point.Position + delta, point.Frame);

    /// <summary>Displaces a point by a compatible 3-D vector.</summary>
    public static QuantityPoint3<T> operator -(QuantityPoint3<T> point, QuantityVector3<T> delta) =>
        new(point.Position - delta, point.Frame);

    /// <summary>Displacement vector from <paramref name="b"/> to <paramref name="a"/> (requires identical <see cref="Frame"/>).</summary>
    public static QuantityVector3<T> operator -(QuantityPoint3<T> a, QuantityPoint3<T> b)
    {
        if (a.Frame != b.Frame)
            throw new InvalidOperationException($"Cannot subtract spatial points in different frames: '{a.Frame.Name}' vs '{b.Frame.Name}'.");
        return a.Position - b.Position;
    }

    /// <summary>Formats as <c>(X, Y, Z) unit @ frame</c>.</summary>
    public override string ToString() => $"{Position} @ {Frame.Name}";
}
