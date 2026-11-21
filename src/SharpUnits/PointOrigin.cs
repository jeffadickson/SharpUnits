namespace SharpUnits;

/// <summary>Reference origin in a 1D physical affine space (for example absolute zero, ice point, vacuum, or standard atmosphere).</summary>
/// <remarks>
/// Pairs with <see cref="QuantityPoint{T}"/> to separate affine points from displacements, matching the affine-space model of C++ <c>mp-units</c>.
/// </remarks>
public sealed record PointOrigin
{
    /// <summary>Stable unique origin identifier.</summary>
    public string Id { get; }

    /// <summary>Human-readable origin name.</summary>
    public string Name { get; }

    /// <summary>Dimension of the affine space.</summary>
    public RationalDimension Dimension { get; }

    /// <summary>Optional ISQ quantity specification associated with this origin.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Offset of this origin relative to the dimension's natural zero origin, expressed in coherent SI units.</summary>
    public double OffsetFromBaseSi { get; }

    /// <summary>Constructs a validated affine point origin.</summary>
    public PointOrigin(
        string id,
        string name,
        RationalDimension dimension,
        double offsetFromBaseSi = 0d,
        QuantitySpec? spec = null)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Origin ID required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Origin name required.", nameof(name));
        if (!double.IsFinite(offsetFromBaseSi)) throw new ArgumentOutOfRangeException(nameof(offsetFromBaseSi));
        if (spec is not null && spec.Dimension != dimension)
            throw new InvalidOperationException("Origin specification dimension disagrees with origin dimension.");
        Id = id;
        Name = name;
        Dimension = dimension;
        OffsetFromBaseSi = offsetFromBaseSi;
        Spec = spec;
    }

    /// <summary>Defines a new origin displaced by <paramref name="offsetSi"/> coherent SI units from <paramref name="parent"/>.</summary>
    public static PointOrigin RelativeTo(PointOrigin parent, double offsetSi, string id, string name)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (!double.IsFinite(offsetSi)) throw new ArgumentOutOfRangeException(nameof(offsetSi));
        return new(id, name, parent.Dimension, parent.OffsetFromBaseSi + offsetSi, parent.Spec);
    }

    /// <summary>Returns the origin name.</summary>
    public override string ToString() => Name;
}

/// <summary>Standard physical affine point origins for temperature, pressure, altitude, and time.</summary>
public static class PointOrigins
{
    /// <summary>Absolute zero (<c>0 K</c>).</summary>
    public static PointOrigin AbsoluteZero { get; } = new("origin.absolute_zero", "absolute zero", RationalDimension.ThermodynamicTemperature, 0d, Isq.ThermodynamicTemperature);

    /// <summary>Celsius ice point (<c>273.15 K = 0 °C</c>).</summary>
    public static PointOrigin IcePoint { get; } = PointOrigin.RelativeTo(AbsoluteZero, 273.15d, "origin.ice_point", "ice point (0 °C)");

    /// <summary>Fahrenheit zero (<c>0 °F = 459.67 °R</c>).</summary>
    public static PointOrigin FahrenheitZero { get; } = PointOrigin.RelativeTo(AbsoluteZero, 459.67d * 5d / 9d, "origin.fahrenheit_zero", "Fahrenheit zero (0 °F)");

    /// <summary>Perfect vacuum (<c>0 Pa</c>, origin for absolute pressure).</summary>
    public static PointOrigin Vacuum { get; } = new("origin.vacuum", "vacuum (0 Pa)", RationalDimension.Pressure, 0d, Isq.Pressure);

    /// <summary>Standard atmospheric pressure (<c>101325 Pa</c>, origin for gauge pressure).</summary>
    public static PointOrigin StandardAtmosphere { get; } = PointOrigin.RelativeTo(Vacuum, 101325d, "origin.standard_atmosphere", "standard atmosphere (0 Pa gauge)");

    /// <summary>Mean sea level (<c>0 m</c>, origin for altitude).</summary>
    public static PointOrigin SeaLevel { get; } = new("origin.sea_level", "mean sea level", RationalDimension.LengthDimension, 0d, Isq.Altitude);

    /// <summary>Creates a natural zero origin for any dimension and optional ISQ specification.</summary>
    public static PointOrigin NaturalZero(RationalDimension dimension, QuantitySpec? spec = null, string id = "origin.zero", string name = "zero") =>
        new(id, name, dimension, 0d, spec);
}
