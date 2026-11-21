namespace SharpUnits.Electrical;

/// <summary>Typed electrical charge measured in coulombs.</summary>
public readonly record struct ElectricCharge(double Coulombs) : IComparable<ElectricCharge>
{
    /// <summary>Value in ampere-hours.</summary>
    public double AmpereHours => Coulombs / 3600d;

    /// <summary>Value in milliampere-hours.</summary>
    public double MilliampereHours => Coulombs / 3.6d;

    /// <summary>Creates charge from coulombs.</summary>
    public static ElectricCharge FromCoulombs(double value) => new(value);

    /// <summary>Creates charge from ampere-hours.</summary>
    public static ElectricCharge FromAmpereHours(double value) => new(value * 3600d);

    /// <summary>Creates charge from milliampere-hours.</summary>
    public static ElectricCharge FromMilliampereHours(double value) => new(value * 3.6d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Coulombs, Units.Coulomb);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static ElectricCharge FromQuantity(Quantity quantity) => new(Units.Coulomb.FromQuantity(quantity));

    /// <summary>Adds two charges.</summary>
    public static ElectricCharge operator +(ElectricCharge a, ElectricCharge b) => new(a.Coulombs + b.Coulombs);

    /// <summary>Subtracts two charges.</summary>
    public static ElectricCharge operator -(ElectricCharge a, ElectricCharge b) => new(a.Coulombs - b.Coulombs);

    /// <summary>Charge divided by time produces current.</summary>
    public static Current operator /(ElectricCharge charge, Duration duration) => new(charge.Coulombs / duration.Seconds);

    /// <summary>Charge divided by voltage produces capacitance.</summary>
    public static Capacitance operator /(ElectricCharge charge, Voltage voltage) => new(charge.Coulombs / voltage.Volts);

    /// <summary>Compares charges.</summary>
    public int CompareTo(ElectricCharge other) => Coulombs.CompareTo(other.Coulombs);

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Typed electrical capacitance measured in farads.</summary>
public readonly record struct Capacitance(double Farads) : IComparable<Capacitance>
{
    /// <summary>Value in microfarads.</summary>
    public double Microfarads => Farads * 1e6;

    /// <summary>Value in nanofarads.</summary>
    public double Nanofarads => Farads * 1e9;

    /// <summary>Value in picofarads.</summary>
    public double Picofarads => Farads * 1e12;

    /// <summary>Creates capacitance from farads.</summary>
    public static Capacitance FromFarads(double value) => new(value);

    /// <summary>Creates capacitance from microfarads.</summary>
    public static Capacitance FromMicrofarads(double value) => new(value * 1e-6);

    /// <summary>Creates capacitance from nanofarads.</summary>
    public static Capacitance FromNanofarads(double value) => new(value * 1e-9);

    /// <summary>Creates capacitance from picofarads.</summary>
    public static Capacitance FromPicofarads(double value) => new(value * 1e-12);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Farads, Units.Farad);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Capacitance FromQuantity(Quantity quantity) => new(Units.Farad.FromQuantity(quantity));

    /// <summary>Parallel combination of capacitances.</summary>
    public static Capacitance operator +(Capacitance a, Capacitance b) => new(a.Farads + b.Farads);

    /// <summary>Subtracts capacitances.</summary>
    public static Capacitance operator -(Capacitance a, Capacitance b) => new(a.Farads - b.Farads);

    /// <summary>Capacitance times voltage produces charge.</summary>
    public static ElectricCharge operator *(Capacitance capacitance, Voltage voltage) => new(capacitance.Farads * voltage.Volts);

    /// <summary>RC time constant: <c>C × R = τ</c>.</summary>
    public static Duration operator *(Capacitance c, Resistance r) => new(c.Farads * r.Ohms);

    /// <summary>Compares capacitances.</summary>
    public int CompareTo(Capacitance other) => Farads.CompareTo(other.Farads);

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Typed electrical inductance measured in henries.</summary>
public readonly record struct Inductance(double Henries) : IComparable<Inductance>
{
    /// <summary>Value in millihenries.</summary>
    public double Millihenries => Henries * 1e3;

    /// <summary>Value in microhenries.</summary>
    public double Microhenries => Henries * 1e6;

    /// <summary>Value in nanohenries.</summary>
    public double Nanohenries => Henries * 1e9;

    /// <summary>Creates inductance from henries.</summary>
    public static Inductance FromHenries(double value) => new(value);

    /// <summary>Creates inductance from millihenries.</summary>
    public static Inductance FromMillihenries(double value) => new(value * 1e-3);

    /// <summary>Creates inductance from microhenries.</summary>
    public static Inductance FromMicrohenries(double value) => new(value * 1e-6);

    /// <summary>Creates inductance from nanohenries.</summary>
    public static Inductance FromNanohenries(double value) => new(value * 1e-9);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Henries, Units.Henry);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Inductance FromQuantity(Quantity quantity) => new(Units.Henry.FromQuantity(quantity));

    /// <summary>Series combination of uncoupled inductances.</summary>
    public static Inductance operator +(Inductance a, Inductance b) => new(a.Henries + b.Henries);

    /// <summary>Subtracts inductances.</summary>
    public static Inductance operator -(Inductance a, Inductance b) => new(a.Henries - b.Henries);

    /// <summary>Inductance times current produces magnetic flux linkage in webers.</summary>
    public static FluxLinkage operator *(Inductance inductance, Current current) => new(inductance.Henries * current.Amperes);

    /// <summary>RL time constant: <c>L / R = τ</c>.</summary>
    public static Duration operator /(Inductance l, Resistance r) => new(l.Henries / r.Ohms);

    /// <summary>Compares inductances.</summary>
    public int CompareTo(Inductance other) => Henries.CompareTo(other.Henries);

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Magnetic flux linkage measured in webers (single-turn equivalent).</summary>
public readonly record struct FluxLinkage(double Webers) : IComparable<FluxLinkage>
{
    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Webers, Units.Weber);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static FluxLinkage FromQuantity(Quantity quantity) => new(Units.Weber.FromQuantity(quantity));

    /// <summary>Compares flux linkages.</summary>
    public int CompareTo(FluxLinkage other) => Webers.CompareTo(other.Webers);

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Electrical conductance measured in siemens.</summary>
public readonly record struct Conductance(double Siemens) : IComparable<Conductance>
{
    /// <summary>Value in millisiemens.</summary>
    public double Millisiemens => Siemens * 1e3;

    /// <summary>Converts to resistance (<c>1 / G</c>).</summary>
    public Resistance ToResistance() => new(1d / Siemens);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Siemens, Units.Siemens);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Conductance FromQuantity(Quantity quantity) => new(Units.Siemens.FromQuantity(quantity));

    /// <summary>Adds two conductances.</summary>
    public static Conductance operator +(Conductance a, Conductance b) => new(a.Siemens + b.Siemens);

    /// <summary>Subtracts two conductances.</summary>
    public static Conductance operator -(Conductance a, Conductance b) => new(a.Siemens - b.Siemens);

    /// <summary>Conductance times voltage produces current.</summary>
    public static Current operator *(Conductance conductance, Voltage voltage) => new(conductance.Siemens * voltage.Volts);

    /// <summary>Compares conductances.</summary>
    public int CompareTo(Conductance other) => Siemens.CompareTo(other.Siemens);

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Typed electrical algebra helpers.</summary>
public static class ElectricalRelations
{
    /// <summary>Current integrated over a duration produces charge.</summary>
    public static ElectricCharge Charge(Current current, Duration duration) => current * duration;

    /// <summary>Charge per unit time produces current.</summary>
    public static Current Current(ElectricCharge charge, Duration duration) => charge / duration;

    /// <summary>Voltage divided by current produces resistance.</summary>
    public static Resistance Resistance(Voltage voltage, Current current) => voltage / current;

    /// <summary>Resistance inverted produces conductance.</summary>
    public static Conductance Conductance(Resistance resistance) => resistance.ToConductance();

    /// <summary>Capacitance times voltage produces charge.</summary>
    public static ElectricCharge Charge(Capacitance capacitance, Voltage voltage) => capacitance * voltage;
}
