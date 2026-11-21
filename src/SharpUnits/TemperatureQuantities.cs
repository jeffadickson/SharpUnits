namespace SharpUnits;

/// <summary>Absolute thermodynamic temperature in kelvin.</summary>
/// <remarks>Absolute temperatures cannot be added to other absolute temperatures in this model.</remarks>
public readonly record struct AbsoluteTemperature : IComparable<AbsoluteTemperature>
{
    /// <summary>Temperature in kelvin.</summary>
    public double Kelvin { get; }

    /// <summary>Temperature in degrees Celsius.</summary>
    public double Celsius => UnitConversion.Celsius.FromSi(Kelvin);

    /// <summary>Temperature in degrees Fahrenheit.</summary>
    public double Fahrenheit => UnitConversion.Fahrenheit.FromSi(Kelvin);

    /// <summary>Creates an absolute temperature; negative kelvin is rejected.</summary>
    public AbsoluteTemperature(double kelvin)
    {
        if (!double.IsFinite(kelvin) || kelvin < 0) throw new ArgumentOutOfRangeException(nameof(kelvin));
        Kelvin = kelvin;
    }

    /// <summary>Creates an absolute temperature from kelvin.</summary>
    public static AbsoluteTemperature FromKelvin(double kelvin) => new(kelvin);

    /// <summary>Creates an absolute temperature from degrees Celsius.</summary>
    public static AbsoluteTemperature FromCelsius(double celsius) => new(UnitConversion.Celsius.ToSi(celsius));

    /// <summary>Creates an absolute temperature from degrees Fahrenheit.</summary>
    public static AbsoluteTemperature FromFahrenheit(double fahrenheit) => new(UnitConversion.Fahrenheit.ToSi(fahrenheit));

    /// <summary>Subtracts absolute temperatures to obtain a temperature difference.</summary>
    public static TemperatureDifference operator -(AbsoluteTemperature a, AbsoluteTemperature b) => new(a.Kelvin - b.Kelvin);

    /// <summary>Adds a temperature difference to an absolute temperature.</summary>
    public static AbsoluteTemperature operator +(AbsoluteTemperature a, TemperatureDifference delta) => new(a.Kelvin + delta.Kelvin);

    /// <summary>Subtracts a temperature difference from an absolute temperature.</summary>
    public static AbsoluteTemperature operator -(AbsoluteTemperature a, TemperatureDifference delta) => new(a.Kelvin - delta.Kelvin);

    /// <summary>Compares absolute temperatures.</summary>
    public int CompareTo(AbsoluteTemperature other) => Kelvin.CompareTo(other.Kelvin);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(AbsoluteTemperature a, AbsoluteTemperature b) => a.Kelvin < b.Kelvin;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(AbsoluteTemperature a, AbsoluteTemperature b) => a.Kelvin <= b.Kelvin;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(AbsoluteTemperature a, AbsoluteTemperature b) => a.Kelvin > b.Kelvin;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(AbsoluteTemperature a, AbsoluteTemperature b) => a.Kelvin >= b.Kelvin;

    /// <summary>Formats in kelvin.</summary>
    public override string ToString() => Quantity.Of(Kelvin, Units.Kelvin).Format();
}

/// <summary>Signed temperature difference expressed in kelvin increments.</summary>
public readonly record struct TemperatureDifference(double Kelvin) : IComparable<TemperatureDifference>
{
    /// <summary>Difference in Celsius degrees (identical in magnitude to kelvin).</summary>
    public double CelsiusDelta => Kelvin;

    /// <summary>Difference in Fahrenheit degrees.</summary>
    public double FahrenheitDelta => Kelvin * 9d / 5d;

    /// <summary>Creates a temperature difference from Celsius/kelvin degrees.</summary>
    public static TemperatureDifference FromCelsiusDelta(double deltaC) => new(deltaC);

    /// <summary>Creates a temperature difference from Fahrenheit degrees.</summary>
    public static TemperatureDifference FromFahrenheitDelta(double deltaF) => new(deltaF * 5d / 9d);

    /// <summary>Adds two temperature differences.</summary>
    public static TemperatureDifference operator +(TemperatureDifference a, TemperatureDifference b) => new(a.Kelvin + b.Kelvin);

    /// <summary>Subtracts temperature differences.</summary>
    public static TemperatureDifference operator -(TemperatureDifference a, TemperatureDifference b) => new(a.Kelvin - b.Kelvin);

    /// <summary>Negates a temperature difference.</summary>
    public static TemperatureDifference operator -(TemperatureDifference a) => new(-a.Kelvin);

    /// <summary>Scales a temperature difference.</summary>
    public static TemperatureDifference operator *(TemperatureDifference a, double scalar) => new(a.Kelvin * scalar);

    /// <summary>Scales a temperature difference.</summary>
    public static TemperatureDifference operator *(double scalar, TemperatureDifference a) => new(scalar * a.Kelvin);

    /// <summary>Divides a temperature difference by a scalar.</summary>
    public static TemperatureDifference operator /(TemperatureDifference a, double divisor) => new(a.Kelvin / divisor);

    /// <summary>Compares temperature differences.</summary>
    public int CompareTo(TemperatureDifference other) => Kelvin.CompareTo(other.Kelvin);

    /// <summary>Less-than comparison.</summary>
    public static bool operator <(TemperatureDifference a, TemperatureDifference b) => a.Kelvin < b.Kelvin;

    /// <summary>Less-than-or-equal comparison.</summary>
    public static bool operator <=(TemperatureDifference a, TemperatureDifference b) => a.Kelvin <= b.Kelvin;

    /// <summary>Greater-than comparison.</summary>
    public static bool operator >(TemperatureDifference a, TemperatureDifference b) => a.Kelvin > b.Kelvin;

    /// <summary>Greater-than-or-equal comparison.</summary>
    public static bool operator >=(TemperatureDifference a, TemperatureDifference b) => a.Kelvin >= b.Kelvin;

    /// <summary>Formats as a kelvin interval.</summary>
    public override string ToString() => Quantity.Delta(Kelvin, Units.Kelvin).Format();
}
