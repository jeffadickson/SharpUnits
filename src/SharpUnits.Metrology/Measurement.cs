namespace SharpUnits.Metrology;

/// <summary>Measured quantity with independent standard uncertainty (1σ) in coherent SI units.</summary>
/// <remarks>Assumes uncorrelated measurements; correlated inputs should use <see cref="UncertaintyBudget"/>.</remarks>
public readonly record struct Measurement
{
    /// <summary>Measured physical quantity.</summary>
    public Quantity Value { get; }

    /// <summary>Standard uncertainty expressed in coherent SI units.</summary>
    public double StandardUncertainty { get; }

    /// <summary>Constructs a measurement with finite nonnegative standard uncertainty.</summary>
    public Measurement(Quantity value, double standardUncertainty)
    {
        if (!double.IsFinite(standardUncertainty) || standardUncertainty < 0)
            throw new ArgumentOutOfRangeException(nameof(standardUncertainty));
        Value = value;
        StandardUncertainty = standardUncertainty;
    }

    /// <summary>Expanded uncertainty at the specified coverage factor (for example <c>k = 2</c>).</summary>
    public double ExpandedUncertainty(double coverageFactor = 2d)
    {
        if (!double.IsFinite(coverageFactor) || coverageFactor < 0)
            throw new ArgumentOutOfRangeException(nameof(coverageFactor));
        double expanded = coverageFactor * StandardUncertainty;
        if (!double.IsFinite(expanded)) throw new OverflowException("Expanded uncertainty overflow.");
        return expanded;
    }

    /// <summary>Converts the underlying reading to a target display unit while retaining coherent SI uncertainty.</summary>
    public Measurement In(UnitDefinition target) => new(Value.In(target), StandardUncertainty);

    /// <summary>Adds independent measurements of compatible dimensions.</summary>
    public static Measurement operator +(Measurement a, Measurement b) =>
        new(a.Value + b.Value, double.Hypot(a.StandardUncertainty, b.StandardUncertainty));

    /// <summary>Subtracts independent measurements of compatible dimensions.</summary>
    public static Measurement operator -(Measurement a, Measurement b) =>
        new(a.Value - b.Value, double.Hypot(a.StandardUncertainty, b.StandardUncertainty));

    /// <summary>Multiplies independent measurements using first-order absolute uncertainty propagation.</summary>
    public static Measurement operator *(Measurement a, Measurement b)
    {
        double u = double.Hypot(b.Value.SiValue * a.StandardUncertainty, a.Value.SiValue * b.StandardUncertainty);
        return new(a.Value * b.Value, u);
    }

    /// <summary>Divides independent measurements using first-order uncertainty propagation.</summary>
    public static Measurement operator /(Measurement a, Measurement b)
    {
        if (b.Value.SiValue == 0d) throw new DivideByZeroException("Cannot divide by a zero-valued measurement.");
        double b2 = b.Value.SiValue * b.Value.SiValue;
        double u = double.Hypot(a.StandardUncertainty / b.Value.SiValue, a.Value.SiValue * b.StandardUncertainty / b2);
        return new(a.Value / b.Value, u);
    }

    /// <summary>Scales a measurement by an exact scalar constant.</summary>
    public static Measurement operator *(Measurement a, double scalar) =>
        new(a.Value * scalar, Math.Abs(scalar) * a.StandardUncertainty);

    /// <summary>Scales a measurement by an exact scalar constant.</summary>
    public static Measurement operator *(double scalar, Measurement a) => a * scalar;

    /// <summary>Divides a measurement by an exact scalar constant.</summary>
    public static Measurement operator /(Measurement a, double divisor)
    {
        if (divisor == 0d) throw new DivideByZeroException();
        return new(a.Value / divisor, a.StandardUncertainty / Math.Abs(divisor));
    }
}
