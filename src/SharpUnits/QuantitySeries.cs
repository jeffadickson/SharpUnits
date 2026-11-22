using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpUnits;

/// <summary>
/// Structure-of-Arrays (SoA) unit-tagged sample buffer pairing a single <see cref="SymbolicUnit"/>, optional ISO 80000
/// <see cref="QuantitySpec"/>, and optional affine <see cref="PointOrigin"/> with a contiguous <typeparamref name="T"/> array.
/// </summary>
/// <typeparam name="T">Numeric representation type (for example <see cref="float"/> for GPU plotting, <see cref="double"/> for scientific sweeps, <see cref="decimal"/> for metrology logs).</typeparam>
/// <remarks>
/// Eliminates per-sample unit metadata overhead while preserving full dimensional, ISQ, and affine safety across waveforms,
/// frequency sweeps, and instrument data channels. Automatically accelerates bulk conversions, element-wise algebra, and
/// reductions using hardware SIMD (<see cref="Vector{T}"/>) when supported by <typeparamref name="T"/>.
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

    /// <summary>True when bulk operations on <typeparamref name="T"/> execute on hardware SIMD registers.</summary>
    public static bool IsSimdAccelerated => Vector.IsHardwareAccelerated && Vector<T>.IsSupported;

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

    /// <summary>Converts all samples to a compatible target unit in a single SIMD-accelerated pass.</summary>
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

    /// <summary>
    /// Converts all samples into a caller-provided <paramref name="destination"/> span in a single zero-allocation SIMD pass,
    /// ideal for writing directly into reusable plotting or GPU staging buffers.
    /// </summary>
    public void CopyConvertedTo(SymbolicUnit target, Span<T> destination)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine)
            throw new InvalidOperationException("Target unit must be linear.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        if (destination.Length < data.Length)
            throw new ArgumentException($"Destination span length ({destination.Length}) must be at least {data.Length}.", nameof(destination));

        var slice = destination[..data.Length];
        if (Unit.Equals(target) || Unit.ScaleToSi == target.ScaleToSi)
        {
            data.AsSpan().CopyTo(slice);
            return;
        }
        ConvertSpan(data, slice, Unit.ScaleToSi, target.ScaleToSi);
    }

    /// <summary>Drops this series to a newly allocated raw <typeparamref name="T"/> array expressed in <paramref name="target"/>.</summary>
    public T[] ToArrayIn(SymbolicUnit target)
    {
        var output = new T[data.Length];
        CopyConvertedTo(target, output);
        return output;
    }

    /// <summary>Drops this series to a raw <typeparamref name="T"/> array expressed in <paramref name="unitSymbol"/>.</summary>
    public T[] ToArrayIn(string unitSymbol, UnitRegistry? registry = null) =>
        ToArrayIn(SymbolicUnit.From((registry ?? UnitRegistry.Standard).Resolve(unitSymbol)));

    /// <summary>Converts this series to a serializer-agnostic primitive <see cref="QuantitySeriesData{T}"/> wire record.</summary>
    public QuantitySeriesData<T> ToData(bool asciiSymbol = false) =>
        new(data.ToArray(), asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Spec?.Id, Origin?.Id);

    /// <summary>Reconstructs a validated <see cref="QuantitySeries{T}"/> from a serializer-agnostic <see cref="QuantitySeriesData{T}"/> record.</summary>
    public static QuantitySeries<T> FromData(QuantitySeriesData<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        PointOrigin? origin = !string.IsNullOrWhiteSpace(data.Origin)
            ? env.ResolveOrigin(data.Origin)
            : null;
        return FromValues(data.Values ?? [], unit, spec, origin);
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
        AddScalarSpan(data, shift, output);
        return new(output, Unit, newOrigin.Spec ?? Spec, newOrigin);
    }

    /// <summary>Automatically selects the best shared engineering SI prefix for this series and returns the scaled series.</summary>
    public QuantitySeries<T> AutoScale()
    {
        if (Origin is not null && Origin.OffsetFromBaseSi != 0d)
            throw new InvalidOperationException("Auto-scaling SI prefixes applies to zero-origin linear series.");
        var baseUnit = ResolvePrefixableBase();
        if (baseUnit is null) return this;

        double peak = ComputePeakAbs(data);
        double peakSi = peak * Unit.ScaleToSi;
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

    /// <summary>Returns minimum and maximum sample values as unit-safe <see cref="Quantity{T}"/> using SIMD reduction when available.</summary>
    public (Quantity<T> Min, Quantity<T> Max) MinMax()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        var (min, max) = ComputeMinMax(data);
        return (new(min, Unit, Spec), new(max, Unit, Spec));
    }

    /// <summary>Returns the peak-to-peak span (<c>Max - Min</c>) of the series.</summary>
    public Quantity<T> PeakToPeak()
    {
        var (min, max) = MinMax();
        return new(max.Value - min.Value, Unit, Spec);
    }

    /// <summary>Returns the arithmetic mean of the series using SIMD sum reduction when available.</summary>
    public Quantity<T> Mean()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        T sum = ComputeSum(data);
        return new(sum / T.CreateChecked(data.Length), Unit, Spec);
    }

    /// <summary>Returns the root-mean-square (RMS) magnitude in double precision using SIMD sum-of-squares when available.</summary>
    public Quantity<double> Rms()
    {
        if (data.Length == 0) throw new InvalidOperationException("Series is empty.");
        if (Origin is not null && Origin.OffsetFromBaseSi != 0d)
            throw new InvalidOperationException("RMS is undefined for offset-origin affine point series.");
        double sumSq = ComputeSumOfSquares(data);
        return new(Math.Sqrt(sumSq / data.Length), Unit, Spec);
    }

    /// <summary>Element-wise addition of compatible linear series using hardware SIMD when available.</summary>
    public static QuantitySeries<T> operator +(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        var (commonUnit, commonSpec) = PrepareBinaryAdditive(a, b, isSubtraction: false, out var resultOrigin);
        var aAligned = a.Unit.Equals(commonUnit) ? a.data : a.In(commonUnit).data;
        var bAligned = b.Unit.Equals(commonUnit) ? b.data : b.In(commonUnit).data;
        var output = new T[a.Count];
        AddSpans(aAligned, bAligned, output);
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
        SubtractSpans(aAligned, bAligned, output);
        return new(output, commonUnit, commonSpec, resultOrigin);
    }

    /// <summary>Element-wise multiplication of two linear series, preserving algebraic symbolic units (for example <c>V · A</c>).</summary>
    public static QuantitySeries<T> operator *(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        RequireLinearSeries(a);
        RequireLinearSeries(b);
        RequireSameLength(a, b);
        var output = new T[a.Count];
        MultiplySpans(a.data, b.data, output);
        return new(output, a.Unit * b.Unit, null, null);
    }

    /// <summary>Element-wise division of two linear series, preserving algebraic symbolic units (for example <c>V / A</c>).</summary>
    public static QuantitySeries<T> operator /(QuantitySeries<T> a, QuantitySeries<T> b)
    {
        RequireLinearSeries(a);
        RequireLinearSeries(b);
        RequireSameLength(a, b);
        var output = new T[a.Count];
        DivideSpans(a.data, b.data, output);
        return new(output, a.Unit / b.Unit, null, null);
    }

    /// <summary>Scales a linear series by a scalar factor using hardware SIMD when available.</summary>
    public static QuantitySeries<T> operator *(QuantitySeries<T> series, T scalar)
    {
        RequireLinearSeries(series);
        var output = new T[series.Count];
        MultiplyScalarSpan(series.data, scalar, output);
        return new(output, series.Unit, series.Spec, null);
    }

    /// <summary>Scales a linear series by a scalar factor using hardware SIMD when available.</summary>
    public static QuantitySeries<T> operator *(T scalar, QuantitySeries<T> series) => series * scalar;

    /// <summary>Divides a linear series by a scalar divisor using hardware SIMD when available.</summary>
    public static QuantitySeries<T> operator /(QuantitySeries<T> series, T divisor)
    {
        RequireLinearSeries(series);
        var output = new T[series.Count];
        DivideScalarSpan(series.data, divisor, output);
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
            if (a.Origin is not null && b.Origin is not null) resultOrigin = null;
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

    // --- SIMD-accelerated span kernels ---

    private static void AddSpans(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = a.Length - step;
            for (; i <= limit; i += step)
                (new Vector<T>(a.Slice(i)) + new Vector<T>(b.Slice(i))).CopyTo(dst.Slice(i));
        }
        for (; i < a.Length; i++) dst[i] = a[i] + b[i];
    }

    private static void SubtractSpans(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = a.Length - step;
            for (; i <= limit; i += step)
                (new Vector<T>(a.Slice(i)) - new Vector<T>(b.Slice(i))).CopyTo(dst.Slice(i));
        }
        for (; i < a.Length; i++) dst[i] = a[i] - b[i];
    }

    private static void MultiplySpans(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = a.Length - step;
            for (; i <= limit; i += step)
                (new Vector<T>(a.Slice(i)) * new Vector<T>(b.Slice(i))).CopyTo(dst.Slice(i));
        }
        for (; i < a.Length; i++) dst[i] = a[i] * b[i];
    }

    private static void DivideSpans(ReadOnlySpan<T> a, ReadOnlySpan<T> b, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && (typeof(T) == typeof(double) || typeof(T) == typeof(float)))
        {
            int step = Vector<T>.Count;
            int limit = a.Length - step;
            for (; i <= limit; i += step)
                (new Vector<T>(a.Slice(i)) / new Vector<T>(b.Slice(i))).CopyTo(dst.Slice(i));
        }
        for (; i < a.Length; i++) dst[i] = a[i] / b[i];
    }

    private static void AddScalarSpan(ReadOnlySpan<T> src, T scalar, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = src.Length - step;
            var sVec = new Vector<T>(scalar);
            for (; i <= limit; i += step)
                (new Vector<T>(src.Slice(i)) + sVec).CopyTo(dst.Slice(i));
        }
        for (; i < src.Length; i++) dst[i] = src[i] + scalar;
    }

    private static void MultiplyScalarSpan(ReadOnlySpan<T> src, T scalar, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = src.Length - step;
            var sVec = new Vector<T>(scalar);
            for (; i <= limit; i += step)
                (new Vector<T>(src.Slice(i)) * sVec).CopyTo(dst.Slice(i));
        }
        for (; i < src.Length; i++) dst[i] = src[i] * scalar;
    }

    private static void DivideScalarSpan(ReadOnlySpan<T> src, T divisor, Span<T> dst)
    {
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && (typeof(T) == typeof(double) || typeof(T) == typeof(float)))
        {
            int step = Vector<T>.Count;
            int limit = src.Length - step;
            var dVec = new Vector<T>(divisor);
            for (; i <= limit; i += step)
                (new Vector<T>(src.Slice(i)) / dVec).CopyTo(dst.Slice(i));
        }
        for (; i < src.Length; i++) dst[i] = src[i] / divisor;
    }

    private static void ConvertSpan(ReadOnlySpan<T> src, Span<T> dst, double srcScale, double dstScale)
    {
        if (typeof(T) == typeof(double))
        {
            var ratio = srcScale / dstScale;
            MultiplyScalarSpan(src, Unsafe.As<double, T>(ref ratio), dst);
            return;
        }
        if (typeof(T) == typeof(float))
        {
            var ratio = (float)(srcScale / dstScale);
            MultiplyScalarSpan(src, Unsafe.As<float, T>(ref ratio), dst);
            return;
        }
        if (typeof(T) == typeof(decimal))
        {
            decimal dSrc = decimal.CreateChecked(srcScale);
            decimal dDst = decimal.CreateChecked(dstScale);
            var srcDec = ReinterpretReadOnly<decimal>(src);
            var dstDec = Reinterpret<decimal>(dst);
            for (int i = 0; i < srcDec.Length; i++)
                dstDec[i] = (srcDec[i] * dSrc) / dDst;
            return;
        }

        // Integral fast SIMD path when ratio is an exact positive integer (e.g. V -> mV, s -> ms).
        double exactRatio = srcScale / dstScale;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported &&
            exactRatio >= 1d && Math.Abs(exactRatio - Math.Round(exactRatio)) <= 1e-12 * exactRatio)
        {
            T factor = T.CreateChecked(Math.Round(exactRatio));
            MultiplyScalarSpan(src, factor, dst);
            return;
        }

        for (int i = 0; i < src.Length; i++)
            dst[i] = Quantity<T>.ConvertScale(src[i], srcScale, dstScale, allowTruncation: false);
    }

    private static T ComputeSum(ReadOnlySpan<T> src)
    {
        T sum = T.Zero;
        int i = 0;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported)
        {
            int step = Vector<T>.Count;
            int limit = src.Length - step;
            var acc = Vector<T>.Zero;
            for (; i <= limit; i += step)
                acc += new Vector<T>(src.Slice(i));
            sum = Vector.Sum(acc);
        }
        for (; i < src.Length; i++) sum += src[i];
        return sum;
    }

    private static (T Min, T Max) ComputeMinMax(ReadOnlySpan<T> src)
    {
        T min = src[0];
        T max = src[0];
        int i = 1;
        if (Vector.IsHardwareAccelerated && Vector<T>.IsSupported && src.Length >= Vector<T>.Count)
        {
            int step = Vector<T>.Count;
            var minVec = new Vector<T>(src);
            var maxVec = minVec;
            i = step;
            int limit = src.Length - step;
            for (; i <= limit; i += step)
            {
                var v = new Vector<T>(src.Slice(i));
                minVec = Vector.Min(minVec, v);
                maxVec = Vector.Max(maxVec, v);
            }
            min = minVec[0];
            max = maxVec[0];
            for (int lane = 1; lane < step; lane++)
            {
                if (minVec[lane] < min) min = minVec[lane];
                if (maxVec[lane] > max) max = maxVec[lane];
            }
        }
        for (; i < src.Length; i++)
        {
            if (src[i] < min) min = src[i];
            if (src[i] > max) max = src[i];
        }
        return (min, max);
    }

    private static double ComputePeakAbs(ReadOnlySpan<T> src)
    {
        if (src.IsEmpty) return 0d;
        if (typeof(T) == typeof(double))
        {
            var span = ReinterpretReadOnly<double>(src);
            double peak = 0d;
            int i = 0;
            if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
            {
                int step = Vector<double>.Count;
                int limit = span.Length - step;
                var acc = Vector<double>.Zero;
                for (; i <= limit; i += step)
                {
                    var abs = Vector.Abs(new Vector<double>(span.Slice(i)));
                    acc = Vector.Max(acc, abs);
                }
                for (int lane = 0; lane < step; lane++)
                    if (double.IsFinite(acc[lane]) && acc[lane] > peak) peak = acc[lane];
            }
            for (; i < span.Length; i++)
            {
                double v = Math.Abs(span[i]);
                if (double.IsFinite(v) && v > peak) peak = v;
            }
            return peak;
        }
        if (typeof(T) == typeof(float))
        {
            var span = ReinterpretReadOnly<float>(src);
            float peak = 0f;
            int i = 0;
            if (Vector.IsHardwareAccelerated && span.Length >= Vector<float>.Count)
            {
                int step = Vector<float>.Count;
                int limit = span.Length - step;
                var acc = Vector<float>.Zero;
                for (; i <= limit; i += step)
                {
                    var abs = Vector.Abs(new Vector<float>(span.Slice(i)));
                    acc = Vector.Max(acc, abs);
                }
                for (int lane = 0; lane < step; lane++)
                    if (float.IsFinite(acc[lane]) && acc[lane] > peak) peak = acc[lane];
            }
            for (; i < span.Length; i++)
            {
                float v = MathF.Abs(span[i]);
                if (float.IsFinite(v) && v > peak) peak = v;
            }
            return peak;
        }

        double maxAbs = 0d;
        for (int i = 0; i < src.Length; i++)
        {
            double v = Math.Abs(double.CreateChecked(src[i]));
            if (double.IsFinite(v) && v > maxAbs) maxAbs = v;
        }
        return maxAbs;
    }

    private static double ComputeSumOfSquares(ReadOnlySpan<T> src)
    {
        if (typeof(T) == typeof(double))
        {
            var span = ReinterpretReadOnly<double>(src);
            double sumSq = 0d;
            int i = 0;
            if (Vector.IsHardwareAccelerated && span.Length >= Vector<double>.Count)
            {
                int step = Vector<double>.Count;
                int limit = span.Length - step;
                var acc = Vector<double>.Zero;
                for (; i <= limit; i += step)
                {
                    var v = new Vector<double>(span.Slice(i));
                    acc += v * v;
                }
                sumSq = Vector.Sum(acc);
            }
            for (; i < span.Length; i++) sumSq += span[i] * span[i];
            return sumSq;
        }
        if (typeof(T) == typeof(float))
        {
            var span = ReinterpretReadOnly<float>(src);
            double sumSq = 0d;
            int i = 0;
            if (Vector.IsHardwareAccelerated && span.Length >= Vector<float>.Count)
            {
                int step = Vector<float>.Count;
                int limit = span.Length - step;
                var acc = Vector<float>.Zero;
                for (; i <= limit; i += step)
                {
                    var v = new Vector<float>(span.Slice(i));
                    acc += v * v;
                }
                sumSq = Vector.Sum(acc);
            }
            for (; i < span.Length; i++) sumSq += (double)span[i] * span[i];
            return sumSq;
        }

        double total = 0d;
        for (int i = 0; i < src.Length; i++)
        {
            double v = double.CreateChecked(src[i]);
            total += v * v;
        }
        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<TTo> ReinterpretReadOnly<TTo>(ReadOnlySpan<T> span) =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<TTo> Reinterpret<TTo>(Span<T> span) =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<T, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
}
