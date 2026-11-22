using System.Numerics;
using System.Numerics.Tensors;

namespace SharpUnits.Tensors;

/// <summary>
/// Zero-copy bridges between <see cref="QuantitySeries{T}"/> and <see cref="System.Numerics.Tensors"/>
/// (<see cref="ReadOnlyTensorSpan{T}"/>, <see cref="Tensor{T}"/>, and <see cref="QuantityTensor{T}"/>).
/// </summary>
public static class QuantitySeriesTensorExtensions
{
    /// <summary>Creates a zero-copy 1-D <see cref="ReadOnlyTensorSpan{T}"/> over the series' underlying buffer.</summary>
    public static ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan<T>(this QuantitySeries<T> series) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);
        return new(series.Values);
    }

    /// <summary>Drops this series to a raw 1-D <see cref="Tensor{T}"/> in its current unit.</summary>
    public static Tensor<T> ToTensor<T>(this QuantitySeries<T> series) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);
        return Tensor.Create(series.Values.ToArray());
    }

    /// <summary>Drops this series to a raw 1-D <see cref="Tensor{T}"/> explicitly converted into <paramref name="targetUnit"/>.</summary>
    public static Tensor<T> ToTensorIn<T>(this QuantitySeries<T> series, SymbolicUnit targetUnit) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);
        return Tensor.Create(series.ToArrayIn(targetUnit));
    }

    /// <summary>Lifts this 1-D <see cref="QuantitySeries{T}"/> into a 1-D <see cref="QuantityTensor{T}"/>.</summary>
    public static QuantityTensor<T> ToQuantityTensor<T>(this QuantitySeries<T> series) where T : INumber<T> =>
        QuantityTensor<T>.FromSeries(series);
}

/// <summary>
/// SIMD-accelerated engineering and DSP kernels backed by <see cref="TensorPrimitives"/> with full dimensional,
/// ISQ, and affine safety across <see cref="QuantitySeries{T}"/> and <see cref="QuantityTensor{T}"/>.
/// </summary>
public static class QuantityTensorMath
{
    /// <summary>
    /// Computes the SIMD dot product of two linear series, multiplying their symbolic units and dimensions
    /// (for example <c>Voltage · Current → Watts</c> or <c>Force · Displacement → Joules</c>).
    /// </summary>
    public static Quantity<T> Dot<T>(QuantitySeries<T> a, QuantitySeries<T> b) where T : INumber<T>
    {
        RequireLinearPair(a, b);
        T result = TensorPrimitives.Dot<T>(a.Values, b.Values);
        return Quantity<T>.Of(result, a.Unit * b.Unit);
    }

    /// <summary>Computes the SIMD L2 Euclidean norm of a linear series, preserving its unit and ISQ spec.</summary>
    public static Quantity<T> Norm<T>(QuantitySeries<T> series) where T : IFloatingPointIeee754<T>
    {
        RequireLinear(series);
        T norm = TensorPrimitives.Norm<T>(series.Values);
        return Quantity<T>.Of(norm, series.Unit, series.Spec);
    }

    /// <summary>Computes the SIMD L2 Euclidean norm of a linear <see cref="QuantityTensor{T}"/>, preserving its unit and ISQ spec.</summary>
    public static Quantity<T> Norm<T>(QuantityTensor<T> tensor) where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(tensor);
        if (tensor.Origin is not null)
            throw new InvalidOperationException("Norm is only defined for linear displacement tensors.");
        T norm = TensorPrimitives.Norm<T>(tensor.FlatValues);
        return Quantity<T>.Of(norm, tensor.Unit, tensor.Spec);
    }

    /// <summary>Computes the SIMD sum of squares of a linear series, squaring its symbolic unit (for example <c>V → V²</c>).</summary>
    public static Quantity<T> SumOfSquares<T>(QuantitySeries<T> series) where T : INumber<T>
    {
        RequireLinear(series);
        T sumSq = TensorPrimitives.SumOfSquares<T>(series.Values);
        return Quantity<T>.Of(sumSq, series.Unit.Pow(2));
    }

    /// <summary>Computes the SIMD Euclidean distance between two dimensionally compatible series in their finer common unit.</summary>
    public static Quantity<T> Distance<T>(QuantitySeries<T> a, QuantitySeries<T> b) where T : IFloatingPointIeee754<T>
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Count != b.Count)
            throw new ArgumentException($"Series length mismatch: {a.Count} vs {b.Count}.");
        if (!a.Unit.IsCompatibleWith(b.Unit))
            throw new InvalidOperationException($"Incompatible dimensions: '{a.Unit.Symbol}' vs '{b.Unit.Symbol}'.");

        var common = SymbolicUnit.CommonUnit(a.Unit, b.Unit);
        var aAligned = a.Origin is not null && b.Origin is not null && a.Origin != b.Origin ? a.RelativeTo(b.Origin) : a;
        var aConv = aAligned.Unit.Equals(common) ? aAligned : aAligned.In(common);
        var bConv = b.Unit.Equals(common) ? b : b.In(common);
        T dist = TensorPrimitives.Distance<T>(aConv.Values, bConv.Values);
        return Quantity<T>.Of(dist, common, a.Spec is not null && b.Spec is not null ? QuantitySpec.CommonSpec(a.Spec, b.Spec) : a.Spec ?? b.Spec);
    }

    /// <summary>Computes the dimensionless SIMD cosine similarity between two linear series of equal length.</summary>
    public static T CosineSimilarity<T>(QuantitySeries<T> a, QuantitySeries<T> b) where T : IFloatingPointIeee754<T>
    {
        RequireLinearPair(a, b);
        return TensorPrimitives.CosineSimilarity<T>(a.Values, b.Values);
    }

    /// <summary>
    /// Computes dimensionally validated SIMD fused multiply-add <c>(a * b) + c</c> using <see cref="TensorPrimitives.MultiplyAdd{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/>.
    /// Automatically converts <paramref name="addend"/> into the product unit <c>a.Unit * b.Unit</c>.
    /// </summary>
    public static QuantitySeries<T> MultiplyAdd<T>(QuantitySeries<T> a, QuantitySeries<T> b, QuantitySeries<T> addend) where T : INumber<T>
    {
        RequireLinearPair(a, b);
        RequireLinear(addend);
        if (a.Count != addend.Count)
            throw new ArgumentException($"Series length mismatch: {a.Count} vs {addend.Count}.");
        var prodUnit = a.Unit * b.Unit;
        if (!prodUnit.IsCompatibleWith(addend.Unit))
            throw new InvalidOperationException($"Addend dimension '{addend.Unit.Symbol}' is incompatible with product dimension '{prodUnit.Symbol}'.");

        var cAligned = addend.Unit.Equals(prodUnit) ? addend : addend.In(prodUnit);
        var output = new T[a.Count];
        TensorPrimitives.MultiplyAdd<T>(a.Values, b.Values, cAligned.Values, output);
        return QuantitySeries<T>.FromValues(output, prodUnit);
    }

    /// <summary>Clamps every sample of a linear series to [<paramref name="min"/>, <paramref name="max"/>] using SIMD <see cref="TensorPrimitives.Clamp{T}(ReadOnlySpan{T}, T, T, Span{T})"/>.</summary>
    public static QuantitySeries<T> Clamp<T>(QuantitySeries<T> series, Quantity<T> min, Quantity<T> max) where T : INumber<T>
    {
        RequireLinear(series);
        T minVal = min.In(series.Unit).Value;
        T maxVal = max.In(series.Unit).Value;
        if (minVal > maxVal)
            throw new ArgumentException("Minimum clamp bound cannot exceed maximum clamp bound.");
        var output = new T[series.Count];
        TensorPrimitives.Clamp<T>(series.Values, minVal, maxVal, output);
        return QuantitySeries<T>.FromValues(output, series.Unit, series.Spec);
    }

    /// <summary>Returns a dimensionless L2-normalized unit-vector <see cref="Tensor{T}"/> from a linear series.</summary>
    public static Tensor<T> Normalize<T>(QuantitySeries<T> series) where T : IFloatingPointIeee754<T>
    {
        RequireLinear(series);
        T norm = TensorPrimitives.Norm<T>(series.Values);
        if (norm == T.Zero)
            throw new InvalidOperationException("Cannot L2-normalize a zero-norm series.");
        var output = new T[series.Count];
        TensorPrimitives.Divide<T>(series.Values, norm, output);
        return Tensor.Create(output);
    }

    /// <summary>
    /// Computes field-quantity decibels <c>20 * log10(|x| / |ref|)</c> using vectorized <see cref="TensorPrimitives.Log10{T}(ReadOnlySpan{T}, Span{T})"/>
    /// (for Bode voltage/current gain or FFT magnitude spectra).
    /// </summary>
    public static QuantitySeries<T> MagnitudeToDecibels<T>(QuantitySeries<T> series, Quantity<T> reference) where T : IFloatingPointIeee754<T>
    {
        RequireLinear(series);
        T refVal = T.Abs(reference.In(series.Unit).Value);
        if (refVal <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(reference), "Decibel reference must be non-zero.");

        var work = new T[series.Count];
        TensorPrimitives.Abs<T>(series.Values, work);
        TensorPrimitives.Divide<T>(work, refVal, work);
        TensorPrimitives.Log10<T>(work, work);
        TensorPrimitives.Multiply<T>(work, T.CreateChecked(20), work);
        return QuantitySeries<T>.FromValues(work, SymbolicUnit.One, Isq.Dimensionless);
    }

    /// <summary>
    /// Computes power-quantity decibels <c>10 * log10(x / ref)</c> using vectorized <see cref="TensorPrimitives.Log10{T}(ReadOnlySpan{T}, Span{T})"/>.
    /// </summary>
    public static QuantitySeries<T> PowerToDecibels<T>(QuantitySeries<T> series, Quantity<T> reference) where T : IFloatingPointIeee754<T>
    {
        RequireLinear(series);
        T refVal = reference.In(series.Unit).Value;
        if (refVal <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(reference), "Power reference must be positive.");

        var work = new T[series.Count];
        TensorPrimitives.Divide<T>(series.Values, refVal, work);
        TensorPrimitives.Log10<T>(work, work);
        TensorPrimitives.Multiply<T>(work, T.CreateChecked(10), work);
        return QuantitySeries<T>.FromValues(work, SymbolicUnit.One, Isq.Dimensionless);
    }

    /// <summary>Converts a power series into dBm (<c>10 * log10(P / 1 mW)</c>) via vectorized <see cref="TensorPrimitives.Log10{T}(ReadOnlySpan{T}, Span{T})"/>.</summary>
    public static QuantitySeries<T> ConvertToDbm<T>(QuantitySeries<T> powerSeries) where T : IFloatingPointIeee754<T> =>
        PowerToDecibels(powerSeries, Quantity<T>.Of(T.One, Units.Milliwatt));

    /// <summary>Converts a voltage series into dBV (<c>20 * log10(|V| / 1 V)</c>) via vectorized <see cref="TensorPrimitives.Log10{T}(ReadOnlySpan{T}, Span{T})"/>.</summary>
    public static QuantitySeries<T> ConvertToDbv<T>(QuantitySeries<T> voltageSeries) where T : IFloatingPointIeee754<T> =>
        MagnitudeToDecibels(voltageSeries, Quantity<T>.Of(T.One, Units.Volt));

    private static void RequireLinear<T>(QuantitySeries<T> series) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);
        if (series.Origin is not null)
            throw new InvalidOperationException("Operation requires a linear (non-affine) QuantitySeries.");
    }

    private static void RequireLinearPair<T>(QuantitySeries<T> a, QuantitySeries<T> b) where T : INumber<T>
    {
        RequireLinear(a);
        RequireLinear(b);
        if (a.Count != b.Count)
            throw new ArgumentException($"Series length mismatch: {a.Count} vs {b.Count}.");
    }
}
