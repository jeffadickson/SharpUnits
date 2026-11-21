namespace SharpUnits.Electrical;

/// <summary>Scale-aware electrical quantity factories.</summary>
public static class ElectricalQuantities
{
    /// <summary>Creates current from milliamperes.</summary>
    public static Current Milliamperes(double value) => Current.FromMilliamperes(value);

    /// <summary>Creates resistance from kiloohms.</summary>
    public static Resistance Kiloohms(double value) => Resistance.FromKiloohms(value);

    /// <summary>Creates voltage from millivolts.</summary>
    public static Voltage Millivolts(double value) => Voltage.FromMillivolts(value);
}

/// <summary>Strongly-typed electrical current measured in amperes.</summary>
public readonly record struct Current(double Amperes) : IComparable<Current>
{
    /// <summary>Value in milliamperes.</summary>
    public double Milliamperes => Amperes * 1e3;

    /// <summary>Value in microamperes.</summary>
    public double Microamperes => Amperes * 1e6;

    /// <summary>Value in nanoamperes.</summary>
    public double Nanoamperes => Amperes * 1e9;

    /// <summary>Creates current from amperes.</summary>
    public static Current FromAmperes(double value) => new(value);

    /// <summary>Creates current from milliamperes.</summary>
    public static Current FromMilliamperes(double value) => new(value * 1e-3);

    /// <summary>Creates current from microamperes.</summary>
    public static Current FromMicroamperes(double value) => new(value * 1e-6);

    /// <summary>Creates current from nanoamperes.</summary>
    public static Current FromNanoamperes(double value) => new(value * 1e-9);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Amperes, Units.Ampere);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Current FromQuantity(Quantity quantity) => new(Units.Ampere.FromQuantity(quantity));

    /// <summary>Adds two currents.</summary>
    public static Current operator +(Current a, Current b) => new(a.Amperes + b.Amperes);

    /// <summary>Subtracts two currents.</summary>
    public static Current operator -(Current a, Current b) => new(a.Amperes - b.Amperes);

    /// <summary>Negates a current.</summary>
    public static Current operator -(Current a) => new(-a.Amperes);

    /// <summary>Scales a current.</summary>
    public static Current operator *(Current a, double scalar) => new(a.Amperes * scalar);

    /// <summary>Scales a current.</summary>
    public static Current operator *(double scalar, Current a) => new(scalar * a.Amperes);

    /// <summary>Divides a current by a scalar.</summary>
    public static Current operator /(Current a, double divisor) => new(a.Amperes / divisor);

    /// <summary>Dimensionless ratio of two currents.</summary>
    public static double operator /(Current a, Current b) => a.Amperes / b.Amperes;

    /// <summary>Ohm's law: <c>I × R = V</c>.</summary>
    public static Voltage operator *(Current a, Resistance b) => new(a.Amperes * b.Ohms);

    /// <summary>Instantaneous power: <c>I × V = P</c>.</summary>
    public static ElectricPower operator *(Current a, Voltage b) => new(a.Amperes * b.Volts);

    /// <summary>Current integrated over duration produces electric charge.</summary>
    public static ElectricCharge operator *(Current current, Duration duration) => new(current.Amperes * duration.Seconds);

    /// <summary>Compares currents.</summary>
    public int CompareTo(Current other) => Amperes.CompareTo(other.Amperes);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Current a, Current b) => a.Amperes < b.Amperes;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Current a, Current b) => a.Amperes <= b.Amperes;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Current a, Current b) => a.Amperes > b.Amperes;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Current a, Current b) => a.Amperes >= b.Amperes;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed electrical resistance measured in ohms.</summary>
public readonly record struct Resistance(double Ohms) : IComparable<Resistance>
{
    /// <summary>Value in milliohms.</summary>
    public double Milliohms => Ohms * 1e3;

    /// <summary>Value in kiloohms.</summary>
    public double Kiloohms => Ohms * 1e-3;

    /// <summary>Value in megaohms.</summary>
    public double Megaohms => Ohms * 1e-6;

    /// <summary>Creates resistance from ohms.</summary>
    public static Resistance FromOhms(double value) => new(value);

    /// <summary>Creates resistance from milliohms.</summary>
    public static Resistance FromMilliohms(double value) => new(value * 1e-3);

    /// <summary>Creates resistance from kiloohms.</summary>
    public static Resistance FromKiloohms(double value) => new(value * 1e3);

    /// <summary>Creates resistance from megaohms.</summary>
    public static Resistance FromMegaohms(double value) => new(value * 1e6);

    /// <summary>Computes the parallel combination of two resistances.</summary>
    public static Resistance Parallel(Resistance a, Resistance b) =>
        a.Ohms == 0d || b.Ohms == 0d ? new(0d) : new((a.Ohms * b.Ohms) / (a.Ohms + b.Ohms));

    /// <summary>Converts to conductance (<c>1 / R</c>).</summary>
    public Conductance ToConductance() => new(1d / Ohms);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Ohms, Units.Ohm);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Resistance FromQuantity(Quantity quantity) => new(Units.Ohm.FromQuantity(quantity));

    /// <summary>Series combination of two resistances.</summary>
    public static Resistance operator +(Resistance a, Resistance b) => new(a.Ohms + b.Ohms);

    /// <summary>Subtracts two resistances.</summary>
    public static Resistance operator -(Resistance a, Resistance b) => new(a.Ohms - b.Ohms);

    /// <summary>Scales a resistance.</summary>
    public static Resistance operator *(Resistance a, double scalar) => new(a.Ohms * scalar);

    /// <summary>Scales a resistance.</summary>
    public static Resistance operator *(double scalar, Resistance a) => new(scalar * a.Ohms);

    /// <summary>Divides a resistance by a scalar.</summary>
    public static Resistance operator /(Resistance a, double divisor) => new(a.Ohms / divisor);

    /// <summary>Dimensionless ratio of two resistances.</summary>
    public static double operator /(Resistance a, Resistance b) => a.Ohms / b.Ohms;

    /// <summary>Ohm's law: <c>R × I = V</c>.</summary>
    public static Voltage operator *(Resistance a, Current b) => b * a;

    /// <summary>RC time constant: <c>R × C = τ</c>.</summary>
    public static Duration operator *(Resistance r, Capacitance c) => new(r.Ohms * c.Farads);

    /// <summary>Compares resistances.</summary>
    public int CompareTo(Resistance other) => Ohms.CompareTo(other.Ohms);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Resistance a, Resistance b) => a.Ohms < b.Ohms;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Resistance a, Resistance b) => a.Ohms <= b.Ohms;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Resistance a, Resistance b) => a.Ohms > b.Ohms;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Resistance a, Resistance b) => a.Ohms >= b.Ohms;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed potential difference measured in volts.</summary>
public readonly record struct Voltage(double Volts) : IComparable<Voltage>
{
    /// <summary>Value in millivolts.</summary>
    public double Millivolts => Volts * 1e3;

    /// <summary>Value in microvolts.</summary>
    public double Microvolts => Volts * 1e6;

    /// <summary>Value in kilovolts.</summary>
    public double Kilovolts => Volts * 1e-3;

    /// <summary>Creates voltage from volts.</summary>
    public static Voltage FromVolts(double value) => new(value);

    /// <summary>Creates voltage from millivolts.</summary>
    public static Voltage FromMillivolts(double value) => new(value * 1e-3);

    /// <summary>Creates voltage from microvolts.</summary>
    public static Voltage FromMicrovolts(double value) => new(value * 1e-6);

    /// <summary>Creates voltage from kilovolts.</summary>
    public static Voltage FromKilovolts(double value) => new(value * 1e3);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Volts, Units.Volt);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Voltage FromQuantity(Quantity quantity) => new(Units.Volt.FromQuantity(quantity));

    /// <summary>Adds two voltages.</summary>
    public static Voltage operator +(Voltage a, Voltage b) => new(a.Volts + b.Volts);

    /// <summary>Subtracts two voltages.</summary>
    public static Voltage operator -(Voltage a, Voltage b) => new(a.Volts - b.Volts);

    /// <summary>Negates a voltage.</summary>
    public static Voltage operator -(Voltage a) => new(-a.Volts);

    /// <summary>Scales a voltage.</summary>
    public static Voltage operator *(Voltage a, double scalar) => new(a.Volts * scalar);

    /// <summary>Scales a voltage.</summary>
    public static Voltage operator *(double scalar, Voltage a) => new(scalar * a.Volts);

    /// <summary>Divides a voltage by a scalar.</summary>
    public static Voltage operator /(Voltage a, double divisor) => new(a.Volts / divisor);

    /// <summary>Dimensionless voltage ratio (gain).</summary>
    public static double operator /(Voltage a, Voltage b) => a.Volts / b.Volts;

    /// <summary>Ohm's law: <c>V / R = I</c>.</summary>
    public static Current operator /(Voltage a, Resistance b) => new(a.Volts / b.Ohms);

    /// <summary>Ohm's law: <c>V / I = R</c>.</summary>
    public static Resistance operator /(Voltage a, Current b) => new(a.Volts / b.Amperes);

    /// <summary>Instantaneous power: <c>V × I = P</c>.</summary>
    public static ElectricPower operator *(Voltage a, Current b) => b * a;

    /// <summary>Compares voltages.</summary>
    public int CompareTo(Voltage other) => Volts.CompareTo(other.Volts);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Voltage a, Voltage b) => a.Volts < b.Volts;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Voltage a, Voltage b) => a.Volts <= b.Volts;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Voltage a, Voltage b) => a.Volts > b.Volts;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Voltage a, Voltage b) => a.Volts >= b.Volts;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed instantaneous electric power measured in watts.</summary>
public readonly record struct ElectricPower(double Watts) : IComparable<ElectricPower>
{
    /// <summary>Value in milliwatts.</summary>
    public double Milliwatts => Watts * 1e3;

    /// <summary>Value in microwatts.</summary>
    public double Microwatts => Watts * 1e6;

    /// <summary>Value in kilowatts.</summary>
    public double Kilowatts => Watts * 1e-3;

    /// <summary>Creates electric power from watts.</summary>
    public static ElectricPower FromWatts(double value) => new(value);

    /// <summary>Creates electric power from milliwatts.</summary>
    public static ElectricPower FromMilliwatts(double value) => new(value * 1e-3);

    /// <summary>Creates electric power from kilowatts.</summary>
    public static ElectricPower FromKilowatts(double value) => new(value * 1e3);

    /// <summary>Converts to a core <see cref="Power"/>.</summary>
    public Power ToPower() => new(Watts);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Watts, Units.Watt);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static ElectricPower FromQuantity(Quantity quantity) => new(Units.Watt.FromQuantity(quantity));

    /// <summary>Adds two electric powers.</summary>
    public static ElectricPower operator +(ElectricPower a, ElectricPower b) => new(a.Watts + b.Watts);

    /// <summary>Subtracts two electric powers.</summary>
    public static ElectricPower operator -(ElectricPower a, ElectricPower b) => new(a.Watts - b.Watts);

    /// <summary>Negates electric power.</summary>
    public static ElectricPower operator -(ElectricPower a) => new(-a.Watts);

    /// <summary>Scales electric power.</summary>
    public static ElectricPower operator *(ElectricPower a, double scalar) => new(a.Watts * scalar);

    /// <summary>Scales electric power.</summary>
    public static ElectricPower operator *(double scalar, ElectricPower a) => new(scalar * a.Watts);

    /// <summary>Divides electric power by a scalar.</summary>
    public static ElectricPower operator /(ElectricPower a, double divisor) => new(a.Watts / divisor);

    /// <summary>Dimensionless ratio of two powers.</summary>
    public static double operator /(ElectricPower a, ElectricPower b) => a.Watts / b.Watts;

    /// <summary>Power divided by voltage produces current.</summary>
    public static Current operator /(ElectricPower p, Voltage v) => new(p.Watts / v.Volts);

    /// <summary>Power divided by current produces voltage.</summary>
    public static Voltage operator /(ElectricPower p, Current i) => new(p.Watts / i.Amperes);

    /// <summary>Power integrated over duration produces energy.</summary>
    public static Energy operator *(ElectricPower p, Duration d) => new(p.Watts * d.Seconds);

    /// <summary>Compares electric powers.</summary>
    public int CompareTo(ElectricPower other) => Watts.CompareTo(other.Watts);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(ElectricPower a, ElectricPower b) => a.Watts < b.Watts;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(ElectricPower a, ElectricPower b) => a.Watts <= b.Watts;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(ElectricPower a, ElectricPower b) => a.Watts > b.Watts;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(ElectricPower a, ElectricPower b) => a.Watts >= b.Watts;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}
