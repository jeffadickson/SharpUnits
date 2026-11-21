using System.Collections.ObjectModel;

namespace SharpUnits;

/// <summary>Integer exponents of the seven SI base dimensions (L, M, T, I, Θ, N, J).</summary>
public readonly record struct Dimension(
    int Length,
    int Mass,
    int Time,
    int Current,
    int Temperature,
    int Amount,
    int Luminosity)
{
    /// <summary>Dimensionless (all exponents zero).</summary>
    public static Dimension None => default;

    /// <summary>Length (L).</summary>
    public static Dimension LengthDimension => new(1, 0, 0, 0, 0, 0, 0);

    /// <summary>Mass (M).</summary>
    public static Dimension MassDimension => new(0, 1, 0, 0, 0, 0, 0);

    /// <summary>Time (T).</summary>
    public static Dimension TimeDimension => new(0, 0, 1, 0, 0, 0, 0);

    /// <summary>Electric current (I).</summary>
    public static Dimension ElectricCurrent => new(0, 0, 0, 1, 0, 0, 0);

    /// <summary>Thermodynamic temperature (Θ).</summary>
    public static Dimension ThermodynamicTemperature => new(0, 0, 0, 0, 1, 0, 0);

    /// <summary>Amount of substance (N).</summary>
    public static Dimension AmountOfSubstance => new(0, 0, 0, 0, 0, 1, 0);

    /// <summary>Luminous intensity (J).</summary>
    public static Dimension LuminousIntensity => new(0, 0, 0, 0, 0, 0, 1);

    /// <summary>Area (L²).</summary>
    public static Dimension Area => new(2, 0, 0, 0, 0, 0, 0);

    /// <summary>Volume (L³).</summary>
    public static Dimension Volume => new(3, 0, 0, 0, 0, 0, 0);

    /// <summary>Velocity (L·T⁻¹).</summary>
    public static Dimension Velocity => new(1, 0, -1, 0, 0, 0, 0);

    /// <summary>Acceleration (L·T⁻²).</summary>
    public static Dimension Acceleration => new(1, 0, -2, 0, 0, 0, 0);

    /// <summary>Frequency (T⁻¹).</summary>
    public static Dimension Frequency => new(0, 0, -1, 0, 0, 0, 0);

    /// <summary>Force (L·M·T⁻²).</summary>
    public static Dimension Force => new(1, 1, -2, 0, 0, 0, 0);

    /// <summary>Pressure or stress (L⁻¹·M·T⁻²).</summary>
    public static Dimension Pressure => new(-1, 1, -2, 0, 0, 0, 0);

    /// <summary>Energy or work (L²·M·T⁻²).</summary>
    public static Dimension Energy => new(2, 1, -2, 0, 0, 0, 0);

    /// <summary>Power (L²·M·T⁻³).</summary>
    public static Dimension Power => new(2, 1, -3, 0, 0, 0, 0);

    /// <summary>Electric charge (T·I).</summary>
    public static Dimension ElectricCharge => new(0, 0, 1, 1, 0, 0, 0);

    /// <summary>Electric potential / voltage (L²·M·T⁻³·I⁻¹).</summary>
    public static Dimension Voltage => new(2, 1, -3, -1, 0, 0, 0);

    /// <summary>Electric capacitance (L⁻²·M⁻¹·T⁴·I²).</summary>
    public static Dimension Capacitance => new(-2, -1, 4, 2, 0, 0, 0);

    /// <summary>Electric resistance (L²·M·T⁻³·I⁻²).</summary>
    public static Dimension Resistance => new(2, 1, -3, -2, 0, 0, 0);

    /// <summary>Electric conductance (L⁻²·M⁻¹·T³·I²).</summary>
    public static Dimension Conductance => new(-2, -1, 3, 2, 0, 0, 0);

    /// <summary>Magnetic flux (L²·M·T⁻²·I⁻¹).</summary>
    public static Dimension MagneticFlux => new(2, 1, -2, -1, 0, 0, 0);

    /// <summary>Magnetic flux density (M·T⁻²·I⁻¹).</summary>
    public static Dimension MagneticFluxDensity => new(0, 1, -2, -1, 0, 0, 0);

    /// <summary>Electric inductance (L²·M·T⁻²·I⁻²).</summary>
    public static Dimension Inductance => new(2, 1, -2, -2, 0, 0, 0);

    /// <summary>Illuminance (L⁻²·J).</summary>
    public static Dimension Illuminance => new(-2, 0, 0, 0, 0, 0, 1);

    /// <summary>Absorbed radiation dose (L²·T⁻²).</summary>
    public static Dimension RadiationDose => new(2, 0, -2, 0, 0, 0, 0);

    /// <summary>Catalytic activity (T⁻¹·N).</summary>
    public static Dimension CatalyticActivity => new(0, 0, -1, 0, 0, 1, 0);

    /// <summary>Raises each base-dimension exponent to an integer power.</summary>
    public Dimension Pow(int power) => new(
        checked(Length * power),
        checked(Mass * power),
        checked(Time * power),
        checked(Current * power),
        checked(Temperature * power),
        checked(Amount * power),
        checked(Luminosity * power));

    /// <summary>Computes an exact integer-exponent root; rejects nonintegral results.</summary>
    public Dimension Root(int root)
    {
        if (root <= 0) throw new ArgumentOutOfRangeException(nameof(root));
        if (Length % root != 0 || Mass % root != 0 || Time % root != 0 || Current % root != 0 ||
            Temperature % root != 0 || Amount % root != 0 || Luminosity % root != 0)
            throw new InvalidOperationException("Root requires fractional dimensional exponents.");
        return new(Length / root, Mass / root, Time / root, Current / root, Temperature / root, Amount / root, Luminosity / root);
    }

    /// <summary>Returns nonzero base dimensions as an immutable snapshot.</summary>
    public IReadOnlyDictionary<string, int> GetBaseDimensions()
    {
        var values = new Dictionary<string, int>();
        if (Length != 0) values["L"] = Length;
        if (Mass != 0) values["M"] = Mass;
        if (Time != 0) values["T"] = Time;
        if (Current != 0) values["I"] = Current;
        if (Temperature != 0) values["Θ"] = Temperature;
        if (Amount != 0) values["N"] = Amount;
        if (Luminosity != 0) values["J"] = Luminosity;
        return new ReadOnlyDictionary<string, int>(values);
    }

    /// <summary>Multiplies dimensions by adding base exponents.</summary>
    public static Dimension operator +(Dimension a, Dimension b) => new(
        checked(a.Length + b.Length),
        checked(a.Mass + b.Mass),
        checked(a.Time + b.Time),
        checked(a.Current + b.Current),
        checked(a.Temperature + b.Temperature),
        checked(a.Amount + b.Amount),
        checked(a.Luminosity + b.Luminosity));

    /// <summary>Divides dimensions by subtracting base exponents.</summary>
    public static Dimension operator -(Dimension a, Dimension b) => new(
        checked(a.Length - b.Length),
        checked(a.Mass - b.Mass),
        checked(a.Time - b.Time),
        checked(a.Current - b.Current),
        checked(a.Temperature - b.Temperature),
        checked(a.Amount - b.Amount),
        checked(a.Luminosity - b.Luminosity));

    /// <summary>Implicitly widens an integer dimension to a canonical rational dimension.</summary>
    public static implicit operator RationalDimension(Dimension d) => RationalDimension.From(d);
}