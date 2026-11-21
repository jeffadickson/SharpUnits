namespace SharpUnits.Electrical;

/// <summary>Idiomatic C# fluent extension methods for electrical and RF quantities.</summary>
public static class ElectricalExtensions
{
    /// <summary>Creates a <see cref="Voltage"/> in volts.</summary>
    public static Voltage Volts(this double value) => Voltage.FromVolts(value);

    /// <summary>Creates a <see cref="Voltage"/> in millivolts.</summary>
    public static Voltage Millivolts(this double value) => Voltage.FromMillivolts(value);

    /// <summary>Creates a <see cref="Voltage"/> in microvolts.</summary>
    public static Voltage Microvolts(this double value) => Voltage.FromMicrovolts(value);

    /// <summary>Creates a <see cref="Voltage"/> in kilovolts.</summary>
    public static Voltage Kilovolts(this double value) => Voltage.FromKilovolts(value);

    /// <summary>Creates a <see cref="Current"/> in amperes.</summary>
    public static Current Amperes(this double value) => Current.FromAmperes(value);

    /// <summary>Creates a <see cref="Current"/> in milliamperes.</summary>
    public static Current Milliamperes(this double value) => Current.FromMilliamperes(value);

    /// <summary>Creates a <see cref="Current"/> in microamperes.</summary>
    public static Current Microamperes(this double value) => Current.FromMicroamperes(value);

    /// <summary>Creates a <see cref="Resistance"/> in ohms.</summary>
    public static Resistance Ohms(this double value) => Resistance.FromOhms(value);

    /// <summary>Creates a <see cref="Resistance"/> in kiloohms.</summary>
    public static Resistance Kiloohms(this double value) => Resistance.FromKiloohms(value);

    /// <summary>Creates a <see cref="Resistance"/> in megaohms.</summary>
    public static Resistance Megaohms(this double value) => Resistance.FromMegaohms(value);

    /// <summary>Creates a <see cref="Capacitance"/> in farads.</summary>
    public static Capacitance Farads(this double value) => Capacitance.FromFarads(value);

    /// <summary>Creates a <see cref="Capacitance"/> in microfarads.</summary>
    public static Capacitance Microfarads(this double value) => Capacitance.FromMicrofarads(value);

    /// <summary>Creates a <see cref="Capacitance"/> in nanofarads.</summary>
    public static Capacitance Nanofarads(this double value) => Capacitance.FromNanofarads(value);

    /// <summary>Creates a <see cref="Capacitance"/> in picofarads.</summary>
    public static Capacitance Picofarads(this double value) => Capacitance.FromPicofarads(value);

    /// <summary>Creates an <see cref="Inductance"/> in henries.</summary>
    public static Inductance Henries(this double value) => Inductance.FromHenries(value);

    /// <summary>Creates an <see cref="Inductance"/> in millihenries.</summary>
    public static Inductance Millihenries(this double value) => Inductance.FromMillihenries(value);

    /// <summary>Creates an <see cref="Inductance"/> in microhenries.</summary>
    public static Inductance Microhenries(this double value) => Inductance.FromMicrohenries(value);

    /// <summary>Creates a relative <see cref="SharpUnits.Electrical.Decibels"/> ratio.</summary>
    public static Decibels Decibels(this double value) => new(value);

    /// <summary>Creates an absolute <see cref="PowerLevelDbm"/> level in dBm.</summary>
    public static PowerLevelDbm Dbm(this double value) => new(value);

    /// <summary>Creates an absolute <see cref="VoltageLevelDbv"/> level in dBV.</summary>
    public static VoltageLevelDbv Dbv(this double value) => new(value);
}
