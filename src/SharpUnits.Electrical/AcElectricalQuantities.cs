using System.Numerics;

namespace SharpUnits.Electrical;

/// <summary>Finite angular frequency in radians per second.</summary>
public readonly record struct AngularFrequency
{
    /// <summary>Radians per second.</summary>
    public double RadiansPerSecond { get; }

    /// <summary>Corresponding ordinary frequency in hertz.</summary>
    public Frequency Frequency => new(RadiansPerSecond / (2d * Math.PI));

    /// <summary>Constructs a validated angular frequency.</summary>
    public AngularFrequency(double value)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        RadiansPerSecond = value;
    }

    /// <summary>Converts hertz to angular frequency.</summary>
    public static AngularFrequency FromHertz(double hertz) => new(CheckFinite(2d * Math.PI * hertz));

    /// <summary>Converts a typed <see cref="SharpUnits.Frequency"/> to angular frequency.</summary>
    public static AngularFrequency FromFrequency(Frequency frequency) => FromHertz(frequency.Hertz);

    private static double CheckFinite(double value) =>
        double.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
}

/// <summary>Complex impedance in ohms.</summary>
public readonly record struct Impedance
{
    /// <summary>Complex ohms.</summary>
    public Complex Ohms { get; }

    /// <summary>Constructs a finite impedance.</summary>
    public Impedance(Complex ohms)
    {
        if (!double.IsFinite(ohms.Real) || !double.IsFinite(ohms.Imaginary))
            throw new ArgumentOutOfRangeException(nameof(ohms));
        Ohms = ohms;
    }

    /// <summary>Series combination.</summary>
    public static Impedance operator +(Impedance a, Impedance b) => new(a.Ohms + b.Ohms);

    /// <summary>Parallel combination.</summary>
    public static Impedance Parallel(Impedance a, Impedance b) =>
        a.Ohms == Complex.Zero || b.Ohms == Complex.Zero
            ? new(Complex.Zero)
            : new(a.Ohms * b.Ohms / (a.Ohms + b.Ohms));
}

/// <summary>Complex RMS voltage.</summary>
public readonly record struct VoltagePhasor(Complex Volts)
{
    /// <summary>AC Ohm's law.</summary>
    public static CurrentPhasor operator /(VoltagePhasor v, Impedance z) =>
        z.Ohms == Complex.Zero ? throw new DivideByZeroException() : new(v.Volts / z.Ohms);
}

/// <summary>Complex RMS current.</summary>
public readonly record struct CurrentPhasor(Complex Amperes);

/// <summary>Ideal lumped-element AC impedance factories.</summary>
public static class AcElectrical
{
    /// <summary>Resistor impedance.</summary>
    public static Impedance Resistor(Resistance r) => new(new Complex(r.Ohms, 0));

    /// <summary>Inductor impedance <c>jωL</c>.</summary>
    public static Impedance Inductor(Inductance l, AngularFrequency f) =>
        new(new Complex(0, f.RadiansPerSecond * l.Henries));

    /// <summary>Capacitor impedance <c>1/(jωC)</c>.</summary>
    public static Impedance Capacitor(Capacitance c, AngularFrequency f)
    {
        if (c.Farads <= 0 || f.RadiansPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(c));
        return new(new Complex(0, -1d / (f.RadiansPerSecond * c.Farads)));
    }
}
