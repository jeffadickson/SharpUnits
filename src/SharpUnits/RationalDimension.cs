using System.Collections.ObjectModel;
using System.Text;

namespace SharpUnits;

/// <summary>Seven SI base dimensions with exact rational exponents.</summary>
public readonly record struct RationalDimension(
    Rational Length,
    Rational Mass,
    Rational Time,
    Rational Current,
    Rational Temperature,
    Rational Amount,
    Rational Luminosity)
{
    /// <summary>Dimensionless (all exponents zero).</summary>
    public static RationalDimension None => default;

    /// <summary>Length (L).</summary>
    public static RationalDimension LengthDimension => From(Dimension.LengthDimension);

    /// <summary>Mass (M).</summary>
    public static RationalDimension MassDimension => From(Dimension.MassDimension);

    /// <summary>Time (T).</summary>
    public static RationalDimension TimeDimension => From(Dimension.TimeDimension);

    /// <summary>Electric current (I).</summary>
    public static RationalDimension ElectricCurrent => From(Dimension.ElectricCurrent);

    /// <summary>Thermodynamic temperature (Θ).</summary>
    public static RationalDimension ThermodynamicTemperature => From(Dimension.ThermodynamicTemperature);

    /// <summary>Amount of substance (N).</summary>
    public static RationalDimension AmountOfSubstance => From(Dimension.AmountOfSubstance);

    /// <summary>Luminous intensity (J).</summary>
    public static RationalDimension LuminousIntensity => From(Dimension.LuminousIntensity);

    /// <summary>Area (L²).</summary>
    public static RationalDimension Area => From(Dimension.Area);

    /// <summary>Volume (L³).</summary>
    public static RationalDimension Volume => From(Dimension.Volume);

    /// <summary>Velocity (L·T⁻¹).</summary>
    public static RationalDimension Velocity => From(Dimension.Velocity);

    /// <summary>Acceleration (L·T⁻²).</summary>
    public static RationalDimension Acceleration => From(Dimension.Acceleration);

    /// <summary>Frequency (T⁻¹).</summary>
    public static RationalDimension Frequency => From(Dimension.Frequency);

    /// <summary>Force (L·M·T⁻²).</summary>
    public static RationalDimension Force => From(Dimension.Force);

    /// <summary>Pressure (L⁻¹·M·T⁻²).</summary>
    public static RationalDimension Pressure => From(Dimension.Pressure);

    /// <summary>Energy (L²·M·T⁻²).</summary>
    public static RationalDimension Energy => From(Dimension.Energy);

    /// <summary>Power (L²·M·T⁻³).</summary>
    public static RationalDimension Power => From(Dimension.Power);

    /// <summary>Electric charge (T·I).</summary>
    public static RationalDimension ElectricCharge => From(Dimension.ElectricCharge);

    /// <summary>Voltage (L²·M·T⁻³·I⁻¹).</summary>
    public static RationalDimension Voltage => From(Dimension.Voltage);

    /// <summary>Capacitance (L⁻²·M⁻¹·T⁴·I²).</summary>
    public static RationalDimension Capacitance => From(Dimension.Capacitance);

    /// <summary>Resistance (L²·M·T⁻³·I⁻²).</summary>
    public static RationalDimension Resistance => From(Dimension.Resistance);

    /// <summary>Conductance (L⁻²·M⁻¹·T³·I²).</summary>
    public static RationalDimension Conductance => From(Dimension.Conductance);

    /// <summary>Magnetic flux (L²·M·T⁻²·I⁻¹).</summary>
    public static RationalDimension MagneticFlux => From(Dimension.MagneticFlux);

    /// <summary>Magnetic flux density (M·T⁻²·I⁻¹).</summary>
    public static RationalDimension MagneticFluxDensity => From(Dimension.MagneticFluxDensity);

    /// <summary>Inductance (L²·M·T⁻²·I⁻²).</summary>
    public static RationalDimension Inductance => From(Dimension.Inductance);

    /// <summary>True when all exponents are zero.</summary>
    public bool IsDimensionless =>
        Length.IsZero && Mass.IsZero && Time.IsZero && Current.IsZero &&
        Temperature.IsZero && Amount.IsZero && Luminosity.IsZero;

    /// <summary>Converts an integer dimension without precision loss.</summary>
    public static RationalDimension From(Dimension d) =>
        new(d.Length, d.Mass, d.Time, d.Current, d.Temperature, d.Amount, d.Luminosity);

    /// <summary>Converts exactly to the integer dimension model, or rejects fractional/overflow exponents.</summary>
    public Dimension ToIntegerDimension()
    {
        static int Exact(Rational value)
        {
            if (!value.IsInteger) throw new InvalidOperationException("Dimension has fractional exponents.");
            return checked((int)value.Numerator);
        }
        return new(
            Exact(Length),
            Exact(Mass),
            Exact(Time),
            Exact(Current),
            Exact(Temperature),
            Exact(Amount),
            Exact(Luminosity));
    }

    /// <summary>Multiplies dimensions by adding exponents.</summary>
    public static RationalDimension operator +(RationalDimension a, RationalDimension b) => new(
        a.Length + b.Length,
        a.Mass + b.Mass,
        a.Time + b.Time,
        a.Current + b.Current,
        a.Temperature + b.Temperature,
        a.Amount + b.Amount,
        a.Luminosity + b.Luminosity);

    /// <summary>Divides dimensions by subtracting exponents.</summary>
    public static RationalDimension operator -(RationalDimension a, RationalDimension b) => new(
        a.Length - b.Length,
        a.Mass - b.Mass,
        a.Time - b.Time,
        a.Current - b.Current,
        a.Temperature - b.Temperature,
        a.Amount - b.Amount,
        a.Luminosity - b.Luminosity);

    /// <summary>Raises all exponents to a rational power.</summary>
    public RationalDimension Pow(Rational power) => new(
        Length * power,
        Mass * power,
        Time * power,
        Current * power,
        Temperature * power,
        Amount * power,
        Luminosity * power);

    /// <summary>Raises all exponents to an integer power.</summary>
    public RationalDimension Pow(int power) => Pow(new Rational(power, 1));

    /// <summary>Returns the exact positive integer-degree root.</summary>
    public RationalDimension Root(int degree)
    {
        if (degree <= 0) throw new ArgumentOutOfRangeException(nameof(degree));
        return Pow(new Rational(1, degree));
    }

    /// <summary>Returns nonzero SI base dimensions as an immutable snapshot.</summary>
    public IReadOnlyDictionary<string, Rational> GetBaseDimensions()
    {
        var values = new Dictionary<string, Rational>();
        if (!Length.IsZero) values["L"] = Length;
        if (!Mass.IsZero) values["M"] = Mass;
        if (!Time.IsZero) values["T"] = Time;
        if (!Current.IsZero) values["I"] = Current;
        if (!Temperature.IsZero) values["Θ"] = Temperature;
        if (!Amount.IsZero) values["N"] = Amount;
        if (!Luminosity.IsZero) values["J"] = Luminosity;
        return new ReadOnlyDictionary<string, Rational>(values);
    }

    /// <summary>Formats coherent SI base unit notation (e.g. <c>m^2·kg·s^-3·A^-1</c> or <c>1</c>).</summary>
    public string ToCoherentSiSymbol()
    {
        if (IsDimensionless) return "1";
        var sb = new StringBuilder();
        Append(sb, "m", Length);
        Append(sb, "kg", Mass);
        Append(sb, "s", Time);
        Append(sb, "A", Current);
        Append(sb, "K", Temperature);
        Append(sb, "mol", Amount);
        Append(sb, "cd", Luminosity);
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string symbol, Rational exp)
    {
        if (exp.IsZero) return;
        if (sb.Length > 0) sb.Append('·');
        sb.Append(symbol);
        if (!exp.IsOne)
        {
            sb.Append('^');
            if (exp.IsInteger) sb.Append(exp.Numerator);
            else sb.Append('(').Append(exp).Append(')');
        }
    }
}