namespace SharpUnits;

/// <summary>Strongly-typed length measured in meters.</summary>
public readonly record struct Length(double Meters) : IComparable<Length>
{
    /// <summary>Value in millimeters.</summary>
    public double Millimeters => Meters * 1e3;

    /// <summary>Value in micrometers.</summary>
    public double Micrometers => Meters * 1e6;

    /// <summary>Value in kilometers.</summary>
    public double Kilometers => Meters * 1e-3;

    /// <summary>Value in inches.</summary>
    public double Inches => Meters / 0.0254d;

    /// <summary>Value in feet.</summary>
    public double Feet => Meters / 0.3048d;

    /// <summary>Creates a length from meters.</summary>
    public static Length FromMeters(double value) => new(value);

    /// <summary>Creates a length from millimeters.</summary>
    public static Length FromMillimeters(double value) => new(value * 1e-3);

    /// <summary>Creates a length from micrometers.</summary>
    public static Length FromMicrometers(double value) => new(value * 1e-6);

    /// <summary>Creates a length from kilometers.</summary>
    public static Length FromKilometers(double value) => new(value * 1e3);

    /// <summary>Creates a length from inches.</summary>
    public static Length FromInches(double value) => new(value * 0.0254d);

    /// <summary>Creates a length from feet.</summary>
    public static Length FromFeet(double value) => new(value * 0.3048d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Meters, Units.Meter);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Length FromQuantity(Quantity quantity) => new(Units.Meter.FromQuantity(quantity));

    /// <summary>Adds two lengths.</summary>
    public static Length operator +(Length a, Length b) => new(a.Meters + b.Meters);

    /// <summary>Subtracts two lengths.</summary>
    public static Length operator -(Length a, Length b) => new(a.Meters - b.Meters);

    /// <summary>Negates a length.</summary>
    public static Length operator -(Length a) => new(-a.Meters);

    /// <summary>Scales a length.</summary>
    public static Length operator *(Length a, double scalar) => new(a.Meters * scalar);

    /// <summary>Scales a length.</summary>
    public static Length operator *(double scalar, Length a) => new(scalar * a.Meters);

    /// <summary>Divides a length by a scalar.</summary>
    public static Length operator /(Length a, double divisor) => new(a.Meters / divisor);

    /// <summary>Dimensionless ratio of two lengths.</summary>
    public static double operator /(Length a, Length b) => a.Meters / b.Meters;

    /// <summary>Compares lengths.</summary>
    public int CompareTo(Length other) => Meters.CompareTo(other.Meters);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Length a, Length b) => a.Meters < b.Meters;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Length a, Length b) => a.Meters <= b.Meters;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Length a, Length b) => a.Meters > b.Meters;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Length a, Length b) => a.Meters >= b.Meters;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed mass measured in kilograms.</summary>
public readonly record struct Mass(double Kilograms) : IComparable<Mass>
{
    /// <summary>Value in grams.</summary>
    public double Grams => Kilograms * 1e3;

    /// <summary>Value in milligrams.</summary>
    public double Milligrams => Kilograms * 1e6;

    /// <summary>Value in pounds.</summary>
    public double Pounds => Kilograms / 0.45359237d;

    /// <summary>Creates a mass from kilograms.</summary>
    public static Mass FromKilograms(double value) => new(value);

    /// <summary>Creates a mass from grams.</summary>
    public static Mass FromGrams(double value) => new(value * 1e-3);

    /// <summary>Creates a mass from milligrams.</summary>
    public static Mass FromMilligrams(double value) => new(value * 1e-6);

    /// <summary>Creates a mass from pounds.</summary>
    public static Mass FromPounds(double value) => new(value * 0.45359237d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Kilograms, Units.Kilogram);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Mass FromQuantity(Quantity quantity) => new(Units.Kilogram.FromQuantity(quantity));

    /// <summary>Adds two masses.</summary>
    public static Mass operator +(Mass a, Mass b) => new(a.Kilograms + b.Kilograms);

    /// <summary>Subtracts two masses.</summary>
    public static Mass operator -(Mass a, Mass b) => new(a.Kilograms - b.Kilograms);

    /// <summary>Negates a mass.</summary>
    public static Mass operator -(Mass a) => new(-a.Kilograms);

    /// <summary>Scales a mass.</summary>
    public static Mass operator *(Mass a, double scalar) => new(a.Kilograms * scalar);

    /// <summary>Scales a mass.</summary>
    public static Mass operator *(double scalar, Mass a) => new(scalar * a.Kilograms);

    /// <summary>Divides a mass by a scalar.</summary>
    public static Mass operator /(Mass a, double divisor) => new(a.Kilograms / divisor);

    /// <summary>Dimensionless ratio of two masses.</summary>
    public static double operator /(Mass a, Mass b) => a.Kilograms / b.Kilograms;

    /// <summary>Compares masses.</summary>
    public int CompareTo(Mass other) => Kilograms.CompareTo(other.Kilograms);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Mass a, Mass b) => a.Kilograms < b.Kilograms;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Mass a, Mass b) => a.Kilograms <= b.Kilograms;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Mass a, Mass b) => a.Kilograms > b.Kilograms;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Mass a, Mass b) => a.Kilograms >= b.Kilograms;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed elapsed duration measured in seconds.</summary>
public readonly record struct Duration(double Seconds) : IComparable<Duration>
{
    /// <summary>Value in milliseconds.</summary>
    public double Milliseconds => Seconds * 1e3;

    /// <summary>Value in microseconds.</summary>
    public double Microseconds => Seconds * 1e6;

    /// <summary>Value in nanoseconds.</summary>
    public double Nanoseconds => Seconds * 1e9;

    /// <summary>Value in minutes.</summary>
    public double Minutes => Seconds / 60d;

    /// <summary>Value in hours.</summary>
    public double Hours => Seconds / 3600d;

    /// <summary>Creates a duration from seconds.</summary>
    public static Duration FromSeconds(double value) => new(value);

    /// <summary>Creates a duration from milliseconds.</summary>
    public static Duration FromMilliseconds(double value) => new(value * 1e-3);

    /// <summary>Creates a duration from microseconds.</summary>
    public static Duration FromMicroseconds(double value) => new(value * 1e-6);

    /// <summary>Creates a duration from nanoseconds.</summary>
    public static Duration FromNanoseconds(double value) => new(value * 1e-9);

    /// <summary>Creates a duration from minutes.</summary>
    public static Duration FromMinutes(double value) => new(value * 60d);

    /// <summary>Creates a duration from hours.</summary>
    public static Duration FromHours(double value) => new(value * 3600d);

    /// <summary>Creates a duration from a <see cref="TimeSpan"/>.</summary>
    public static Duration FromTimeSpan(TimeSpan span) => new(span.TotalSeconds);

    /// <summary>Converts to a <see cref="TimeSpan"/>.</summary>
    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Seconds);

    /// <summary>Reciprocal frequency (<c>1 / T</c>).</summary>
    public Frequency ToFrequency() => new(1d / Seconds);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Seconds, Units.Second);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Duration FromQuantity(Quantity quantity) => new(Units.Second.FromQuantity(quantity));

    /// <summary>Adds two durations.</summary>
    public static Duration operator +(Duration a, Duration b) => new(a.Seconds + b.Seconds);

    /// <summary>Subtracts two durations.</summary>
    public static Duration operator -(Duration a, Duration b) => new(a.Seconds - b.Seconds);

    /// <summary>Negates a duration.</summary>
    public static Duration operator -(Duration a) => new(-a.Seconds);

    /// <summary>Scales a duration.</summary>
    public static Duration operator *(Duration a, double scalar) => new(a.Seconds * scalar);

    /// <summary>Scales a duration.</summary>
    public static Duration operator *(double scalar, Duration a) => new(scalar * a.Seconds);

    /// <summary>Divides a duration by a scalar.</summary>
    public static Duration operator /(Duration a, double divisor) => new(a.Seconds / divisor);

    /// <summary>Dimensionless ratio of two durations.</summary>
    public static double operator /(Duration a, Duration b) => a.Seconds / b.Seconds;

    /// <summary>Compares durations.</summary>
    public int CompareTo(Duration other) => Seconds.CompareTo(other.Seconds);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Duration a, Duration b) => a.Seconds < b.Seconds;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Duration a, Duration b) => a.Seconds <= b.Seconds;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Duration a, Duration b) => a.Seconds > b.Seconds;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Duration a, Duration b) => a.Seconds >= b.Seconds;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed frequency measured in hertz.</summary>
public readonly record struct Frequency(double Hertz) : IComparable<Frequency>
{
    /// <summary>Value in kilohertz.</summary>
    public double Kilohertz => Hertz * 1e-3;

    /// <summary>Value in megahertz.</summary>
    public double Megahertz => Hertz * 1e-6;

    /// <summary>Value in gigahertz.</summary>
    public double Gigahertz => Hertz * 1e-9;

    /// <summary>Corresponding period (<c>1 / f</c>).</summary>
    public Duration Period => new(1d / Hertz);

    /// <summary>Creates a frequency from hertz.</summary>
    public static Frequency FromHertz(double value) => new(value);

    /// <summary>Creates a frequency from kilohertz.</summary>
    public static Frequency FromKilohertz(double value) => new(value * 1e3);

    /// <summary>Creates a frequency from megahertz.</summary>
    public static Frequency FromMegahertz(double value) => new(value * 1e6);

    /// <summary>Creates a frequency from gigahertz.</summary>
    public static Frequency FromGigahertz(double value) => new(value * 1e9);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Hertz, Units.Hertz);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Frequency FromQuantity(Quantity quantity) => new(Units.Hertz.FromQuantity(quantity));

    /// <summary>Adds two frequencies.</summary>
    public static Frequency operator +(Frequency a, Frequency b) => new(a.Hertz + b.Hertz);

    /// <summary>Subtracts two frequencies.</summary>
    public static Frequency operator -(Frequency a, Frequency b) => new(a.Hertz - b.Hertz);

    /// <summary>Negates a frequency.</summary>
    public static Frequency operator -(Frequency a) => new(-a.Hertz);

    /// <summary>Scales a frequency.</summary>
    public static Frequency operator *(Frequency a, double scalar) => new(a.Hertz * scalar);

    /// <summary>Scales a frequency.</summary>
    public static Frequency operator *(double scalar, Frequency a) => new(scalar * a.Hertz);

    /// <summary>Divides a frequency by a scalar.</summary>
    public static Frequency operator /(Frequency a, double divisor) => new(a.Hertz / divisor);

    /// <summary>Dimensionless ratio of two frequencies.</summary>
    public static double operator /(Frequency a, Frequency b) => a.Hertz / b.Hertz;

    /// <summary>Compares frequencies.</summary>
    public int CompareTo(Frequency other) => Hertz.CompareTo(other.Hertz);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Frequency a, Frequency b) => a.Hertz < b.Hertz;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Frequency a, Frequency b) => a.Hertz <= b.Hertz;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Frequency a, Frequency b) => a.Hertz > b.Hertz;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Frequency a, Frequency b) => a.Hertz >= b.Hertz;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed force measured in newtons.</summary>
public readonly record struct Force(double Newtons) : IComparable<Force>
{
    /// <summary>Value in kilonewtons.</summary>
    public double Kilonewtons => Newtons * 1e-3;

    /// <summary>Value in pound-force.</summary>
    public double PoundsForce => Newtons / 4.4482216152605d;

    /// <summary>Creates a force from newtons.</summary>
    public static Force FromNewtons(double value) => new(value);

    /// <summary>Creates a force from pound-force.</summary>
    public static Force FromPoundsForce(double value) => new(value * 4.4482216152605d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Newtons, Units.Newton);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Force FromQuantity(Quantity quantity) => new(Units.Newton.FromQuantity(quantity));

    /// <summary>Adds two forces.</summary>
    public static Force operator +(Force a, Force b) => new(a.Newtons + b.Newtons);

    /// <summary>Subtracts two forces.</summary>
    public static Force operator -(Force a, Force b) => new(a.Newtons - b.Newtons);

    /// <summary>Negates a force.</summary>
    public static Force operator -(Force a) => new(-a.Newtons);

    /// <summary>Scales a force.</summary>
    public static Force operator *(Force a, double scalar) => new(a.Newtons * scalar);

    /// <summary>Scales a force.</summary>
    public static Force operator *(double scalar, Force a) => new(scalar * a.Newtons);

    /// <summary>Divides a force by a scalar.</summary>
    public static Force operator /(Force a, double divisor) => new(a.Newtons / divisor);

    /// <summary>Dimensionless ratio of two forces.</summary>
    public static double operator /(Force a, Force b) => a.Newtons / b.Newtons;

    /// <summary>Compares forces.</summary>
    public int CompareTo(Force other) => Newtons.CompareTo(other.Newtons);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Force a, Force b) => a.Newtons < b.Newtons;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Force a, Force b) => a.Newtons <= b.Newtons;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Force a, Force b) => a.Newtons > b.Newtons;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Force a, Force b) => a.Newtons >= b.Newtons;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed pressure measured in pascals.</summary>
public readonly record struct Pressure(double Pascals) : IComparable<Pressure>
{
    /// <summary>Value in kilopascals.</summary>
    public double Kilopascals => Pascals * 1e-3;

    /// <summary>Value in megapascals.</summary>
    public double Megapascals => Pascals * 1e-6;

    /// <summary>Value in bar.</summary>
    public double Bar => Pascals * 1e-5;

    /// <summary>Value in psi.</summary>
    public double Psi => Pascals / 6894.757293168361d;

    /// <summary>Value in standard atmospheres.</summary>
    public double Atmospheres => Pascals / 101325d;

    /// <summary>Creates a pressure from pascals.</summary>
    public static Pressure FromPascals(double value) => new(value);

    /// <summary>Creates a pressure from kilopascals.</summary>
    public static Pressure FromKilopascals(double value) => new(value * 1e3);

    /// <summary>Creates a pressure from bar.</summary>
    public static Pressure FromBar(double value) => new(value * 1e5);

    /// <summary>Creates a pressure from psi.</summary>
    public static Pressure FromPsi(double value) => new(value * 6894.757293168361d);

    /// <summary>Creates a pressure from atmospheres.</summary>
    public static Pressure FromAtmospheres(double value) => new(value * 101325d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Pascals, Units.Pascal);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Pressure FromQuantity(Quantity quantity) => new(Units.Pascal.FromQuantity(quantity));

    /// <summary>Adds two pressures.</summary>
    public static Pressure operator +(Pressure a, Pressure b) => new(a.Pascals + b.Pascals);

    /// <summary>Subtracts two pressures.</summary>
    public static Pressure operator -(Pressure a, Pressure b) => new(a.Pascals - b.Pascals);

    /// <summary>Negates a pressure.</summary>
    public static Pressure operator -(Pressure a) => new(-a.Pascals);

    /// <summary>Scales a pressure.</summary>
    public static Pressure operator *(Pressure a, double scalar) => new(a.Pascals * scalar);

    /// <summary>Scales a pressure.</summary>
    public static Pressure operator *(double scalar, Pressure a) => new(scalar * a.Pascals);

    /// <summary>Divides a pressure by a scalar.</summary>
    public static Pressure operator /(Pressure a, double divisor) => new(a.Pascals / divisor);

    /// <summary>Dimensionless ratio of two pressures.</summary>
    public static double operator /(Pressure a, Pressure b) => a.Pascals / b.Pascals;

    /// <summary>Compares pressures.</summary>
    public int CompareTo(Pressure other) => Pascals.CompareTo(other.Pascals);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Pressure a, Pressure b) => a.Pascals < b.Pascals;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Pressure a, Pressure b) => a.Pascals <= b.Pascals;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Pressure a, Pressure b) => a.Pascals > b.Pascals;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Pressure a, Pressure b) => a.Pascals >= b.Pascals;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed energy measured in joules.</summary>
public readonly record struct Energy(double Joules) : IComparable<Energy>
{
    /// <summary>Value in kilojoules.</summary>
    public double Kilojoules => Joules * 1e-3;

    /// <summary>Value in watt-hours.</summary>
    public double WattHours => Joules / 3600d;

    /// <summary>Value in kilowatt-hours.</summary>
    public double KilowattHours => Joules / 3.6e6;

    /// <summary>Creates energy from joules.</summary>
    public static Energy FromJoules(double value) => new(value);

    /// <summary>Creates energy from watt-hours.</summary>
    public static Energy FromWattHours(double value) => new(value * 3600d);

    /// <summary>Creates energy from kilowatt-hours.</summary>
    public static Energy FromKilowattHours(double value) => new(value * 3.6e6);

    /// <summary>Converts to a unified <see cref="Quantity"/> tagged with <see cref="QuantityKind.Energy"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Joules, Units.Joule);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Energy FromQuantity(Quantity quantity) => new(Units.Joule.FromQuantity(quantity));

    /// <summary>Adds two energies.</summary>
    public static Energy operator +(Energy a, Energy b) => new(a.Joules + b.Joules);

    /// <summary>Subtracts two energies.</summary>
    public static Energy operator -(Energy a, Energy b) => new(a.Joules - b.Joules);

    /// <summary>Negates energy.</summary>
    public static Energy operator -(Energy a) => new(-a.Joules);

    /// <summary>Scales energy.</summary>
    public static Energy operator *(Energy a, double scalar) => new(a.Joules * scalar);

    /// <summary>Scales energy.</summary>
    public static Energy operator *(double scalar, Energy a) => new(scalar * a.Joules);

    /// <summary>Divides energy by a scalar.</summary>
    public static Energy operator /(Energy a, double divisor) => new(a.Joules / divisor);

    /// <summary>Energy divided by duration yields power.</summary>
    public static Power operator /(Energy energy, Duration duration) => new(energy.Joules / duration.Seconds);

    /// <summary>Dimensionless ratio of two energies.</summary>
    public static double operator /(Energy a, Energy b) => a.Joules / b.Joules;

    /// <summary>Compares energies.</summary>
    public int CompareTo(Energy other) => Joules.CompareTo(other.Joules);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Energy a, Energy b) => a.Joules < b.Joules;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Energy a, Energy b) => a.Joules <= b.Joules;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Energy a, Energy b) => a.Joules > b.Joules;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Energy a, Energy b) => a.Joules >= b.Joules;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Strongly-typed power measured in watts.</summary>
public readonly record struct Power(double Watts) : IComparable<Power>
{
    /// <summary>Value in milliwatts.</summary>
    public double Milliwatts => Watts * 1e3;

    /// <summary>Value in kilowatts.</summary>
    public double Kilowatts => Watts * 1e-3;

    /// <summary>Value in megawatts.</summary>
    public double Megawatts => Watts * 1e-6;

    /// <summary>Creates power from watts.</summary>
    public static Power FromWatts(double value) => new(value);

    /// <summary>Creates power from milliwatts.</summary>
    public static Power FromMilliwatts(double value) => new(value * 1e-3);

    /// <summary>Creates power from kilowatts.</summary>
    public static Power FromKilowatts(double value) => new(value * 1e3);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Watts, Units.Watt);

    /// <summary>Creates from a dimensionally compatible <see cref="Quantity"/>.</summary>
    public static Power FromQuantity(Quantity quantity) => new(Units.Watt.FromQuantity(quantity));

    /// <summary>Adds two powers.</summary>
    public static Power operator +(Power a, Power b) => new(a.Watts + b.Watts);

    /// <summary>Subtracts two powers.</summary>
    public static Power operator -(Power a, Power b) => new(a.Watts - b.Watts);

    /// <summary>Negates power.</summary>
    public static Power operator -(Power a) => new(-a.Watts);

    /// <summary>Scales power.</summary>
    public static Power operator *(Power a, double scalar) => new(a.Watts * scalar);

    /// <summary>Scales power.</summary>
    public static Power operator *(double scalar, Power a) => new(scalar * a.Watts);

    /// <summary>Power integrated over duration yields energy.</summary>
    public static Energy operator *(Power power, Duration duration) => new(power.Watts * duration.Seconds);

    /// <summary>Duration times power yields energy.</summary>
    public static Energy operator *(Duration duration, Power power) => new(duration.Seconds * power.Watts);

    /// <summary>Divides power by a scalar.</summary>
    public static Power operator /(Power a, double divisor) => new(a.Watts / divisor);

    /// <summary>Dimensionless ratio of two powers.</summary>
    public static double operator /(Power a, Power b) => a.Watts / b.Watts;

    /// <summary>Compares powers.</summary>
    public int CompareTo(Power other) => Watts.CompareTo(other.Watts);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(Power a, Power b) => a.Watts < b.Watts;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(Power a, Power b) => a.Watts <= b.Watts;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(Power a, Power b) => a.Watts > b.Watts;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(Power a, Power b) => a.Watts >= b.Watts;

    /// <summary>Formats using engineering prefix scaling.</summary>
    public override string ToString() => ToQuantity().FormatEngineering();
}
