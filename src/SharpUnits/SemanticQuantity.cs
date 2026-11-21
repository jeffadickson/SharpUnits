using System.Collections.Concurrent;

namespace SharpUnits;

/// <summary>Stable semantic identity for physically distinct quantity kinds.</summary>
public readonly record struct QuantityKind
{
    /// <summary>Stable nonempty identifier suitable for serialization.</summary>
    public string Id { get; }

    /// <summary>Constructs a kind identifier.</summary>
    public QuantityKind(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Kind ID required.", nameof(id));
        Id = id;
    }

    /// <summary>Length.</summary>
    public static QuantityKind Length => new("si.length");

    /// <summary>Mass.</summary>
    public static QuantityKind Mass => new("si.mass");

    /// <summary>Time duration.</summary>
    public static QuantityKind Time => new("si.time");

    /// <summary>Electric current.</summary>
    public static QuantityKind Current => new("si.current");

    /// <summary>Thermodynamic temperature.</summary>
    public static QuantityKind Temperature => new("si.temperature");

    /// <summary>Temperature interval / difference.</summary>
    public static QuantityKind TemperatureDifference => new("si.temperature_difference");

    /// <summary>Plane angle.</summary>
    public static QuantityKind Angle => new("si.angle");

    /// <summary>Solid angle.</summary>
    public static QuantityKind SolidAngle => new("si.solid_angle");

    /// <summary>Frequency (cycles per second).</summary>
    public static QuantityKind Frequency => new("si.frequency");

    /// <summary>Angular frequency (radians per second).</summary>
    public static QuantityKind AngularFrequency => new("si.angular_frequency");

    /// <summary>Velocity.</summary>
    public static QuantityKind Velocity => new("si.velocity");

    /// <summary>Acceleration.</summary>
    public static QuantityKind Acceleration => new("si.acceleration");

    /// <summary>Force.</summary>
    public static QuantityKind Force => new("si.force");

    /// <summary>Pressure or mechanical stress.</summary>
    public static QuantityKind Pressure => new("si.pressure");

    /// <summary>Energy or work.</summary>
    public static QuantityKind Energy => new("si.energy");

    /// <summary>Torque (moment of force — dimensionally identical to energy, semantically distinct).</summary>
    public static QuantityKind Torque => new("si.torque");

    /// <summary>Active / real power.</summary>
    public static QuantityKind Power => new("si.power");

    /// <summary>Apparent electrical power (volt-amperes).</summary>
    public static QuantityKind ApparentPower => new("si.apparent_power");

    /// <summary>Reactive electrical power (vars).</summary>
    public static QuantityKind ReactivePower => new("si.reactive_power");

    /// <summary>Electric charge.</summary>
    public static QuantityKind Charge => new("si.charge");

    /// <summary>Electric potential / voltage.</summary>
    public static QuantityKind Voltage => new("si.voltage");

    /// <summary>Electric capacitance.</summary>
    public static QuantityKind Capacitance => new("si.capacitance");

    /// <summary>Electric resistance.</summary>
    public static QuantityKind Resistance => new("si.resistance");

    /// <summary>Electric conductance.</summary>
    public static QuantityKind Conductance => new("si.conductance");

    /// <summary>Magnetic flux.</summary>
    public static QuantityKind MagneticFlux => new("si.magnetic_flux");

    /// <summary>Magnetic flux density.</summary>
    public static QuantityKind MagneticFluxDensity => new("si.magnetic_flux_density");

    /// <summary>Electric inductance.</summary>
    public static QuantityKind Inductance => new("si.inductance");

    /// <summary>Returns the kind identifier.</summary>
    public override string ToString() => Id;
}

/// <summary>Thread-safe registry mapping semantic quantity kinds to canonical dimensions.</summary>
public sealed class QuantityKindRegistry
{
    private readonly ConcurrentDictionary<QuantityKind, RationalDimension> definitions = new();

    /// <summary>Registers a new kind; returns false if already registered.</summary>
    public bool Register(QuantityKind kind, RationalDimension dimension)
    {
        if (string.IsNullOrWhiteSpace(kind.Id)) throw new ArgumentException("Invalid kind.", nameof(kind));
        return definitions.TryAdd(kind, dimension);
    }

    /// <summary>Gets the registered dimension for <paramref name="kind"/>.</summary>
    public RationalDimension DimensionOf(QuantityKind kind) =>
        definitions.TryGetValue(kind, out var d) ? d : throw new KeyNotFoundException($"Unknown kind: {kind.Id}");

    /// <summary>Attempts to retrieve the registered dimension for <paramref name="kind"/>.</summary>
    public bool TryGetDimension(QuantityKind kind, out RationalDimension dimension) =>
        definitions.TryGetValue(kind, out dimension);

    /// <summary>Shared default standard registry instance.</summary>
    public static QuantityKindRegistry Standard { get; } = CreateStandard();

    /// <summary>Creates a standard registry populated with SI mechanical, thermal, and electrical kinds.</summary>
    public static QuantityKindRegistry CreateStandard()
    {
        var r = new QuantityKindRegistry();
        r.Register(QuantityKind.Length, RationalDimension.LengthDimension);
        r.Register(QuantityKind.Mass, RationalDimension.MassDimension);
        r.Register(QuantityKind.Time, RationalDimension.TimeDimension);
        r.Register(QuantityKind.Current, RationalDimension.ElectricCurrent);
        r.Register(QuantityKind.Temperature, RationalDimension.ThermodynamicTemperature);
        r.Register(QuantityKind.TemperatureDifference, RationalDimension.ThermodynamicTemperature);
        r.Register(QuantityKind.Angle, RationalDimension.None);
        r.Register(QuantityKind.SolidAngle, RationalDimension.None);
        r.Register(QuantityKind.Frequency, RationalDimension.Frequency);
        r.Register(QuantityKind.AngularFrequency, RationalDimension.Frequency);
        r.Register(QuantityKind.Velocity, RationalDimension.Velocity);
        r.Register(QuantityKind.Acceleration, RationalDimension.Acceleration);
        r.Register(QuantityKind.Force, RationalDimension.Force);
        r.Register(QuantityKind.Pressure, RationalDimension.Pressure);
        r.Register(QuantityKind.Energy, RationalDimension.Energy);
        r.Register(QuantityKind.Torque, RationalDimension.Energy);
        r.Register(QuantityKind.Power, RationalDimension.Power);
        r.Register(QuantityKind.ApparentPower, RationalDimension.Power);
        r.Register(QuantityKind.ReactivePower, RationalDimension.Power);
        r.Register(QuantityKind.Charge, RationalDimension.ElectricCharge);
        r.Register(QuantityKind.Voltage, RationalDimension.Voltage);
        r.Register(QuantityKind.Capacitance, RationalDimension.Capacitance);
        r.Register(QuantityKind.Resistance, RationalDimension.Resistance);
        r.Register(QuantityKind.Conductance, RationalDimension.Conductance);
        r.Register(QuantityKind.MagneticFlux, RationalDimension.MagneticFlux);
        r.Register(QuantityKind.MagneticFluxDensity, RationalDimension.MagneticFluxDensity);
        r.Register(QuantityKind.Inductance, RationalDimension.Inductance);
        return r;
    }
}

/// <summary>Quantity carrying both dimensions and explicit semantic physical identity.</summary>
public readonly record struct SemanticQuantity
{
    /// <summary>Underlying physical quantity.</summary>
    public Quantity Value { get; }

    /// <summary>Physical kind.</summary>
    public QuantityKind Kind { get; }

    /// <summary>Creates a semantically checked quantity.</summary>
    public SemanticQuantity(Quantity value, QuantityKind kind, QuantityKindRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (value.RationalDimension != registry.DimensionOf(kind))
            throw new InvalidOperationException("Kind and dimensions disagree.");
        Value = value.WithKind(kind);
        Kind = kind;
    }

    /// <summary>Adds quantities with identical semantic kind.</summary>
    public SemanticQuantity Add(SemanticQuantity other, QuantityKindRegistry registry)
    {
        if (Kind != other.Kind) throw new InvalidOperationException("Different physical kinds.");
        return new(Value + other.Value, Kind, registry);
    }

    /// <summary>Subtracts quantities with identical semantic kind.</summary>
    public SemanticQuantity Subtract(SemanticQuantity other, QuantityKindRegistry registry)
    {
        if (Kind != other.Kind) throw new InvalidOperationException("Different physical kinds.");
        return new(Value - other.Value, Kind, registry);
    }

    /// <summary>Multiplies quantities with an explicitly chosen result kind.</summary>
    public SemanticQuantity MultiplyAs(SemanticQuantity other, QuantityKind resultKind, QuantityKindRegistry registry) =>
        new(Value * other.Value, resultKind, registry);
}
