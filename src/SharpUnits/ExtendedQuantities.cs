using System.Globalization;

namespace SharpUnits;

/// <summary>Strongly-typed area measured in square meters.</summary>
public readonly record struct Area(double SquareMeters) : IComparable<Area>
{
    /// <summary>Value in square centimeters.</summary>
    public double SquareCentimeters => SquareMeters * 1e4;

    /// <summary>Value in square millimeters.</summary>
    public double SquareMillimeters => SquareMeters * 1e6;

    /// <summary>Value in square inches.</summary>
    public double SquareInches => SquareMeters / (0.0254d * 0.0254d);

    /// <summary>Value in square feet.</summary>
    public double SquareFeet => SquareMeters / (0.3048d * 0.3048d);

    /// <summary>Creates area from square meters.</summary>
    public static Area FromSquareMeters(double value) => new(value);

    /// <summary>Creates area from square millimeters.</summary>
    public static Area FromSquareMillimeters(double value) => new(value * 1e-6);

    /// <summary>Creates area from square centimeters.</summary>
    public static Area FromSquareCentimeters(double value) => new(value * 1e-4);

    /// <summary>Creates area from square inches.</summary>
    public static Area FromSquareInches(double value) => new(value * 0.0254d * 0.0254d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => new(SquareMeters, RationalDimension.Area, Units.Meter.Pow(2).ToUnitDefinition(), QuantityScale.Absolute, null, Isq.Area);

    /// <summary>Adds two areas.</summary>
    public static Area operator +(Area a, Area b) => new(a.SquareMeters + b.SquareMeters);

    /// <summary>Subtracts two areas.</summary>
    public static Area operator -(Area a, Area b) => new(a.SquareMeters - b.SquareMeters);

    /// <summary>Area divided by length yields length.</summary>
    public static Length operator /(Area a, Length l) => new(a.SquareMeters / l.Meters);

    /// <summary>Area times length yields volume.</summary>
    public static Volume operator *(Area a, Length l) => new(a.SquareMeters * l.Meters);

    /// <summary>Scales area.</summary>
    public static Area operator *(Area a, double scalar) => new(a.SquareMeters * scalar);

    /// <summary>Compares areas.</summary>
    public int CompareTo(Area other) => SquareMeters.CompareTo(other.SquareMeters);

    /// <inheritdoc/>
    public override string ToString() => $"{SquareMeters.ToString("G4", CultureInfo.InvariantCulture)} m²";
}

/// <summary>Strongly-typed volume measured in cubic meters.</summary>
public readonly record struct Volume(double CubicMeters) : IComparable<Volume>
{
    /// <summary>Value in liters.</summary>
    public double Liters => CubicMeters * 1e3;

    /// <summary>Value in milliliters.</summary>
    public double Milliliters => CubicMeters * 1e6;

    /// <summary>Value in US liquid gallons.</summary>
    public double UsGallons => CubicMeters / 3.785411784e-3d;

    /// <summary>Creates volume from cubic meters.</summary>
    public static Volume FromCubicMeters(double value) => new(value);

    /// <summary>Creates volume from liters.</summary>
    public static Volume FromLiters(double value) => new(value * 1e-3);

    /// <summary>Creates volume from milliliters.</summary>
    public static Volume FromMilliliters(double value) => new(value * 1e-6);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => new(CubicMeters, RationalDimension.Volume, Units.Meter.Pow(3).ToUnitDefinition(), QuantityScale.Absolute, null, Isq.Volume);

    /// <summary>Adds two volumes.</summary>
    public static Volume operator +(Volume a, Volume b) => new(a.CubicMeters + b.CubicMeters);

    /// <summary>Subtracts two volumes.</summary>
    public static Volume operator -(Volume a, Volume b) => new(a.CubicMeters - b.CubicMeters);

    /// <summary>Volume divided by area yields length.</summary>
    public static Length operator /(Volume v, Area a) => new(v.CubicMeters / a.SquareMeters);

    /// <summary>Compares volumes.</summary>
    public int CompareTo(Volume other) => CubicMeters.CompareTo(other.CubicMeters);

    /// <inheritdoc/>
    public override string ToString() => $"{CubicMeters.ToString("G4", CultureInfo.InvariantCulture)} m³";
}

/// <summary>Strongly-typed linear velocity/speed measured in meters per second.</summary>
public readonly record struct Velocity(double MetersPerSecond) : IComparable<Velocity>
{
    /// <summary>Value in kilometers per hour.</summary>
    public double KilometersPerHour => MetersPerSecond * 3.6d;

    /// <summary>Value in miles per hour.</summary>
    public double MilesPerHour => MetersPerSecond / 0.44704d;

    /// <summary>Value in knots.</summary>
    public double Knots => MetersPerSecond / (1852d / 3600d);

    /// <summary>Creates velocity from meters per second.</summary>
    public static Velocity FromMetersPerSecond(double value) => new(value);

    /// <summary>Creates velocity from kilometers per hour.</summary>
    public static Velocity FromKilometersPerHour(double value) => new(value / 3.6d);

    /// <summary>Creates velocity from miles per hour.</summary>
    public static Velocity FromMilesPerHour(double value) => new(value * 0.44704d);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(MetersPerSecond, Units.Meter / Units.Second, spec: Isq.Velocity);

    /// <summary>Adds two velocities.</summary>
    public static Velocity operator +(Velocity a, Velocity b) => new(a.MetersPerSecond + b.MetersPerSecond);

    /// <summary>Subtracts two velocities.</summary>
    public static Velocity operator -(Velocity a, Velocity b) => new(a.MetersPerSecond - b.MetersPerSecond);

    /// <summary>Velocity times duration yields length.</summary>
    public static Length operator *(Velocity v, Duration t) => new(v.MetersPerSecond * t.Seconds);

    /// <summary>Velocity divided by duration yields acceleration.</summary>
    public static Acceleration operator /(Velocity v, Duration t) => new(v.MetersPerSecond / t.Seconds);

    /// <summary>Compares velocities.</summary>
    public int CompareTo(Velocity other) => MetersPerSecond.CompareTo(other.MetersPerSecond);

    /// <inheritdoc/>
    public override string ToString() => $"{MetersPerSecond.ToString("G4", CultureInfo.InvariantCulture)} m/s";
}

/// <summary>Strongly-typed acceleration measured in meters per second squared.</summary>
public readonly record struct Acceleration(double MetersPerSecondSquared) : IComparable<Acceleration>
{
    /// <summary>Standard gravity constant (<c>9.80665 m/s²</c>).</summary>
    public const double StandardGravityMetersPerSecondSquared = 9.80665d;

    /// <summary>Value in standard gravities (<c>g₀</c>).</summary>
    public double StandardGravities => MetersPerSecondSquared / StandardGravityMetersPerSecondSquared;

    /// <summary>Creates acceleration from m/s².</summary>
    public static Acceleration FromMetersPerSecondSquared(double value) => new(value);

    /// <summary>Creates acceleration from multiples of standard gravity (<c>g₀</c>).</summary>
    public static Acceleration FromStandardGravities(double g) => new(g * StandardGravityMetersPerSecondSquared);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(MetersPerSecondSquared, Units.Meter / Units.Second.Pow(2), spec: Isq.Acceleration);

    /// <summary>Adds two accelerations.</summary>
    public static Acceleration operator +(Acceleration a, Acceleration b) => new(a.MetersPerSecondSquared + b.MetersPerSecondSquared);

    /// <summary>Subtracts two accelerations.</summary>
    public static Acceleration operator -(Acceleration a, Acceleration b) => new(a.MetersPerSecondSquared - b.MetersPerSecondSquared);

    /// <summary>Acceleration times duration yields velocity.</summary>
    public static Velocity operator *(Acceleration a, Duration t) => new(a.MetersPerSecondSquared * t.Seconds);

    /// <summary>Compares accelerations.</summary>
    public int CompareTo(Acceleration other) => MetersPerSecondSquared.CompareTo(other.MetersPerSecondSquared);

    /// <inheritdoc/>
    public override string ToString() => $"{MetersPerSecondSquared.ToString("G4", CultureInfo.InvariantCulture)} m/s²";
}

/// <summary>Strongly-typed plane angle measured in radians.</summary>
public readonly record struct Angle(double Radians) : IComparable<Angle>
{
    /// <summary>Value in degrees.</summary>
    public double Degrees => Radians * (180d / Math.PI);

    /// <summary>Value in full revolutions.</summary>
    public double Revolutions => Radians / (2d * Math.PI);

    /// <summary>Creates an angle from radians.</summary>
    public static Angle FromRadians(double value) => new(value);

    /// <summary>Creates an angle from degrees.</summary>
    public static Angle FromDegrees(double value) => new(value * (Math.PI / 180d));

    /// <summary>Creates an angle from full revolutions.</summary>
    public static Angle FromRevolutions(double value) => new(value * 2d * Math.PI);

    /// <summary>Sine of this angle.</summary>
    public double Sin() => Math.Sin(Radians);

    /// <summary>Cosine of this angle.</summary>
    public double Cos() => Math.Cos(Radians);

    /// <summary>Tangent of this angle.</summary>
    public double Tan() => Math.Tan(Radians);

    /// <summary>Converts to a unified <see cref="Quantity"/> tagged with <see cref="Isq.AngularMeasure"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Radians, Units.Radian, spec: Isq.AngularMeasure);

    /// <summary>Adds two angles.</summary>
    public static Angle operator +(Angle a, Angle b) => new(a.Radians + b.Radians);

    /// <summary>Subtracts two angles.</summary>
    public static Angle operator -(Angle a, Angle b) => new(a.Radians - b.Radians);

    /// <summary>Negates an angle.</summary>
    public static Angle operator -(Angle a) => new(-a.Radians);

    /// <summary>Scales an angle.</summary>
    public static Angle operator *(Angle a, double scalar) => new(a.Radians * scalar);

    /// <summary>Compares angles.</summary>
    public int CompareTo(Angle other) => Radians.CompareTo(other.Radians);

    /// <inheritdoc/>
    public override string ToString() => $"{Degrees.ToString("G4", CultureInfo.InvariantCulture)}°";
}

/// <summary>Strongly-typed torque (moment of force) measured in newton-meters — intentionally distinct from <see cref="Energy"/>.</summary>
public readonly record struct Torque(double NewtonMeters) : IComparable<Torque>
{
    /// <summary>Value in pound-force feet (<c>lbf·ft</c>).</summary>
    public double PoundFeet => NewtonMeters / 1.3558179483314004d;

    /// <summary>Creates torque from newton-meters.</summary>
    public static Torque FromNewtonMeters(double value) => new(value);

    /// <summary>Creates torque from pound-force feet.</summary>
    public static Torque FromPoundFeet(double value) => new(value * 1.3558179483314004d);

    /// <summary>Converts to a unified <see cref="Quantity"/> tagged with <see cref="Isq.Torque"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(NewtonMeters, Units.NewtonMeter, spec: Isq.Torque);

    /// <summary>Adds two torques.</summary>
    public static Torque operator +(Torque a, Torque b) => new(a.NewtonMeters + b.NewtonMeters);

    /// <summary>Subtracts two torques.</summary>
    public static Torque operator -(Torque a, Torque b) => new(a.NewtonMeters - b.NewtonMeters);

    /// <summary>Scales torque.</summary>
    public static Torque operator *(Torque a, double scalar) => new(a.NewtonMeters * scalar);

    /// <summary>Compares torques.</summary>
    public int CompareTo(Torque other) => NewtonMeters.CompareTo(other.NewtonMeters);

    /// <inheritdoc/>
    public override string ToString() => $"{NewtonMeters.ToString("G4", CultureInfo.InvariantCulture)} N·m";
}

/// <summary>Strongly-typed mass density measured in kilograms per cubic meter.</summary>
public readonly record struct Density(double KilogramsPerCubicMeter) : IComparable<Density>
{
    /// <summary>Value in grams per cubic centimeter (<c>g/cm³</c>).</summary>
    public double GramsPerCubicCentimeter => KilogramsPerCubicMeter * 1e-3;

    /// <summary>Creates density from kg/m³.</summary>
    public static Density FromKilogramsPerCubicMeter(double value) => new(value);

    /// <summary>Creates density from g/cm³.</summary>
    public static Density FromGramsPerCubicCentimeter(double value) => new(value * 1e3);

    /// <summary>Density times volume yields mass.</summary>
    public static Mass operator *(Density d, Volume v) => new(d.KilogramsPerCubicMeter * v.CubicMeters);

    /// <summary>Compares densities.</summary>
    public int CompareTo(Density other) => KilogramsPerCubicMeter.CompareTo(other.KilogramsPerCubicMeter);

    /// <inheritdoc/>
    public override string ToString() => $"{KilogramsPerCubicMeter.ToString("G4", CultureInfo.InvariantCulture)} kg/m³";
}

/// <summary>Strongly-typed magnetic flux density measured in tesla.</summary>
public readonly record struct MagneticFluxDensity(double Tesla) : IComparable<MagneticFluxDensity>
{
    /// <summary>Value in millitesla.</summary>
    public double Millitesla => Tesla * 1e3;

    /// <summary>Value in gauss (<c>1 T = 10,000 G</c>).</summary>
    public double Gauss => Tesla * 1e4;

    /// <summary>Creates flux density from tesla.</summary>
    public static MagneticFluxDensity FromTesla(double value) => new(value);

    /// <summary>Creates flux density from millitesla.</summary>
    public static MagneticFluxDensity FromMillitesla(double value) => new(value * 1e-3);

    /// <summary>Creates flux density from gauss.</summary>
    public static MagneticFluxDensity FromGauss(double value) => new(value * 1e-4);

    /// <summary>Converts to a unified <see cref="Quantity"/>.</summary>
    public Quantity ToQuantity() => Quantity.Of(Tesla, Units.Tesla);

    /// <summary>Adds two flux densities.</summary>
    public static MagneticFluxDensity operator +(MagneticFluxDensity a, MagneticFluxDensity b) => new(a.Tesla + b.Tesla);

    /// <summary>Subtracts two flux densities.</summary>
    public static MagneticFluxDensity operator -(MagneticFluxDensity a, MagneticFluxDensity b) => new(a.Tesla - b.Tesla);

    /// <summary>Compares flux densities.</summary>
    public int CompareTo(MagneticFluxDensity other) => Tesla.CompareTo(other.Tesla);

    /// <inheritdoc/>
    public override string ToString() => ToQuantity().FormatEngineering();
}

/// <summary>Digital information size measured in bytes (supporting decimal SI and binary IEC prefixes for DAQ/instrument streaming).</summary>
public readonly record struct DataSize(double Bytes) : IComparable<DataSize>
{
    /// <summary>Value in bits.</summary>
    public double Bits => Bytes * 8d;

    /// <summary>Value in kibibytes (<c>1024 B</c>).</summary>
    public double Kibibytes => Bytes / 1024d;

    /// <summary>Value in mebibytes (<c>1024² B</c>).</summary>
    public double Mebibytes => Bytes / (1024d * 1024d);

    /// <summary>Value in gibibytes (<c>1024³ B</c>).</summary>
    public double Gibibytes => Bytes / (1024d * 1024d * 1024d);

    /// <summary>Value in decimal megabytes (<c>10⁶ B</c>).</summary>
    public double Megabytes => Bytes * 1e-6;

    /// <summary>Creates data size from bytes.</summary>
    public static DataSize FromBytes(double value) => new(value);

    /// <summary>Creates data size from bits.</summary>
    public static DataSize FromBits(double value) => new(value / 8d);

    /// <summary>Creates data size from kibibytes (<c>KiB</c>).</summary>
    public static DataSize FromKibibytes(double value) => new(value * 1024d);

    /// <summary>Creates data size from mebibytes (<c>MiB</c>).</summary>
    public static DataSize FromMebibytes(double value) => new(value * 1024d * 1024d);

    /// <summary>Creates data size from gibibytes (<c>GiB</c>).</summary>
    public static DataSize FromGibibytes(double value) => new(value * 1024d * 1024d * 1024d);

    /// <summary>Adds two data sizes.</summary>
    public static DataSize operator +(DataSize a, DataSize b) => new(a.Bytes + b.Bytes);

    /// <summary>Subtracts two data sizes.</summary>
    public static DataSize operator -(DataSize a, DataSize b) => new(a.Bytes - b.Bytes);

    /// <summary>Data size divided by duration yields data transfer bit rate.</summary>
    public static BitRate operator /(DataSize size, Duration duration) => new(size.Bits / duration.Seconds);

    /// <summary>Compares data sizes.</summary>
    public int CompareTo(DataSize other) => Bytes.CompareTo(other.Bytes);

    /// <inheritdoc/>
    public override string ToString() => Bytes switch
    {
        >= 1024d * 1024d * 1024d => $"{Gibibytes.ToString("G4", CultureInfo.InvariantCulture)} GiB",
        >= 1024d * 1024d => $"{Mebibytes.ToString("G4", CultureInfo.InvariantCulture)} MiB",
        >= 1024d => $"{Kibibytes.ToString("G4", CultureInfo.InvariantCulture)} KiB",
        _ => $"{Bytes.ToString("G4", CultureInfo.InvariantCulture)} B"
    };
}

/// <summary>Digital throughput / sampling transfer rate measured in bits per second.</summary>
public readonly record struct BitRate(double BitsPerSecond) : IComparable<BitRate>
{
    /// <summary>Value in kilobits per second (<c>kbps</c>).</summary>
    public double KilobitsPerSecond => BitsPerSecond * 1e-3;

    /// <summary>Value in megabits per second (<c>Mbps</c>).</summary>
    public double MegabitsPerSecond => BitsPerSecond * 1e-6;

    /// <summary>Value in gigabits per second (<c>Gbps</c>).</summary>
    public double GigabitsPerSecond => BitsPerSecond * 1e-9;

    /// <summary>Value in bytes per second (<c>B/s</c>).</summary>
    public double BytesPerSecond => BitsPerSecond / 8d;

    /// <summary>Value in megabytes per second (<c>MB/s</c>).</summary>
    public double MegabytesPerSecond => BitsPerSecond / 8e6;

    /// <summary>Creates bit rate from bits per second.</summary>
    public static BitRate FromBitsPerSecond(double value) => new(value);

    /// <summary>Creates bit rate from megabits per second.</summary>
    public static BitRate FromMegabitsPerSecond(double value) => new(value * 1e6);

    /// <summary>Creates bit rate from megabytes per second.</summary>
    public static BitRate FromMegabytesPerSecond(double value) => new(value * 8e6);

    /// <summary>Bit rate integrated over duration yields transferred <see cref="DataSize"/>.</summary>
    public static DataSize operator *(BitRate rate, Duration duration) => DataSize.FromBits(rate.BitsPerSecond * duration.Seconds);

    /// <summary>Duration times bit rate yields transferred <see cref="DataSize"/>.</summary>
    public static DataSize operator *(Duration duration, BitRate rate) => rate * duration;

    /// <summary>Compares bit rates.</summary>
    public int CompareTo(BitRate other) => BitsPerSecond.CompareTo(other.BitsPerSecond);

    /// <inheritdoc/>
    public override string ToString() => BitsPerSecond switch
    {
        >= 1e9 => $"{GigabitsPerSecond.ToString("G4", CultureInfo.InvariantCulture)} Gbps",
        >= 1e6 => $"{MegabitsPerSecond.ToString("G4", CultureInfo.InvariantCulture)} Mbps",
        >= 1e3 => $"{KilobitsPerSecond.ToString("G4", CultureInfo.InvariantCulture)} kbps",
        _ => $"{BitsPerSecond.ToString("G4", CultureInfo.InvariantCulture)} bps"
    };
}
