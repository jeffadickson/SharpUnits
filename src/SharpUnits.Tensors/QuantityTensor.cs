using System.Numerics;
using System.Numerics.Tensors;

namespace SharpUnits.Tensors;

/// <summary>
/// Serializer-agnostic wire record for an N-dimensional <see cref="QuantityTensor{T}"/>.
/// Contains only primitive fields so any .NET serializer can round-trip shaped unit-tagged tensors.
/// </summary>
public readonly record struct QuantityTensorData<T>(
    T[] Values,
    long[] Shape,
    string Unit,
    string? Spec = null,
    string? Origin = null) where T : INumber<T>;

/// <summary>
/// N-dimensional, Structure-of-Arrays physical quantity tensor pairing a contiguous shaped <see cref="Tensor{T}"/>
/// with a single <see cref="SymbolicUnit"/>, optional ISO 80000 <see cref="QuantitySpec"/>, and optional affine <see cref="PointOrigin"/>.
/// </summary>
/// <typeparam name="T">Numeric representation type (<see cref="INumber{TSelf}"/>).</typeparam>
/// <remarks>
/// Built for multi-channel oscilloscope captures (<c>[Channels, Samples]</c>), thermal frames (<c>[Height, Width]</c>),
/// spectrograms (<c>[Frames, Bins]</c>), and direct zero-copy interop with <see cref="ReadOnlyTensorSpan{T}"/>,
/// <see cref="Tensor{T}"/>, and SIMD <see cref="TensorPrimitives"/>.
/// </remarks>
public sealed class QuantityTensor<T> where T : INumber<T>
{
    private readonly T[] flatData;
    private readonly nint[] shape;

    /// <summary>Flat contiguous read-only span over all elements in row-major order.</summary>
    public ReadOnlySpan<T> FlatValues => flatData;

    /// <summary>Dimensions (lengths) along each tensor axis.</summary>
    public ReadOnlySpan<nint> Shape => shape;

    /// <summary>Number of tensor axes (rank).</summary>
    public int Rank => shape.Length;

    /// <summary>Total number of scalar elements across all dimensions.</summary>
    public int FlattenedLength => flatData.Length;

    /// <summary>Shared symbolic unit of all elements.</summary>
    public SymbolicUnit Unit { get; }

    /// <summary>Optional ISO 80000 ISQ specification shared by all elements.</summary>
    public QuantitySpec? Spec { get; }

    /// <summary>Optional affine origin (non-null for Celsius thermal frames, gauge pressure grids, etc.).</summary>
    public PointOrigin? Origin { get; }

    /// <summary>Canonical exact rational dimension.</summary>
    public RationalDimension Dimension => Unit.Dimension;

    private QuantityTensor(T[] ownedFlatData, nint[] ownedShape, SymbolicUnit unit, QuantitySpec? spec, PointOrigin? origin)
    {
        flatData = ownedFlatData;
        shape = ownedShape;
        Unit = unit;
        Spec = spec;
        Origin = origin;
    }

    /// <summary>Creates an N-dimensional <see cref="QuantityTensor{T}"/> by copying flat row-major values with explicit <paramref name="lengths"/>.</summary>
    public static QuantityTensor<T> FromValues(
        ReadOnlySpan<T> flatValues,
        ReadOnlySpan<nint> lengths,
        SymbolicUnit unit,
        QuantitySpec? spec = null,
        PointOrigin? origin = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ValidateMetadata(unit, spec, origin, out var resolvedSpec);
        nint[] shapeCopy = ValidateShape(lengths, flatValues.Length);
        return new(flatValues.ToArray(), shapeCopy, unit, resolvedSpec, origin);
    }

    /// <summary>Wraps a <see cref="Tensor{T}"/> into an immutable unit-tagged <see cref="QuantityTensor{T}"/>.</summary>
    public static QuantityTensor<T> FromTensor(
        Tensor<T> tensor,
        SymbolicUnit unit,
        QuantitySpec? spec = null,
        PointOrigin? origin = null)
    {
        ArgumentNullException.ThrowIfNull(tensor);
        ArgumentNullException.ThrowIfNull(unit);
        ValidateMetadata(unit, spec, origin, out var resolvedSpec);
        int count = checked((int)tensor.FlattenedLength);
        var flat = new T[count];
        tensor.FlattenTo(flat);
        var lengths = tensor.Lengths.ToArray();
        return new(flat, lengths, unit, resolvedSpec, origin);
    }

    /// <summary>Wraps a <see cref="ReadOnlyTensorSpan{T}"/> into an immutable unit-tagged <see cref="QuantityTensor{T}"/>.</summary>
    public static QuantityTensor<T> FromTensorSpan(
        ReadOnlyTensorSpan<T> tensorSpan,
        SymbolicUnit unit,
        QuantitySpec? spec = null,
        PointOrigin? origin = null)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ValidateMetadata(unit, spec, origin, out var resolvedSpec);
        int count = checked((int)tensorSpan.FlattenedLength);
        var flat = new T[count];
        tensorSpan.FlattenTo(flat);
        var lengths = tensorSpan.Lengths.ToArray();
        return new(flat, lengths, unit, resolvedSpec, origin);
    }

    /// <summary>Creates a 1-D <see cref="QuantityTensor{T}"/> from an existing <see cref="QuantitySeries{T}"/>.</summary>
    public static QuantityTensor<T> FromSeries(QuantitySeries<T> series)
    {
        ArgumentNullException.ThrowIfNull(series);
        return FromValues(series.Values, [series.Count], series.Unit, series.Spec, series.Origin);
    }

    /// <summary>Creates an affine temperature tensor in degrees Celsius (anchored to <see cref="PointOrigins.IcePoint"/> without offset drift).</summary>
    public static QuantityTensor<T> FromCelsius(ReadOnlySpan<T> flatCelsius, ReadOnlySpan<nint> lengths) =>
        FromValues(flatCelsius, lengths, Units.Kelvin, Isq.ThermodynamicTemperature, PointOrigins.IcePoint);

    /// <summary>Creates an affine gauge pressure tensor anchored to <see cref="PointOrigins.StandardAtmosphere"/>.</summary>
    public static QuantityTensor<T> FromGaugePressure(ReadOnlySpan<T> flatValues, ReadOnlySpan<nint> lengths, SymbolicUnit pressureUnit) =>
        FromValues(flatValues, lengths, pressureUnit, Isq.Pressure, PointOrigins.StandardAtmosphere);

    /// <summary>Zero-copy read-only <see cref="ReadOnlyTensorSpan{T}"/> view for ML.NET, ONNX Runtime, GPU, or <see cref="TensorPrimitives"/> pipelines.</summary>
    public ReadOnlyTensorSpan<T> AsReadOnlyTensorSpan() => new(flatData, shape);

    /// <summary>Drops this tensor to a detached <see cref="Tensor{T}"/> in its current <see cref="Unit"/>.</summary>
    public Tensor<T> ToTensor() => Tensor.Create(flatData.ToArray(), shape);

    /// <summary>Drops this tensor to a raw <see cref="Tensor{T}"/> explicitly converted into <paramref name="targetUnit"/>.</summary>
    public Tensor<T> ToTensorIn(SymbolicUnit targetUnit)
    {
        var converted = In(targetUnit);
        return Tensor.Create(converted.flatData.ToArray(), converted.shape);
    }

    /// <summary>Converts a 1-D tensor (or flattened view) into a <see cref="QuantitySeries{T}"/>.</summary>
    public QuantitySeries<T> ToSeries() =>
        QuantitySeries<T>.FromValues(flatData, Unit, Spec, Origin);

    /// <summary>Reads a single element at <paramref name="indices"/> as a <see cref="Quantity{T}"/>.</summary>
    public Quantity<T> GetQuantity(params ReadOnlySpan<nint> indices) =>
        new(GetFlatElement(indices), Unit, Spec);

    /// <summary>Reads a single element at <paramref name="indices"/> as an affine <see cref="QuantityPoint{T}"/>.</summary>
    public QuantityPoint<T> GetPoint(params ReadOnlySpan<nint> indices)
    {
        if (Origin is null)
            throw new InvalidOperationException("Tensor does not have an affine PointOrigin.");
        return new(new Quantity<T>(GetFlatElement(indices), Unit, Spec), Origin);
    }

    /// <summary>Converts all elements to a compatible linear target unit using SIMD acceleration, preserving exact N-D shape.</summary>
    public QuantityTensor<T> In(SymbolicUnit target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.IsAffine)
            throw new InvalidOperationException("Target unit must be linear.");
        if (!Unit.IsCompatibleWith(target))
            throw new InvalidOperationException($"Incompatible dimensions: '{Unit.Symbol}' vs '{target.Symbol}'.");
        if (Unit.Equals(target) || Unit.ScaleToSi == target.ScaleToSi)
            return new(flatData.ToArray(), shape.ToArray(), target, Spec, Origin);

        var output = new T[flatData.Length];
        ConvertSpan(flatData, output, Unit, target);
        return new(output, shape.ToArray(), target, Spec, Origin);
    }

    /// <summary>Re-anchors an affine tensor to a different compatible <see cref="PointOrigin"/> (for example °C frame to K, or gauge to vacuum pressure).</summary>
    public QuantityTensor<T> RelativeTo(PointOrigin newOrigin)
    {
        ArgumentNullException.ThrowIfNull(newOrigin);
        if (Origin is null)
            throw new InvalidOperationException("Cannot re-anchor a non-affine displacement tensor.");
        if (newOrigin.Dimension != Origin.Dimension)
            throw new InvalidOperationException($"Incompatible origin dimensions: '{Origin.Name}' vs '{newOrigin.Name}'.");
        if (Origin == newOrigin)
            return new(flatData.ToArray(), shape.ToArray(), Unit, Spec, Origin);

        T shift = QuantityPoint<T>.At(T.Zero, Unit, Origin).RelativeTo(newOrigin).Value;
        var output = new T[flatData.Length];
        TensorPrimitives.Add<T>(flatData, shift, output);
        return new(output, shape.ToArray(), Unit, newOrigin.Spec ?? Spec, newOrigin);
    }

    /// <summary>Selects the optimal shared engineering SI prefix across all elements of a zero-origin tensor.</summary>
    public QuantityTensor<T> AutoScale()
    {
        var scaledSeries = QuantitySeries<T>.FromValues(flatData, Unit, Spec, Origin).AutoScale();
        return new(scaledSeries.Values.ToArray(), shape.ToArray(), scaledSeries.Unit, scaledSeries.Spec, scaledSeries.Origin);
    }

    /// <summary>Reshapes this tensor to <paramref name="newLengths"/> (same element count) while preserving unit, ISQ spec, and origin.</summary>
    public QuantityTensor<T> Reshape(ReadOnlySpan<nint> newLengths)
    {
        nint[] shapeCopy = ValidateShape(newLengths, flatData.Length);
        return new(flatData.ToArray(), shapeCopy, Unit, Spec, Origin);
    }

    /// <summary>Slices the leading axis at <paramref name="index"/> (for example extracting channel <c>index</c> from a <c>[Channels, Samples]</c> capture).</summary>
    public QuantityTensor<T> SliceLeadingAxis(nint index)
    {
        if (shape.Length < 2)
            throw new InvalidOperationException("SliceLeadingAxis requires a tensor of rank >= 2.");
        if (index < 0 || index >= shape[0])
            throw new ArgumentOutOfRangeException(nameof(index));

        int stride = flatData.Length / checked((int)shape[0]);
        int start = checked((int)index * stride);
        var subData = new T[stride];
        flatData.AsSpan(start, stride).CopyTo(subData);
        var subShape = shape.AsSpan(1).ToArray();
        return new(subData, subShape, Unit, Spec, Origin);
    }

    /// <summary>Returns the minimum element as a <see cref="Quantity{T}"/> using <see cref="TensorPrimitives.Min{T}(ReadOnlySpan{T})"/>.</summary>
    public Quantity<T> Min()
    {
        if (flatData.Length == 0) throw new InvalidOperationException("Tensor is empty.");
        return new(TensorPrimitives.Min<T>(flatData), Unit, Spec);
    }

    /// <summary>Returns the maximum element as a <see cref="Quantity{T}"/> using <see cref="TensorPrimitives.Max{T}(ReadOnlySpan{T})"/>.</summary>
    public Quantity<T> Max()
    {
        if (flatData.Length == 0) throw new InvalidOperationException("Tensor is empty.");
        return new(TensorPrimitives.Max<T>(flatData), Unit, Spec);
    }

    /// <summary>Returns the sum of all elements as a <see cref="Quantity{T}"/> using <see cref="TensorPrimitives.Sum{T}(ReadOnlySpan{T})"/>.</summary>
    public Quantity<T> Sum()
    {
        RequireLinearTensor(this);
        return new(TensorPrimitives.Sum<T>(flatData), Unit, Spec);
    }

    /// <summary>Returns the arithmetic mean of all elements.</summary>
    public Quantity<T> Mean()
    {
        if (flatData.Length == 0) throw new InvalidOperationException("Tensor is empty.");
        T sum = TensorPrimitives.Sum<T>(flatData);
        return new(sum / T.CreateChecked(flatData.Length), Unit, Spec);
    }

    /// <summary>Clamps every element to [<paramref name="min"/>, <paramref name="max"/>] (automatically converting bounds into this tensor's unit) using <see cref="TensorPrimitives.Clamp{T}(ReadOnlySpan{T}, T, T, Span{T})"/>.</summary>
    public QuantityTensor<T> Clamp(Quantity<T> min, Quantity<T> max)
    {
        RequireLinearTensor(this);
        T minVal = min.In(Unit).Value;
        T maxVal = max.In(Unit).Value;
        if (minVal > maxVal)
            throw new ArgumentException("Minimum clamp bound cannot exceed maximum clamp bound.");
        var output = new T[flatData.Length];
        TensorPrimitives.Clamp<T>(flatData, minVal, maxVal, output);
        return new(output, shape.ToArray(), Unit, Spec, Origin);
    }

    /// <summary>Converts this tensor to a serializer-agnostic primitive <see cref="QuantityTensorData{T}"/> wire record.</summary>
    public QuantityTensorData<T> ToData(bool asciiSymbol = false)
    {
        var shapeLong = new long[shape.Length];
        for (int i = 0; i < shape.Length; i++) shapeLong[i] = shape[i];
        return new(flatData.ToArray(), shapeLong, asciiSymbol ? Unit.AsciiSymbol : Unit.Symbol, Spec?.Id, Origin?.Id);
    }

    /// <summary>Reconstructs a validated <see cref="QuantityTensor{T}"/> from a serializer-agnostic <see cref="QuantityTensorData{T}"/> record.</summary>
    public static QuantityTensor<T> FromData(QuantityTensorData<T> data, UnitEnvironment? environment = null)
    {
        var env = environment ?? UnitEnvironment.Standard;
        var unit = env.ResolveUnit(data.Unit);
        QuantitySpec? spec = null;
        if (!string.IsNullOrWhiteSpace(data.Spec) && !Isq.TryGetById(data.Spec, out spec))
            spec = new QuantitySpec(data.Spec, data.Spec, unit.Dimension);
        PointOrigin? origin = !string.IsNullOrWhiteSpace(data.Origin)
            ? env.ResolveOrigin(data.Origin)
            : null;
        var lengths = new nint[data.Shape.Length];
        for (int i = 0; i < data.Shape.Length; i++) lengths[i] = checked((nint)data.Shape[i]);
        return FromValues(data.Values ?? [], lengths, unit, spec, origin);
    }

    /// <summary>Element-wise addition enforcing shape, dimensional, ISQ, and affine-space rules via <see cref="TensorPrimitives.Add{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/>.</summary>
    public static QuantityTensor<T> operator +(QuantityTensor<T> a, QuantityTensor<T> b)
    {
        RequireSameShape(a, b);
        var (commonUnit, commonSpec, resultOrigin) = ResolveAdditiveMetadata(a, b, isAdd: true);
        var aConv = a.Unit.Equals(commonUnit) ? a : a.In(commonUnit);
        var bConv = b.Unit.Equals(commonUnit) ? b : b.In(commonUnit);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Add<T>(aConv.flatData, bConv.flatData, output);
        return new(output, a.shape.ToArray(), commonUnit, commonSpec, resultOrigin);
    }

    /// <summary>Element-wise subtraction (subtracting two affine point tensors yields a linear displacement tensor).</summary>
    public static QuantityTensor<T> operator -(QuantityTensor<T> a, QuantityTensor<T> b)
    {
        RequireSameShape(a, b);
        var (commonUnit, commonSpec, resultOrigin) = ResolveAdditiveMetadata(a, b, isAdd: false);
        var aAligned = a.Origin is not null && b.Origin is not null && a.Origin != b.Origin ? a.RelativeTo(b.Origin) : a;
        var aConv = aAligned.Unit.Equals(commonUnit) ? aAligned : aAligned.In(commonUnit);
        var bConv = b.Unit.Equals(commonUnit) ? b : b.In(commonUnit);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Subtract<T>(aConv.flatData, bConv.flatData, output);
        return new(output, a.shape.ToArray(), commonUnit, commonSpec, resultOrigin);
    }

    /// <summary>Element-wise multiplication combining symbolic units via <see cref="TensorPrimitives.Multiply{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/>.</summary>
    public static QuantityTensor<T> operator *(QuantityTensor<T> a, QuantityTensor<T> b)
    {
        RequireSameShape(a, b);
        RequireLinearTensor(a);
        RequireLinearTensor(b);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Multiply<T>(a.flatData, b.flatData, output);
        return new(output, a.shape.ToArray(), a.Unit * b.Unit, null, null);
    }

    /// <summary>Element-wise division combining symbolic units via <see cref="TensorPrimitives.Divide{T}(ReadOnlySpan{T}, ReadOnlySpan{T}, Span{T})"/>.</summary>
    public static QuantityTensor<T> operator /(QuantityTensor<T> a, QuantityTensor<T> b)
    {
        RequireSameShape(a, b);
        RequireLinearTensor(a);
        RequireLinearTensor(b);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Divide<T>(a.flatData, b.flatData, output);
        return new(output, a.shape.ToArray(), a.Unit / b.Unit, null, null);
    }

    /// <summary>Scales a linear tensor by a dimensionless scalar using <see cref="TensorPrimitives.Multiply{T}(ReadOnlySpan{T}, T, Span{T})"/>.</summary>
    public static QuantityTensor<T> operator *(QuantityTensor<T> a, T scalar)
    {
        RequireLinearTensor(a);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Multiply<T>(a.flatData, scalar, output);
        return new(output, a.shape.ToArray(), a.Unit, a.Spec, null);
    }

    /// <summary>Scales a linear tensor by a dimensionless scalar.</summary>
    public static QuantityTensor<T> operator *(T scalar, QuantityTensor<T> a) => a * scalar;

    /// <summary>Divides a linear tensor by a dimensionless scalar using <see cref="TensorPrimitives.Divide{T}(ReadOnlySpan{T}, T, Span{T})"/>.</summary>
    public static QuantityTensor<T> operator /(QuantityTensor<T> a, T divisor)
    {
        RequireLinearTensor(a);
        var output = new T[a.flatData.Length];
        TensorPrimitives.Divide<T>(a.flatData, divisor, output);
        return new(output, a.shape.ToArray(), a.Unit, a.Spec, null);
    }

    /// <summary>Returns a concise summary of shape, unit, and optional origin.</summary>
    public override string ToString()
    {
        string dims = string.Join("×", shape);
        return Origin is null
            ? $"QuantityTensor<{typeof(T).Name}>[{dims}] ({Unit.Symbol})"
            : $"QuantityTensor<{typeof(T).Name}>[{dims}] ({Unit.Symbol}, origin: {Origin.Name})";
    }

    private T GetFlatElement(ReadOnlySpan<nint> indices)
    {
        if (indices.Length != shape.Length)
            throw new ArgumentException($"Expected {shape.Length} indices, got {indices.Length}.", nameof(indices));
        int flatIndex = 0;
        int stride = 1;
        for (int i = shape.Length - 1; i >= 0; i--)
        {
            if (indices[i] < 0 || indices[i] >= shape[i])
                throw new ArgumentOutOfRangeException(nameof(indices));
            flatIndex += checked((int)indices[i] * stride);
            stride *= checked((int)shape[i]);
        }
        return flatData[flatIndex];
    }

    private static void ValidateMetadata(SymbolicUnit unit, QuantitySpec? spec, PointOrigin? origin, out QuantitySpec? resolvedSpec)
    {
        if (unit.IsAffine)
            throw new InvalidOperationException("Use a linear unit paired with a PointOrigin (for example Units.Kelvin + PointOrigins.IcePoint) for affine tensors.");
        resolvedSpec = spec ?? origin?.Spec ?? unit.DefaultSpec;
        if (resolvedSpec is not null && resolvedSpec.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Specification '{resolvedSpec.Id}' dimension disagrees with unit '{unit.Symbol}'.");
        if (origin is not null && origin.Dimension != unit.Dimension)
            throw new InvalidOperationException($"Origin '{origin.Name}' dimension disagrees with unit '{unit.Symbol}'.");
    }

    private static nint[] ValidateShape(ReadOnlySpan<nint> lengths, int flatLength)
    {
        if (lengths.Length == 0)
            throw new ArgumentException("Tensor shape must have at least one dimension.", nameof(lengths));
        long product = 1;
        var copy = new nint[lengths.Length];
        for (int i = 0; i < lengths.Length; i++)
        {
            if (lengths[i] < 0)
                throw new ArgumentOutOfRangeException(nameof(lengths), "Tensor dimensions cannot be negative.");
            copy[i] = lengths[i];
            product = checked(product * lengths[i]);
        }
        if (product != flatLength)
            throw new ArgumentException($"Shape product ({product}) does not match element count ({flatLength}).", nameof(lengths));
        return copy;
    }

    private static void RequireSameShape(QuantityTensor<T> a, QuantityTensor<T> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (!a.shape.AsSpan().SequenceEqual(b.shape))
            throw new ArgumentException($"Tensor shape mismatch: [{string.Join(",", a.shape)}] vs [{string.Join(",", b.shape)}].");
    }

    private static void RequireLinearTensor(QuantityTensor<T> t)
    {
        ArgumentNullException.ThrowIfNull(t);
        if (t.Origin is not null)
            throw new InvalidOperationException("Affine point tensors cannot participate in linear scaling, summation, or multiplicative algebra.");
    }

    private static (SymbolicUnit Unit, QuantitySpec? Spec, PointOrigin? Origin) ResolveAdditiveMetadata(
        QuantityTensor<T> a, QuantityTensor<T> b, bool isAdd)
    {
        if (!a.Unit.IsCompatibleWith(b.Unit))
            throw new InvalidOperationException($"Incompatible dimensions: '{a.Unit.Symbol}' vs '{b.Unit.Symbol}'.");

        QuantitySpec? commonSpec = a.Spec ?? b.Spec;
        if (a.Spec is not null && b.Spec is not null)
        {
            commonSpec = QuantitySpec.CommonSpec(a.Spec, b.Spec)
                ?? throw new InvalidOperationException($"Incompatible ISQ quantity kinds: '{a.Spec.Id}' vs '{b.Spec.Id}'.");
        }

        PointOrigin? resultOrigin;
        if (isAdd)
        {
            if (a.Origin is not null && b.Origin is not null)
                throw new InvalidOperationException("Cannot add two affine point tensors.");
            resultOrigin = a.Origin ?? b.Origin;
        }
        else
        {
            if (a.Origin is not null && b.Origin is not null) resultOrigin = null;
            else if (a.Origin is not null && b.Origin is null) resultOrigin = a.Origin;
            else if (a.Origin is null && b.Origin is not null)
                throw new InvalidOperationException("Cannot subtract an affine point tensor from a displacement tensor.");
            else resultOrigin = null;
        }

        return (SymbolicUnit.CommonUnit(a.Unit, b.Unit), commonSpec, resultOrigin);
    }

    private static void ConvertSpan(ReadOnlySpan<T> src, Span<T> dst, SymbolicUnit srcUnit, SymbolicUnit dstUnit)
    {
        if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
        {
            T factor = T.CreateChecked(srcUnit.ScaleToSi / dstUnit.ScaleToSi);
            TensorPrimitives.Multiply<T>(src, factor, dst);
            return;
        }
        for (int i = 0; i < src.Length; i++)
            dst[i] = Quantity<T>.Of(src[i], srcUnit).In(dstUnit).Value;
    }
}
