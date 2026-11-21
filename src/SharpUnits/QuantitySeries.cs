using System.Numerics;

namespace SharpUnits;

/// <summary>
/// Structure-of-Arrays (SoA) unit-tagged sample buffer pairing a single <see cref="SymbolicUnit"/>, optional ISO 80000
/// <see cref="QuantitySpec"/>, and optional affine <see cref="PointOrigin"/> with a contiguous <typeparamref name="T"/> array.
/// </summary>
/// <typeparam name="T">Numeric representation type (for example <see cref="float"/> for GPU plotting, <see cref="double"/> for scientific sweeps, <see cref="decimal"/> for metrology logs).</typeparam>
/// <remarks>
/// Eliminates per-sample unit metadata overhead while preserving full dimensional, ISQ, and affine safety across waveforms,
/// frequency sweeps, and instrument data channels.
/// </remarks>
public sealed class QuantitySeries<T> where T : INumber<T>
{
    private readonly T[] data;

    /// <summary>Contiguous sample values expressed in <see cref="Unit"/>.</summary>
    public ReadOnlySpan<T> Values => data;

    /// <summary>Read-only memory view over the underlying contiguous buffer.</summary>
    public ReadOnlyMemory<T> Memory => data;

    /// <summary>Number of samples in the series.</summary>
    public int Count => data.Length;

    /// <summary>Shared symbolic unit of all samples.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Optional ISO 80000 ISQ specification shared by all samples.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Optional affine point origin (non-null for temperature logs, gauge pressure traces, etc.).</summary>
    public PointOrigin? Origin { get; }

    /// <summary>Canonical exact rational dimension of all samples.</summary>
    public RationalDimension Dimension => Unit.Dimension;

    /// <summary>Returns the sample at <paramref name="index"/> as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> this[int index] => new(data[index], Unit, Spec);

    /// <summary>Returns the sample at <paramref name="index"/> as an affine <see cref="QuantityPoint{T}"/>.</summary>
    public QuantityPoint<T> PointAt(int index)
    {
        if (Origin is null)
            throw new InvalidOperationException("Series does not have an affine PointOrigin.");
        return new(new Quantity<T>(data[index], Unit, Spec), Origin);
    }

    private QuantitySeries(T[] buffer, SymbolicUnit unit, QuantitySpec? spec, PointOrigin? origin)
    {
        data = buffer;
        Unit = unit;
        Spec = spec;
        Origin = origin;
    }

    /// <summary>Creates a series by copying <paramref name="samples"/> into an immutable unit-tagged buffer.</summary>
    public static QuantitySeries<T> FromValues(
        ReadOnlySpan<T> samples,
        SymbolicUnit unit,
        QuantitySpec? spec = null,
        PointOrigin? origin = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (unit.IsAffine)
            throw new InvalidOperationException("Use a linear unit paired with a PointOrigin (for example Units.Kelvin + PointOrigins.IcePoint) for affine series.");
        var resolvedSpec = spec ?? origin?.Spec ?? unit.DefaultSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Specification '{resolvedSpec.Id}' dimension disagrees with unit '{unit.Symbol}'.");
        if (origin is not null && origin.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Origin '{origin.Name}' dimension disagrees with unit '{unit.Symbol}'.");
        return new(samples.ToArray(), unit, resolvedSpec, origin);
    }

    /// <summary>Creates a temperature series in degrees Celsius (anchored to <see cref="PointOrigins.IcePoint"/> without offset drift).</summary>
    public static QuantitySeries<T> FromCelsius(ReadOnlySpan<T> celsiusSamples) =>
        FromValues(celsiusSamples, Units.Kelvin, Isq.ThermodynamicTemperature, PointOrigins.IcePoint);

    /// <summary>Creates a gauge pressure series anchored to <see cref="PointOrigins.StandardAtmosphere"/>.</summary>
    public static QuantitySeries<T> FromGaugePressure(ReadOnlySpan<T> samples, SymbolicUnit pressureUnit) =>
        FromValues(samples, pressureUnit, Isq.Pressure, PointOrigins.StandardAtmosphere);

    /// <summary>Converts all samples to a compatible target unit in a single tight pass.</summary>
    public QuantitySeries<T> In(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine)
            throw new InvalidOperationException("Target unit must be linear.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        if (Unit.Equals(target) || Unit.ScaleToSi == target.ScaleToSi)
            return new(data.ToArray(), target, Spec, Origin);

        var output = new T[data.Length];
        ConvertSpan(data, output, Unit.ScaleToSi, target.ScaleToSi);
        return new(output, target, Spec, Origin);
    }

    /// <summary>Re-anchors an affine series to a different compatible <see cref="PointOrigin"/> (for example gauge to absolute pressure, or °C to K).</summary>
    public QuantitySeries<T> RelativeTo(PointOrigin newOrigin)
    {
        ArgumentNullException.ThrowIfNull(newOrigin);
        if (Origin is null)
            throw new InvalidOperationException("Cannot re-anchor a non-affine displacement series.");
        if (newOrigin.Dimension != Origin.Dimension)
            throw new InvalidOperationException($"Incompatible origin dimensions: '{Origin.Name}' vs '{newOrigin.Name}'.");
        if (Origin == newOrigin)
            return new(data.ToArray(), Unit, Spec, Origin);

        double deltaOriginSi = Origin.OffsetFromBaseSi - newOrigin.OffsetFromBaseSi;
        T shift = Quantity<T>.ConvertScale(
            typeof(T) == typeof(decimal) ? (T)(object)decimal.CreateChecked(deltaOriginSi) : T.CreateChecked(deltaOriginSi),
            1d,
            Unit.ScaleToSi,
            allowTruncation: false);

        var output = new T[data.Length];
        for (int i = 0; i < data.Length; i++) output[i] = data[i] + shift;
        return new(output, Unit, newOrigin.Spec ?? Spec, newOrigin);
    }

    /// <summary>Automatically selects the best shared engineering SI prefix for this series and returns the scaled series.</summary>
    public QuantitySeries<T> AutoScale()
    {
        if (Origin is not null && Origin.OffsetFromBaseSi != 0d)
            throw new InvalidOperationException("Auto-scaling SI prefixes applies to zero-origin linear series.");
        var baseUnit = ResolvePrefixableBase();
        if (baseUnit is null) return this;

        double peakSi = 0d;
        double scaleToSi = Unit.ScaleToSi;
        for (int i = 0; i < data.Length; i++)
        {
            double v = Math.Abs(double.CreateChecked(data[i]) * scaleToSi);
            if (double.IsFinite(v) && v > peakSi) peakSi = v;
        }
        var selection = UnitAutoScaler.SelectForSiMagnitude(peakSi, baseUnit);
        return In(SymbolicUnit.From(selection.Unit));
    }

    /// <summary>Converts the underlying numeric representation in one pass (for example <c>double</c> to <c>float</c> for GPU upload).</summary>
    public QuantitySeries<TOther> As<TOther>() where TOther : INumber<TOther>
    {
        var output = new TOther[data.Length];
        for (int i = 0; i < data.Length; i++) output[i] = TOther.CreateChecked(data[i]);
        return QuantitySeries<TOther>.FromValues(output, Unit, Spec, Origin);
    }

    /// <summary>Returns minimum and maximum sample values as unit-safe <see cref="Quantity{T}"/>.</summary>
    public (Quantity<T> Min, Quantity<T> Max) MinMax()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        T min = data[0], max = data[0];
        for (int i = 1; i < data.Length; i++)
        {
            if (data[i] < min) min = data[i];
            if (data[i] > max) max = data[i];
        }
        return (new(min, Unit, Spec), new(max, Unit, Spec));
    }

    /// <summary>Returns the peak-to-peak span (<c>Max - Min</c>) of the series.</summary>
    public Quantity<T> PeakToPeak()
    {
        var (min, max) = MinMax();
        return new(max.Value - min.Value, Unit, Spec);
    }

    /// <summary>Returns the arithmetic mean of the series.</summary>
    public Quantity<T> Mean()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        T sum = T.Zero;
        for (int i = 0; i < data.Length; i++) sum += data[i];
        return new(sum / T.CreateChecked(data.Length), Unit, Spec);
    }

    /// <summary>Returns the root-mean-square (RMS) magnitude in double precision (for AC waveforms and noise measurements).</summary>
    public Quantity<double> Rms()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        if (Origin is not null && Origin.OffsetFromBaseSi != 0d)
            throw new InvalidOperationException("RMS is undefined for offset-origin affine point series.");
        double sumSq = 0d;
        for (int i = 0; i < data.Length; i++)
        {
            double v = double.CreateChecked(data[i]);
            sumSq += v * v;
        }
        return new(Math.Sqrt(sumSq / data.Length), Unit, Spec);
    }

    /// <summary>Element-wise addition of compatible linear series.</summary>
    public static QuantitySeries<T> operator +(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        var (commonUnit, commonSpec) = PrepareBinaryAdditive(a, b, isSubtraction: false, out var resultOrigin);
        var aAligned = a.Unit.Equals(commonUnit) ? a.data : a.In(commonUnit).data;
        var bAligned = b.Unit.Equals(commonUnit) ? b.data : b.In(commonUnit).data;
        var output = new T[a.Count];
        for (int i = 0; i < output.Length; i++) output[i] = aAligned[i] + bAligned[i];
        return new(output, commonUnit, commonSpec, resultOrigin);
    }

    /// <summary>Element-wise subtraction of compatible series (subtracting two affine point series yields a displacement series).</summary>
    public static QuantitySeries<T> operator -(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        var (commonUnit, commonSpec) = PrepareBinaryAdditive(a, b, isSubtraction: true, out var resultOrigin);
        var bReanchored = (a.Origin is not null && b.Origin is not null && a.Origin != b.Origin)
            ? b.RelativeTo(a.Origin)
            : b;
        var aAligned = a.Unit.Equals(commonUnit) ? a.data : a.In(commonUnit).data;
        var bAligned = bReanchored.Unit.Equals(commonUnit) ? bReanchored.data : bReanchored.In(commonUnit).data;
        var output = new T[a.Count];
        for (int i = 0; i < output.Length; i++) output[i] = aAligned[i] - bAligned[i];
        return new(output, commonUnit, commonSpec, resultOrigin);
    }

    /// <summary>Element-wise multiplication of two linear series, preserving algebraic symbolic units (for example <c>V · A</c>).</summary>
    public static QuantitySeries<T> operator *(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        RequireLinearSeries(a);
        RequireLinearSeries(b);
        RequireSameLength(a, b);
        var output = new T[a.Count];
        for (int i = 0; i < output.Length; i++) output[i] = a.data[i] * b.data[i];
        return new(output, a.Unit * b.Unit, null, null);
    }

    /// <summary>Element-wise division of two linear series, preserving algebraic symbolic units (for example <c>V / A</c>).</summary>
    public static QuantitySeries<T> operator /(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        RequireLinearSeries(a);
        RequireLinearSeries(b);
        RequireSameLength(a, b);
        var output = new T[a.Count];
        for (int i = 0; i < output.Length; i++) output[i] = a.data[i] / b.data[i];
        return new(output, a.Unit / b.Unit, null, null);
    }

    /// <summary>Scales a linear series by a scalar factor.</summary>
    public static QuantitySeries<T> operator *(QuantitySeries<T> series, T scalar)
    {
        RequireLinearSeries(series);
        var output = new T[series.Count];
        for (int i = 0; i < output.Length; i++) output[i] = series.data[i] * scalar;
        return new(output, series.Unit, series.Spec, null);
    }

    /// <summary>Scales a linear series by a scalar factor.</summary>
    public static QuantitySeries<T> operator *(T scalar, QuantitySeries<T> series) => series * scalar;

    /// <summary>Divides a linear series by a scalar divisor.</summary>
    public static QuantitySeries<T> operator /(QuantitySeries<T> series, T divisor)
    {
        RequireLinearSeries(series);
        var output = new T[series.Count];
        for (int i = 0; i < output.Length; i++) output[i] = series.data[i] / divisor;
        return new(output, series.Unit, series.Spec, null);
    }

    private UnitDefinition? ResolvePrefixableBase()
    {
        if (Unit.Factors.Count == 1 && Unit.Factors[0].Exponent.IsOne)
        {
            var u = Unit.Factors[0].Unit;
            if (u.AllowPrefixes || u == Units.Kilogram) return u;
        }
        if (Units.TryGetCoherentSiUnit(Dimension, out var coherent) && coherent is not null)
        {
            if (coherent.AllowPrefixes || coherent == Units.Kilogram) return coherent;
        }
        return null;
    }

    private static (SymbolicUnit CommonUnit, QuantitySpec? CommonSpec) PrepareBinaryAdditive(
        QuantitySeries<T> a,
        QuantitySeries<T> b,
        bool isSubtraction,
        out PointOrigin? resultOrigin)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        RequireSameLength(a, b);
        if (a.Dimension != b.Dimension)
            throw new InvalidOperationException($"Incompatible series dimensions: '{a.Unit.Symbol}' vs '{b.Unit.Symbol}'.");

        QuantitySpec? commonSpec = null;
        if (a.Spec is not null || b.Spec is not null)
        {
            commonSpec = QuantitySpec.CommonSpec(a.Spec, b.Spec);
            if (commonSpec is null)
                throw new InvalidOperationException($"Incompatible ISQ series kinds: '{a.Spec?.Id}' vs '{b.Spec?.Id}'.");
        }

        if (!isSubtraction)
        {
            if (a.Origin is not null && b.Origin is not null)
                throw new InvalidOperationException("Two affine point series cannot be added.");
            resultOrigin = a.Origin ?? b.Origin;
        }
        else
        {
            if (a.Origin is not null && b.Origin is not null) resultOrigin = null; // point - point = displacement
            else if (a.Origin is not null && b.Origin is null) resultOrigin = a.Origin;
            else if (a.Origin is null && b.Origin is not null)
                throw new InvalidOperationException("Cannot subtract an affine point series from a displacement series.");
            else resultOrigin = null;
        }

        return (SymbolicUnit.CommonUnit(a.Unit, b.Unit), commonSpec);
    }

    private static void RequireLinearSeries(QuantitySeries<T> s)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (s.Origin is not null)
            throw new InvalidOperationException("Affine point series cannot participate in linear scaling or multiplicative algebra.");
    }

    private static void RequireSameLength(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        if (a.Count != b.Count)
            throw new ArgumentException($"Series length mismatch: {a.Count} vs {b.Count}.");
    }

    private static void ConvertSpan(ReadOnlySpan<T> src, Span<T> dst, double srcScale, double dstScale)
    {
        if (typeof(T) == typeof(double))
        {
            double ratio = srcScale / dstScale;
            for (int i = 0; i < src.Length; i++)
                dst[i] = (T)(object)((double)(object)src[i] * ratio);
            return;
        }
        if (typeof(T) == typeof(float))
        {
            double ratio = srcScale / dstScale;
            for (int i = 0; i < src.Length; i++)
                dst[i] = (T)(object)(float)((float)(object)src[i] * ratio);
            return;
        }
        if (typeof(T) == typeof(decimal))
        {
            decimal dSrc = decimal.CreateChecked(srcScale);
            decimal dDst = decimal.CreateChecked(dstScale);
            for (int i = 0; i < src.Length; i++)
                dst[i] = (T)(object)(((decimal)(object)src[i] * dSrc) / dDst);
            return;
        }
        for (int i = 0; i < src.Length; i++)
            dst[i] = Quantity<T>.ConvertScale(src[i], srcScale, dstScale, allowTruncation: false);
    }
}
