using System.Numerics;

namespace SharpUnits;

/// <summary>Idiomatic C# fluent extension methods for constructing quantities and affine points from numeric literals.</summary>
public static class QuantityExtensions
{
    /// <summary>Attaches a symbolic unit to any <see cref="INumber{TSelf}"/> value (for example <c>0.1m.WithUnit(Units.Millivolt)</c>).</summary>
    public static Quantity<T> WithUnit<T>(this T value, SymbolicUnit unit, QuantitySpec? spec = null)
        where T : INumber<T> => Quantity<T>.Of(value, unit, spec);

    /// <summary>Attaches a resolved unit symbol to a <see cref="double"/> value (for example <c>12.5.WithUnit("kΩ")</c>).</summary>
    public static Quantity WithUnit(this double value, string unitSymbol, UnitRegistry? registry = null) =>
        Quantity.Of(value, unitSymbol, registry);

    /// <summary>Attaches a <see cref="UnitDefinition"/> to a <see cref="double"/> value.</summary>
    public static Quantity WithUnit(this double value, UnitDefinition unit) =>
        Quantity.Of(value, unit);

    // --- Core Length ---

    /// <summary>Creates a <see cref="Length"/> in meters.</summary>
    public static Length Meters(this double value) => Length.FromMeters(value);

    /// <summary>Creates a <see cref="Length"/> in millimeters.</summary>
    public static Length Millimeters(this double value) => Length.FromMillimeters(value);

    /// <summary>Creates a <see cref="Length"/> in kilometers.</summary>
    public static Length Kilometers(this double value) => Length.FromKilometers(value);

    /// <summary>Creates a <see cref="Length"/> in inches.</summary>
    public static Length Inches(this double value) => Length.FromInches(value);

    // --- Core Mass ---

    /// <summary>Creates a <see cref="Mass"/> in kilograms.</summary>
    public static Mass Kilograms(this double value) => Mass.FromKilograms(value);

    /// <summary>Creates a <see cref="Mass"/> in grams.</summary>
    public static Mass Grams(this double value) => Mass.FromGrams(value);

    // --- Core Time & Frequency ---

    /// <summary>Creates a <see cref="Duration"/> in seconds.</summary>
    public static Duration Seconds(this double value) => Duration.FromSeconds(value);

    /// <summary>Creates a <see cref="Duration"/> in milliseconds.</summary>
    public static Duration Milliseconds(this double value) => Duration.FromMilliseconds(value);

    /// <summary>Creates a <see cref="Duration"/> in microseconds.</summary>
    public static Duration Microseconds(this double value) => Duration.FromMicroseconds(value);

    /// <summary>Creates a <see cref="Duration"/> in nanoseconds.</summary>
    public static Duration Nanoseconds(this double value) => Duration.FromNanoseconds(value);

    /// <summary>Creates a <see cref="Frequency"/> in hertz.</summary>
    public static Frequency Hertz(this double value) => Frequency.FromHertz(value);

    /// <summary>Creates a <see cref="Frequency"/> in kilohertz.</summary>
    public static Frequency Kilohertz(this double value) => Frequency.FromKilohertz(value);

    /// <summary>Creates a <see cref="Frequency"/> in megahertz.</summary>
    public static Frequency Megahertz(this double value) => Frequency.FromMegahertz(value);

    /// <summary>Creates a <see cref="Frequency"/> in gigahertz.</summary>
    public static Frequency Gigahertz(this double value) => Frequency.FromGigahertz(value);

    // --- Core Mechanics ---

    /// <summary>Creates a <see cref="Force"/> in newtons.</summary>
    public static Force Newtons(this double value) => Force.FromNewtons(value);

    /// <summary>Creates a <see cref="Pressure"/> in pascals.</summary>
    public static Pressure Pascals(this double value) => Pressure.FromPascals(value);

    /// <summary>Creates a <see cref="Pressure"/> in bar.</summary>
    public static Pressure Bar(this double value) => Pressure.FromBar(value);

    /// <summary>Creates a <see cref="Pressure"/> in psi.</summary>
    public static Pressure Psi(this double value) => Pressure.FromPsi(value);

    /// <summary>Creates an <see cref="Energy"/> in joules.</summary>
    public static Energy Joules(this double value) => Energy.FromJoules(value);

    /// <summary>Creates a <see cref="Power"/> in watts.</summary>
    public static Power Watts(this double value) => Power.FromWatts(value);

    /// <summary>Creates a <see cref="Power"/> in kilowatts.</summary>
    public static Power Kilowatts(this double value) => Power.FromKilowatts(value);

    // --- Affine Points ---

    /// <summary>Creates a Celsius temperature point anchored to <see cref="PointOrigins.IcePoint"/>.</summary>
    public static QuantityPoint<double> DegreesCelsius(this double value) => QuantityPoint<double>.FromCelsius(value);

    /// <summary>Creates a Fahrenheit temperature point anchored to <see cref="PointOrigins.FahrenheitZero"/>.</summary>
    public static QuantityPoint<double> DegreesFahrenheit(this double value) => QuantityPoint<double>.FromFahrenheit(value);

    /// <summary>Creates a kelvin temperature point anchored to <see cref="PointOrigins.AbsoluteZero"/>.</summary>
    public static QuantityPoint<double> KelvinPoint(this double value) => QuantityPoint<double>.FromKelvin(value);

    /// <summary>Creates a gauge pressure point in bar anchored to <see cref="PointOrigins.StandardAtmosphere"/>.</summary>
    public static QuantityPoint<double> BarGauge(this double value) => QuantityPoint<double>.FromGaugePressure(value, Units.Bar);

    /// <summary>Creates a gauge pressure point in psi anchored to <see cref="PointOrigins.StandardAtmosphere"/>.</summary>
    public static QuantityPoint<double> PsiGauge(this double value) => QuantityPoint<double>.FromGaugePressure(value, Units.Psi);
}
