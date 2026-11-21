using System.Collections.Concurrent;

namespace SharpUnits;

/// <summary>ISO 80000 ISQ quantity specification forming a specialization hierarchy over physical dimensions.</summary>
/// <remarks>
/// Inspired by C++ <c>mp-units</c>: sub-quantities within the same kind root (for example <see cref="Isq.Width"/> and
/// <see cref="Isq.Height"/> under <see cref="Isq.Length"/>) combine smoothly to their common ancestor, while distinct
/// physical kinds sharing a dimension (for example <see cref="Isq.Energy"/> vs. <see cref="Isq.Torque"/>, or
/// <see cref="Isq.Frequency"/> vs. <see cref="Isq.AngularFrequency"/>) cannot be mixed implicitly.
/// </remarks>
public sealed record QuantitySpec
{
    /// <summary>Stable unique specification identifier (for example <c>isq.length</c>, <c>isq.torque</c>).</summary>
    public string Id { get; }

    /// <summary>Human-readable ISO 80000 quantity name.</summary>
    public string Name { get; }

    /// <summary>Canonical exact rational dimension of this quantity specification.</summary>
    public RationalDimension Dimension { get; }

    /// <summary>Optional parent specification in the ISQ hierarchy.</summary>
    public QuantitySpec? Parent { get; }

    /// <summary>True when this specification establishes a distinct physical kind boundary.</summary>
    public bool IsKind { get; }

    /// <summary>Constructs a validated quantity specification.</summary>
    public QuantitySpec(
        string id,
        string name,
        RationalDimension dimension,
        QuantitySpec? parent = null,
        bool isKind = false)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Specification ID required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Specification name required.", nameof(name));
        if (parent is not null && parent.Dimension != dimension)
            throw new InvalidOperationException($"Parent specification '{parent.Id}' dimension disagrees with '{id}'.");
        Id = id;
        Name = name;
        Dimension = dimension;
        Parent = parent;
        IsKind = isKind || parent is null;
    }

    /// <summary>Returns the nearest enclosing kind-root specification (or this specification when it is a kind root).</summary>
    public QuantitySpec KindRoot
    {
        get
        {
            var current = this;
            while (!current.IsKind && current.Parent is not null)
                current = current.Parent;
            return current;
        }
    }

    /// <summary>Returns true when this specification equals or specializes <paramref name="ancestor"/>.</summary>
    public bool IsSubSpecOf(QuantitySpec ancestor)
    {
        ArgumentNullException.ThrowIfNull(ancestor);
        var current = this;
        while (current is not null)
        {
            if (current == ancestor) return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>Returns true when this specification can be implicitly widened to <paramref name="target"/>.</summary>
    public bool IsImplicitlyConvertibleTo(QuantitySpec target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return KindRoot == target.KindRoot && IsSubSpecOf(target);
    }

    /// <summary>Returns true when this specification can be explicitly cast to <paramref name="target"/> (same kind tree).</summary>
    public bool IsExplicitlyConvertibleTo(QuantitySpec target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Dimension == target.Dimension && KindRoot == target.KindRoot;
    }

    /// <summary>Projects this specification to a <see cref="QuantityKind"/> identifier.</summary>
    public QuantityKind ToKind() => new(Id);

    /// <summary>
    /// Computes the lowest common specification for additive/comparison operations, or returns <c>null</c> if
    /// <paramref name="a"/> and <paramref name="b"/> belong to incompatible physical kinds.
    /// </summary>
    public static QuantitySpec? CommonSpec(QuantitySpec? a, QuantitySpec? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        if (a == b) return a;
        if (a.Dimension != b.Dimension) return null;
        if (a.KindRoot != b.KindRoot) return null;

        // Walk up from 'a' until we hit an ancestor of 'b' within the same kind root.
        var current = a;
        while (current is not null)
        {
            if (b.IsSubSpecOf(current)) return current;
            if (current.IsKind) break;
            current = current.Parent;
        }
        return null;
    }

    /// <summary>Returns the specification identifier.</summary>
    public override string ToString() => Id;
}

/// <summary>ISO 80000 International System of Quantities (ISQ) specification catalog.</summary>
public static class Isq
{
    private static readonly ConcurrentDictionary<string, QuantitySpec> ById = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<QuantityKind, QuantitySpec> ByLegacyKind = new();

    // --- Dimensionless & Angles (ISO 80000-3) ---

    /// <summary>Generic dimensionless quantity.</summary>
    public static QuantitySpec Dimensionless { get; } = Define("isq.dimensionless", "dimensionless", RationalDimension.None, isKind: true);

    /// <summary>Dimensionless ratio / factor.</summary>
    public static QuantitySpec Ratio { get; } = Define("isq.ratio", "ratio", RationalDimension.None, Dimensionless);

    /// <summary>Signal gain or attenuation ratio.</summary>
    public static QuantitySpec Gain { get; } = Define("isq.gain", "gain", RationalDimension.None, Ratio);

    /// <summary>Efficiency (output/input power or energy ratio).</summary>
    public static QuantitySpec Efficiency { get; } = Define("isq.efficiency", "efficiency", RationalDimension.None, Ratio);

    /// <summary>AC power factor.</summary>
    public static QuantitySpec PowerFactor { get; } = Define("isq.power_factor", "power factor", RationalDimension.None, Ratio);

    /// <summary>Plane angular measure (distinct kind from plain dimensionless numbers).</summary>
    public static QuantitySpec AngularMeasure { get; } = Define("isq.angle", "plane angle", RationalDimension.None, Dimensionless, isKind: true, legacyKind: QuantityKind.Angle);

    /// <summary>Phase angle in sinusoidal/AC analysis.</summary>
    public static QuantitySpec PhaseAngle { get; } = Define("isq.phase_angle", "phase angle", RationalDimension.None, AngularMeasure);

    /// <summary>Solid angular measure (distinct kind from plane angle and dimensionless numbers).</summary>
    public static QuantitySpec SolidAngularMeasure { get; } = Define("isq.solid_angle", "solid angle", RationalDimension.None, Dimensionless, isKind: true, legacyKind: QuantityKind.SolidAngle);

    // --- Space & Time (ISO 80000-3) ---

    /// <summary>Length (base kind).</summary>
    public static QuantitySpec Length { get; } = Define("isq.length", "length", RationalDimension.LengthDimension, isKind: true, legacyKind: QuantityKind.Length);

    /// <summary>Width (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Width { get; } = Define("isq.width", "width", RationalDimension.LengthDimension, Length);

    /// <summary>Height (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Height { get; } = Define("isq.height", "height", RationalDimension.LengthDimension, Length);

    /// <summary>Depth (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Depth { get; } = Define("isq.depth", "depth", RationalDimension.LengthDimension, Length);

    /// <summary>Distance (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Distance { get; } = Define("isq.distance", "distance", RationalDimension.LengthDimension, Length);

    /// <summary>Radius (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Radius { get; } = Define("isq.radius", "radius", RationalDimension.LengthDimension, Length);

    /// <summary>Diameter (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Diameter { get; } = Define("isq.diameter", "diameter", RationalDimension.LengthDimension, Length);

    /// <summary>Wavelength (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Wavelength { get; } = Define("isq.wavelength", "wavelength", RationalDimension.LengthDimension, Length);

    /// <summary>Altitude (specialization of <see cref="Length"/>).</summary>
    public static QuantitySpec Altitude { get; } = Define("isq.altitude", "altitude", RationalDimension.LengthDimension, Length);

    /// <summary>Area.</summary>
    public static QuantitySpec Area { get; } = Define("isq.area", "area", RationalDimension.Area, isKind: true);

    /// <summary>Volume.</summary>
    public static QuantitySpec Volume { get; } = Define("isq.volume", "volume", RationalDimension.Volume, isKind: true);

    /// <summary>Time / duration.</summary>
    public static QuantitySpec Time { get; } = Define("isq.time", "time", RationalDimension.TimeDimension, isKind: true, legacyKind: QuantityKind.Time);

    /// <summary>Periodic wave period.</summary>
    public static QuantitySpec Period { get; } = Define("isq.period", "period", RationalDimension.TimeDimension, Time);

    /// <summary>Velocity.</summary>
    public static QuantitySpec Velocity { get; } = Define("isq.velocity", "velocity", RationalDimension.Velocity, isKind: true, legacyKind: QuantityKind.Velocity);

    /// <summary>Speed (scalar magnitude of velocity).</summary>
    public static QuantitySpec Speed { get; } = Define("isq.speed", "speed", RationalDimension.Velocity, Velocity);

    /// <summary>Acceleration.</summary>
    public static QuantitySpec Acceleration { get; } = Define("isq.acceleration", "acceleration", RationalDimension.Acceleration, isKind: true, legacyKind: QuantityKind.Acceleration);

    /// <summary>Frequency in cycles per second (distinct kind from <see cref="AngularFrequency"/>).</summary>
    public static QuantitySpec Frequency { get; } = Define("isq.frequency", "frequency", RationalDimension.Frequency, isKind: true, legacyKind: QuantityKind.Frequency);

    /// <summary>Angular frequency in radians per second (distinct kind from <see cref="Frequency"/>).</summary>
    public static QuantitySpec AngularFrequency { get; } = Define("isq.angular_frequency", "angular frequency", RationalDimension.Frequency, isKind: true, legacyKind: QuantityKind.AngularFrequency);

    /// <summary>Radioactive activity in becquerels (distinct kind from <see cref="Frequency"/>).</summary>
    public static QuantitySpec Activity { get; } = Define("isq.activity", "activity", RationalDimension.Frequency, isKind: true);

    // --- Mechanics (ISO 80000-4) ---

    /// <summary>Mass.</summary>
    public static QuantitySpec Mass { get; } = Define("isq.mass", "mass", RationalDimension.MassDimension, isKind: true, legacyKind: QuantityKind.Mass);

    /// <summary>Force.</summary>
    public static QuantitySpec Force { get; } = Define("isq.force", "force", RationalDimension.Force, isKind: true, legacyKind: QuantityKind.Force);

    /// <summary>Weight (specialization of <see cref="Force"/>).</summary>
    public static QuantitySpec Weight { get; } = Define("isq.weight", "weight", RationalDimension.Force, Force);

    /// <summary>Pressure.</summary>
    public static QuantitySpec Pressure { get; } = Define("isq.pressure", "pressure", RationalDimension.Pressure, isKind: true, legacyKind: QuantityKind.Pressure);

    /// <summary>Mechanical stress (specialization of <see cref="Pressure"/>).</summary>
    public static QuantitySpec Stress { get; } = Define("isq.stress", "stress", RationalDimension.Pressure, Pressure);

    /// <summary>Energy (distinct kind from <see cref="Torque"/>).</summary>
    public static QuantitySpec Energy { get; } = Define("isq.energy", "energy", RationalDimension.Energy, isKind: true, legacyKind: QuantityKind.Energy);

    /// <summary>Mechanical work (specialization of <see cref="Energy"/>).</summary>
    public static QuantitySpec Work { get; } = Define("isq.work", "work", RationalDimension.Energy, Energy);

    /// <summary>Thermal heat (specialization of <see cref="Energy"/>).</summary>
    public static QuantitySpec Heat { get; } = Define("isq.heat", "heat", RationalDimension.Energy, Energy);

    /// <summary>Torque / moment of force (distinct kind from <see cref="Energy"/> despite identical SI dimensions).</summary>
    public static QuantitySpec Torque { get; } = Define("isq.torque", "torque", RationalDimension.Energy, isKind: true, legacyKind: QuantityKind.Torque);

    /// <summary>Active / real power.</summary>
    public static QuantitySpec Power { get; } = Define("isq.power", "power", RationalDimension.Power, isKind: true, legacyKind: QuantityKind.Power);

    /// <summary>Apparent AC power in volt-amperes (distinct kind from active power).</summary>
    public static QuantitySpec ApparentPower { get; } = Define("isq.apparent_power", "apparent power", RationalDimension.Power, isKind: true, legacyKind: QuantityKind.ApparentPower);

    /// <summary>Reactive AC power in vars (distinct kind from active power).</summary>
    public static QuantitySpec ReactivePower { get; } = Define("isq.reactive_power", "reactive power", RationalDimension.Power, isKind: true, legacyKind: QuantityKind.ReactivePower);

    // --- Thermodynamics (ISO 80000-5) ---

    /// <summary>Thermodynamic temperature.</summary>
    public static QuantitySpec ThermodynamicTemperature { get; } = Define("isq.temperature", "thermodynamic temperature", RationalDimension.ThermodynamicTemperature, isKind: true, legacyKind: QuantityKind.Temperature);

    /// <summary>Temperature interval / difference.</summary>
    public static QuantitySpec TemperatureInterval { get; } = Define("isq.temperature_interval", "temperature interval", RationalDimension.ThermodynamicTemperature, ThermodynamicTemperature, legacyKind: QuantityKind.TemperatureDifference);

    // --- Electromagnetism (IEC 80000-6) ---

    /// <summary>Electric current.</summary>
    public static QuantitySpec ElectricCurrent { get; } = Define("isq.current", "electric current", RationalDimension.ElectricCurrent, isKind: true, legacyKind: QuantityKind.Current);

    /// <summary>Electric charge.</summary>
    public static QuantitySpec ElectricCharge { get; } = Define("isq.charge", "electric charge", RationalDimension.ElectricCharge, isKind: true, legacyKind: QuantityKind.Charge);

    /// <summary>Electric potential / voltage.</summary>
    public static QuantitySpec Voltage { get; } = Define("isq.voltage", "voltage", RationalDimension.Voltage, isKind: true, legacyKind: QuantityKind.Voltage);

    /// <summary>Electromotive force (specialization of <see cref="Voltage"/>).</summary>
    public static QuantitySpec ElectromotiveForce { get; } = Define("isq.emf", "electromotive force", RationalDimension.Voltage, Voltage);

    /// <summary>Electric resistance.</summary>
    public static QuantitySpec Resistance { get; } = Define("isq.resistance", "resistance", RationalDimension.Resistance, isKind: true, legacyKind: QuantityKind.Resistance);

    /// <summary>AC impedance magnitude (specialization of <see cref="Resistance"/>).</summary>
    public static QuantitySpec Impedance { get; } = Define("isq.impedance", "impedance", RationalDimension.Resistance, Resistance);

    /// <summary>AC reactance (specialization of <see cref="Resistance"/>).</summary>
    public static QuantitySpec Reactance { get; } = Define("isq.reactance", "reactance", RationalDimension.Resistance, Resistance);

    /// <summary>Electric conductance.</summary>
    public static QuantitySpec Conductance { get; } = Define("isq.conductance", "conductance", RationalDimension.Conductance, isKind: true, legacyKind: QuantityKind.Conductance);

    /// <summary>Electric capacitance.</summary>
    public static QuantitySpec Capacitance { get; } = Define("isq.capacitance", "capacitance", RationalDimension.Capacitance, isKind: true, legacyKind: QuantityKind.Capacitance);

    /// <summary>Electric inductance.</summary>
    public static QuantitySpec Inductance { get; } = Define("isq.inductance", "inductance", RationalDimension.Inductance, isKind: true, legacyKind: QuantityKind.Inductance);

    /// <summary>Magnetic flux.</summary>
    public static QuantitySpec MagneticFlux { get; } = Define("isq.magnetic_flux", "magnetic flux", RationalDimension.MagneticFlux, isKind: true, legacyKind: QuantityKind.MagneticFlux);

    /// <summary>Magnetic flux density.</summary>
    public static QuantitySpec MagneticFluxDensity { get; } = Define("isq.magnetic_flux_density", "magnetic flux density", RationalDimension.MagneticFluxDensity, isKind: true, legacyKind: QuantityKind.MagneticFluxDensity);

    /// <summary>Amount of substance.</summary>
    public static QuantitySpec AmountOfSubstance { get; } = Define("isq.amount_of_substance", "amount of substance", RationalDimension.AmountOfSubstance, isKind: true);

    /// <summary>Luminous intensity.</summary>
    public static QuantitySpec LuminousIntensity { get; } = Define("isq.luminous_intensity", "luminous intensity", RationalDimension.LuminousIntensity, isKind: true);

    /// <summary>Attempts to resolve a registered ISQ specification by ID.</summary>
    public static bool TryGetById(string id, out QuantitySpec? spec) => ById.TryGetValue(id, out spec);

    /// <summary>Attempts to resolve the ISQ specification corresponding to a <see cref="QuantityKind"/>.</summary>
    public static bool TryFromKind(QuantityKind kind, out QuantitySpec? spec)
    {
        if (ByLegacyKind.TryGetValue(kind, out spec)) return true;
        return ById.TryGetValue(kind.Id, out spec);
    }

    private static QuantitySpec Define(
        string id,
        string name,
        RationalDimension dimension,
        QuantitySpec? parent = null,
        bool isKind = false,
        QuantityKind? legacyKind = null)
    {
        var spec = new QuantitySpec(id, name, dimension, parent, isKind);
        ById[id] = spec;
        if (legacyKind is not null) ByLegacyKind[legacyKind.Value] = spec;
        return spec;
    }
}
