using System.Numerics;

namespace SharpUnits;

/// <summary>Result of selecting a shared engineering display unit for a plot axis or data series.</summary>
/// <param name="Unit">Selected prefixed or base display unit (for example <c>mV</c>, <c>kHz</c>, <c>MΩ</c>).</param>
/// <param name="ScaleFromSi">Multiplier converting a coherent SI value into <paramref name="Unit"/> (<c>displayValue = siValue * ScaleFromSi</c>).</param>
public readonly record struct AxisUnitSelection(UnitDefinition Unit, double ScaleFromSi)
{
    /// <summary>Converts a single coherent SI value into the selected display unit.</summary>
    public double FromSi(double siValue) => siValue * ScaleFromSi;

    /// <summary>Converts a span of coherent SI values into display values using hardware SIMD when available.</summary>
    public void ConvertFromSi(ReadOnlySpan<double> siValues, Span<double> destination)
    {
        if (destination.Length < siValues.Length)
            throw new ArgumentException("Destination span is shorter than source span.", nameof(destination));
        double factor = ScaleFromSi;
        int i = 0;
        if (Vector.IsHardwareAccelerated && siValues.Length >= Vector<double>.Count)
        {
            int step = Vector<double>.Count;
            int limit = siValues.Length - step;
            var fVec = new Vector<double>(factor);
            for (; i <= limit; i += step)
                (new Vector<double>(siValues.Slice(i)) * fVec).CopyTo(destination.Slice(i));
        }
        for (; i < siValues.Length; i++)
            destination[i] = siValues[i] * factor;
    }
}

/// <summary>Automatic engineering SI prefix selection for quantities, instrument readouts, and plot axes.</summary>
public static class UnitAutoScaler
{
    /// <summary>Selects the best engineering SI prefix for a single quantity so its magnitude lies in <c>[1, 1000)</c> when possible.</summary>
    public static Quantity Scale(Quantity quantity)
    {
        var baseUnit = ResolvePrefixableBase(quantity);
        if (baseUnit is null) return quantity;
        var selection = SelectForSiMagnitude(Math.Abs(quantity.SiValue), baseUnit);
        return quantity.In(selection.Unit);
    }

    /// <summary>Selects a single shared engineering unit for an entire plot axis range given coherent SI bounds.</summary>
    public static AxisUnitSelection SelectForAxis(double minSi, double maxSi, UnitDefinition baseUnit)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        if (!double.IsFinite(minSi) || !double.IsFinite(maxSi))
            throw new ArgumentOutOfRangeException(nameof(minSi), "Axis bounds must be finite.");
        double peak = Math.Max(Math.Abs(minSi), Math.Abs(maxSi));
        return SelectForSiMagnitude(peak, baseUnit);
    }

    /// <summary>Selects a single shared engineering unit for a span of coherent SI samples (for example a waveform or sweep).</summary>
    public static AxisUnitSelection SelectForSeries(ReadOnlySpan<double> siSamples, UnitDefinition baseUnit)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        double peak = 0d;
        for (int i = 0; i < siSamples.Length; i++)
        {
            double v = Math.Abs(siSamples[i]);
            if (double.IsFinite(v) && v > peak) peak = v;
        }
        return SelectForSiMagnitude(peak, baseUnit);
    }

    /// <summary>Selects the best engineering unit for a representative nonnegative SI magnitude.</summary>
    public static AxisUnitSelection SelectForSiMagnitude(double absSiValue, UnitDefinition baseUnit)
    {
        ArgumentNullException.ThrowIfNull(baseUnit);
        if (!baseUnit.Converter.IsLinear)
            throw new InvalidOperationException("Affine units cannot be auto-scaled with SI prefixes.");

        // Special case: kilograms scale through grams so 0.005 kg -> 5 g, 1e-6 kg -> 1 mg, 5 kg -> 5 kg.
        if (baseUnit == Units.Kilogram)
        {
            if (absSiValue == 0d || !double.IsFinite(absSiValue))
                return new(Units.Kilogram, 1d);
            if (absSiValue >= 1d && absSiValue < 1000d)
                return new(Units.Kilogram, 1d);
            baseUnit = Units.Gram;
        }

        if (!baseUnit.AllowPrefixes || absSiValue == 0d || !double.IsFinite(absSiValue))
            return new(baseUnit, 1d / baseUnit.Converter.Scale);

        double valueInBase = absSiValue / baseUnit.Converter.Scale;
        if (valueInBase == 0d || !double.IsFinite(valueInBase))
            return new(baseUnit, 1d / baseUnit.Converter.Scale);

        int exponent = (int)Math.Floor(Math.Log10(valueInBase) / 3d) * 3;
        exponent = Math.Clamp(exponent, -30, 30);

        if (exponent == 0)
            return new(baseUnit, 1d / baseUnit.Converter.Scale);

        var prefix = SiPrefixCatalog.Engineering.FirstOrDefault(p => p.Exponent == exponent);
        if (prefix is null)
            return new(baseUnit, 1d / baseUnit.Converter.Scale);

        var scaledUnit = prefix.Apply(baseUnit);
        return new(scaledUnit, 1d / scaledUnit.Converter.Scale);
    }

    private static UnitDefinition? ResolvePrefixableBase(Quantity quantity)
    {
        if (quantity.Unit is not null)
        {
            if (quantity.Unit.IsAffine) return null;
            if (quantity.Unit.AllowPrefixes || quantity.Unit == Units.Kilogram) return quantity.Unit;
        }
        if (Units.TryGetCoherentSiUnit(quantity.RationalDimension, out var coherent) && coherent is not null)
        {
            if (coherent.AllowPrefixes || coherent == Units.Kilogram) return coherent;
        }
        return null;
    }
}
