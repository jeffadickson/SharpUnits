using System.Globalization;

namespace SharpUnits.Electrical;

/// <summary>Relative logarithmic ratio expressed in decibels (<c>dB</c>).</summary>
public readonly record struct Decibels(double Value) : IComparable<Decibels>
{
    /// <summary>Creates decibels from a linear power ratio (<c>10 log10(P2 / P1)</c>).</summary>
    public static Decibels FromPowerRatio(double ratio)
    {
        if (!double.IsFinite(ratio) || ratio <= 0d)
            throw new ArgumentOutOfRangeException(nameof(ratio), "Power ratio must be finite and positive.");
        return new(10d * Math.Log10(ratio));
    }

    /// <summary>Creates decibels from a linear amplitude/field ratio (<c>20 log10(A2 / A1)</c>).</summary>
    public static Decibels FromAmplitudeRatio(double ratio)
    {
        if (!double.IsFinite(ratio) || ratio <= 0d)
            throw new ArgumentOutOfRangeException(nameof(ratio), "Amplitude ratio must be finite and positive.");
        return new(20d * Math.Log10(ratio));
    }

    /// <summary>Converts to a linear power ratio (<c>10^(dB / 10)</c>).</summary>
    public double ToPowerRatio() => Math.Pow(10d, Value / 10d);

    /// <summary>Converts to a linear amplitude/field ratio (<c>10^(dB / 20)</c>).</summary>
    public double ToAmplitudeRatio() => Math.Pow(10d, Value / 20d);

    /// <summary>Adds decibel gains (cascaded stages).</summary>
    public static Decibels operator +(Decibels a, Decibels b) => new(a.Value + b.Value);

    /// <summary>Subtracts decibel gains.</summary>
    public static Decibels operator -(Decibels a, Decibels b) => new(a.Value - b.Value);

    /// <summary>Negates decibel gain.</summary>
    public static Decibels operator -(Decibels a) => new(-a.Value);

    /// <summary>Compares decibel values.</summary>
    public int CompareTo(Decibels other) => Value.CompareTo(other.Value);

    /// <summary>Formats with <c>dB</c> suffix.</summary>
    public override string ToString() => $"{Value.ToString("G4", CultureInfo.InvariantCulture)} dB";
}

/// <summary>Absolute power level referenced to 1 milliwatt (<c>dBm</c>).</summary>
public readonly record struct PowerLevelDbm(double Value) : IComparable<PowerLevelDbm>
{
    /// <summary>Creates a <c>dBm</c> level from electric power.</summary>
    public static PowerLevelDbm FromPower(ElectricPower power)
    {
        if (!double.IsFinite(power.Watts) || power.Watts <= 0d)
            throw new ArgumentOutOfRangeException(nameof(power), "Power must be finite and positive.");
        return new(10d * Math.Log10(power.Watts / 1e-3d));
    }

    /// <summary>Creates a <c>dBm</c> level from RMS voltage across a reference load impedance (default 50 Ω).</summary>
    public static PowerLevelDbm FromRmsVoltage(Voltage rmsVoltage, Resistance? referenceImpedance = null)
    {
        double r = (referenceImpedance ?? new Resistance(50d)).Ohms;
        if (!double.IsFinite(r) || r <= 0d) throw new ArgumentOutOfRangeException(nameof(referenceImpedance));
        return FromPower(new ElectricPower((rmsVoltage.Volts * rmsVoltage.Volts) / r));
    }

    /// <summary>Converts to linear electric power in watts.</summary>
    public ElectricPower ToPower() => new(1e-3d * Math.Pow(10d, Value / 10d));

    /// <summary>Converts to RMS voltage across a reference load impedance (default 50 Ω).</summary>
    public Voltage ToRmsVoltage(Resistance? referenceImpedance = null)
    {
        double r = (referenceImpedance ?? new Resistance(50d)).Ohms;
        if (!double.IsFinite(r) || r <= 0d) throw new ArgumentOutOfRangeException(nameof(referenceImpedance));
        return new(Math.Sqrt(ToPower().Watts * r));
    }

    /// <summary>Applies a decibel gain/attenuation to a power level.</summary>
    public static PowerLevelDbm operator +(PowerLevelDbm level, Decibels gain) => new(level.Value + gain.Value);

    /// <summary>Applies decibel attenuation to a power level.</summary>
    public static PowerLevelDbm operator -(PowerLevelDbm level, Decibels loss) => new(level.Value - loss.Value);

    /// <summary>Difference between two absolute power levels yields a relative decibel ratio.</summary>
    public static Decibels operator -(PowerLevelDbm a, PowerLevelDbm b) => new(a.Value - b.Value);

    /// <summary>Compares power levels.</summary>
    public int CompareTo(PowerLevelDbm other) => Value.CompareTo(other.Value);

    /// <summary>Formats with <c>dBm</c> suffix.</summary>
    public override string ToString() => $"{Value.ToString("G4", CultureInfo.InvariantCulture)} dBm";
}

/// <summary>Absolute RMS voltage level referenced to 1 volt (<c>dBV</c>).</summary>
public readonly record struct VoltageLevelDbv(double Value) : IComparable<VoltageLevelDbv>
{
    /// <summary>Creates a <c>dBV</c> level from a positive RMS voltage.</summary>
    public static VoltageLevelDbv FromVoltage(Voltage rmsVoltage)
    {
        if (!double.IsFinite(rmsVoltage.Volts) || rmsVoltage.Volts <= 0d)
            throw new ArgumentOutOfRangeException(nameof(rmsVoltage), "RMS voltage must be finite and positive.");
        return new(20d * Math.Log10(rmsVoltage.Volts));
    }

    /// <summary>Converts to linear RMS voltage.</summary>
    public Voltage ToVoltage() => new(Math.Pow(10d, Value / 20d));

    /// <summary>Applies a decibel gain to a voltage level.</summary>
    public static VoltageLevelDbv operator +(VoltageLevelDbv level, Decibels gain) => new(level.Value + gain.Value);

    /// <summary>Applies decibel attenuation to a voltage level.</summary>
    public static VoltageLevelDbv operator -(VoltageLevelDbv level, Decibels loss) => new(level.Value - loss.Value);

    /// <summary>Difference between two <c>dBV</c> levels yields a relative decibel ratio.</summary>
    public static Decibels operator -(VoltageLevelDbv a, VoltageLevelDbv b) => new(a.Value - b.Value);

    /// <summary>Compares voltage levels.</summary>
    public int CompareTo(VoltageLevelDbv other) => Value.CompareTo(other.Value);

    /// <summary>Formats with <c>dBV</c> suffix.</summary>
    public override string ToString() => $"{Value.ToString("G4", CultureInfo.InvariantCulture)} dBV";
}
