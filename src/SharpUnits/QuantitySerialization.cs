using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpUnits;

/// <summary>
/// Serializer-agnostic primitive wire record for <see cref="Quantity"/>.
/// Contains only primitive fields so any .NET serializer (MessagePack, MemoryPack, Protobuf, YAML, XML, Dapper, EF Core, Parquet) can round-trip it without custom converters.
/// </summary>
public readonly record struct QuantityData(
    double Value,
    string Unit,
    string? Spec = null,
    QuantityScale Scale = QuantityScale.Absolute);

/// <summary>
/// Serializer-agnostic primitive wire record for <see cref="Quantity{T}"/>.
/// Contains only primitive fields so any .NET serializer (MessagePack, MemoryPack, Protobuf, YAML, XML, Dapper, EF Core, Parquet) can round-trip it without custom converters.
/// </summary>
public readonly record struct QuantityData<T>(
    T Value,
    string Unit,
    string? Spec = null) where T : INumber<T>;

/// <summary>
/// Serializer-agnostic primitive wire record for <see cref="QuantityPoint{T}"/>.
/// Preserves the affine <see cref="PointOrigin"/> identifier alongside the displacement value and unit.
/// </summary>
public readonly record struct QuantityPointData<T>(
    T Value,
    string Unit,
    string Origin,
    string? Spec = null) where T : INumber<T>;

/// <summary>
/// Serializer-agnostic Structure-of-Arrays wire record for <see cref="QuantitySeries{T}"/>.
/// Stores unit/spec/origin metadata once alongside a contiguous <typeparamref name="T"/> array.
/// </summary>
public readonly record struct QuantitySeriesData<T>(
    T[] Values,
    string Unit,
    string? Spec = null,
    string? Origin = null) where T : INumber<T>;

/// <summary>Serializer-agnostic primitive wire record for <see cref="QuantityVector2{T}"/>.</summary>
public readonly record struct QuantityVector2Data<T>(
    T X,
    T Y,
    string Unit,
    string? Spec = null) where T : INumber<T>;

/// <summary>Serializer-agnostic primitive wire record for <see cref="QuantityVector3{T}"/>.</summary>
public readonly record struct QuantityVector3Data<T>(
    T X,
    T Y,
    T Z,
    string Unit,
    string? Spec = null) where T : INumber<T>;

/// <summary>Serializer-agnostic primitive wire record for <see cref="QuantityPoint2{T}"/>.</summary>
public readonly record struct QuantityPoint2Data<T>(
    T X,
    T Y,
    string Unit,
    string Frame,
    string? Spec = null) where T : INumber<T>;

/// <summary>Serializer-agnostic primitive wire record for <see cref="QuantityPoint3{T}"/>.</summary>
public readonly record struct QuantityPoint3Data<T>(
    T X,
    T Y,
    T Z,
    string Unit,
    string Frame,
    string? Spec = null) where T : INumber<T>;

/// <summary>
/// NativeAOT-safe <see cref="TypeConverter"/> enabling <see cref="Quantity"/> binding in <c>IConfiguration</c>,
/// ASP.NET Core, Blazor, Avalonia, WPF, YAML, and CSV pipelines.
/// </summary>
public sealed class QuantityTypeConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s) return Quantity.Parse(s);
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Quantity q)
            return q.ToString("G", culture ?? CultureInfo.InvariantCulture);
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>
/// NativeAOT-safe <see cref="TypeConverter"/> enabling <see cref="SymbolicUnit"/> binding from strings in configuration and UI frameworks.
/// </summary>
public sealed class SymbolicUnitTypeConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s) return SymbolicUnit.Parse(s, culture);
        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is SymbolicUnit u)
            return u.Symbol;
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

/// <summary>
/// Compact, deterministic, NativeAOT-safe little-endian binary wire codec for SharpUnits types.
/// Uses <see cref="IBufferWriter{T}"/> and <see cref="ReadOnlySpan{T}"/> with bulk <see cref="MemoryMarshal.AsBytes{T}(ReadOnlySpan{T})"/>
/// streaming for high-throughput DAQ/telemetry <see cref="QuantitySeries{T}"/> buffers.
/// </summary>
public static class SharpUnitsBinary
{
    /// <summary>Binary wire format version.</summary>
    public const byte FormatVersion = 1;

    /// <summary>Maximum UTF-8 byte length permitted for any metadata string in an untrusted binary frame.</summary>
    public const int MaxStringBytes = 1024;

    /// <summary>Maximum sample count permitted when decoding an untrusted <see cref="QuantitySeries{T}"/> frame.</summary>
    public const int MaxSeriesCount = 50_000_000;

    private const byte KindQuantity = 1;
    private const byte KindGenericQuantity = 2;
    private const byte KindQuantityPoint = 3;
    private const byte KindQuantitySeries = 4;
    private const byte KindQuantityVector2 = 5;
    private const byte KindQuantityVector3 = 6;

    private const byte TypeDouble = 1;
    private const byte TypeFloat = 2;
    private const byte TypeDecimal = 3;
    private const byte TypeInt32 = 4;
    private const byte TypeInt64 = 5;

    private const byte FlagRelative = 1 << 0;
    private const byte FlagHasSpec = 1 << 1;
    private const byte FlagHasOrigin = 1 << 2;

    // --- Quantity (double) ---

    /// <summary>Writes a <see cref="Quantity"/> in compact little-endian binary format.</summary>
    public static void Write(IBufferWriter<byte> writer, Quantity value, bool asciiSymbol = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (!double.IsFinite(value.Value))
            throw new ArgumentException("Quantity value must be finite for binary serialization.", nameof(value));

        var data = value.ToData(asciiSymbol);
        byte flags = 0;
        if (data.Scale == QuantityScale.Relative) flags |= FlagRelative;
        if (!string.IsNullOrEmpty(data.Spec)) flags |= FlagHasSpec;

        WriteHeader(writer, KindQuantity, TypeDouble, flags);
        WriteUtf8String(writer, data.Unit);
        if ((flags & FlagHasSpec) != 0) WriteUtf8String(writer, data.Spec!);
        WriteDouble(writer, data.Value);
    }

    /// <summary>Serializes a <see cref="Quantity"/> to a newly allocated compact binary byte array.</summary>
    public static byte[] ToBytes(Quantity value, bool asciiSymbol = false)
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        Write(buffer, value, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads a <see cref="Quantity"/> from a binary frame.</summary>
    public static Quantity ReadQuantity(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null)
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindQuantity, TypeDouble, out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        double val = ReadDouble(buffer, ref offset);
        if (!double.IsFinite(val)) throw new FormatException("Non-finite double value in binary Quantity frame.");
        bytesRead = offset;
        var scale = (flags & FlagRelative) != 0 ? QuantityScale.Relative : QuantityScale.Absolute;
        return Quantity.FromData(new QuantityData(val, unit, spec, scale), environment);
    }

    // --- Quantity<T> ---

    /// <summary>Writes a generic <see cref="Quantity{T}"/> (<c>double</c>, <c>float</c>, <c>decimal</c>, <c>int</c>, or <c>long</c>).</summary>
    public static void Write<T>(IBufferWriter<byte> writer, Quantity<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(writer);
        byte typeTag = GetNumericTypeTag<T>();
        ValidateFinite(value.Value);

        var data = value.ToData(asciiSymbol);
        byte flags = string.IsNullOrEmpty(data.Spec) ? (byte)0 : FlagHasSpec;

        WriteHeader(writer, KindGenericQuantity, typeTag, flags);
        WriteUtf8String(writer, data.Unit);
        if ((flags & FlagHasSpec) != 0) WriteUtf8String(writer, data.Spec!);
        WriteScalar(writer, data.Value);
    }

    /// <summary>Serializes a <see cref="Quantity{T}"/> to a compact binary byte array.</summary>
    public static byte[] ToBytes<T>(Quantity<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        Write(buffer, value, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads a generic <see cref="Quantity{T}"/> from a binary frame.</summary>
    public static Quantity<T> ReadQuantity<T>(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null) where T : INumber<T>
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindGenericQuantity, GetNumericTypeTag<T>(), out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        T val = ReadScalar<T>(buffer, ref offset);
        ValidateFinite(val);
        bytesRead = offset;
        return Quantity<T>.FromData(new QuantityData<T>(val, unit, spec), environment);
    }

    // --- QuantityPoint<T> ---

    /// <summary>Writes an affine <see cref="QuantityPoint{T}"/> in compact binary format.</summary>
    public static void Write<T>(IBufferWriter<byte> writer, QuantityPoint<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(writer);
        byte typeTag = GetNumericTypeTag<T>();
        ValidateFinite(value.Value);

        var data = value.ToData(asciiSymbol);
        byte flags = FlagHasOrigin;
        if (!string.IsNullOrEmpty(data.Spec)) flags |= FlagHasSpec;

        WriteHeader(writer, KindQuantityPoint, typeTag, flags);
        WriteUtf8String(writer, data.Unit);
        WriteUtf8String(writer, data.Origin);
        if ((flags & FlagHasSpec) != 0) WriteUtf8String(writer, data.Spec!);
        WriteScalar(writer, data.Value);
    }

    /// <summary>Serializes a <see cref="QuantityPoint{T}"/> to a compact binary byte array.</summary>
    public static byte[] ToBytes<T>(QuantityPoint<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        Write(buffer, value, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads an affine <see cref="QuantityPoint{T}"/> from a binary frame.</summary>
    public static QuantityPoint<T> ReadQuantityPoint<T>(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null) where T : INumber<T>
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindQuantityPoint, GetNumericTypeTag<T>(), out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string origin = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        T val = ReadScalar<T>(buffer, ref offset);
        ValidateFinite(val);
        bytesRead = offset;
        return QuantityPoint<T>.FromData(new QuantityPointData<T>(val, unit, origin, spec), environment);
    }

    // --- QuantitySeries<T> ---

    /// <summary>Writes a Structure-of-Arrays <see cref="QuantitySeries{T}"/> with bulk contiguous binary payload.</summary>
    public static void Write<T>(IBufferWriter<byte> writer, QuantitySeries<T> series, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(series);
        byte typeTag = GetNumericTypeTag<T>();

        string unitSym = asciiSymbol ? series.Unit.AsciiSymbol : series.Unit.Symbol;
        byte flags = 0;
        if (series.Spec is not null) flags |= FlagHasSpec;
        if (series.Origin is not null) flags |= FlagHasOrigin;

        WriteHeader(writer, KindQuantitySeries, typeTag, flags);
        WriteUtf8String(writer, unitSym);
        if (series.Spec is not null) WriteUtf8String(writer, series.Spec.Id);
        if (series.Origin is not null) WriteUtf8String(writer, series.Origin.Id);

        var values = series.Values;
        WriteInt32(writer, values.Length);

        if (values.Length == 0) return;

        if (BitConverter.IsLittleEndian &&
            (typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(int) || typeof(T) == typeof(long)))
        {
            var rawBytes = AsReadOnlyBytes(values);
            var dst = writer.GetSpan(rawBytes.Length);
            rawBytes.CopyTo(dst);
            writer.Advance(rawBytes.Length);
            return;
        }

        for (int i = 0; i < values.Length; i++)
            WriteScalar(writer, values[i]);
    }

    /// <summary>Serializes a <see cref="QuantitySeries{T}"/> to a compact binary byte array.</summary>
    public static byte[] ToBytes<T>(QuantitySeries<T> series, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(series);
        int elemSize = GetElementByteSize<T>();
        var buffer = new ArrayBufferWriter<byte>(64 + series.Count * elemSize);
        Write(buffer, series, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads a <see cref="QuantitySeries{T}"/> from a binary frame.</summary>
    public static QuantitySeries<T> ReadQuantitySeries<T>(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null) where T : INumber<T>
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindQuantitySeries, GetNumericTypeTag<T>(), out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        string? origin = (flags & FlagHasOrigin) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        int count = ReadInt32(buffer, ref offset);
        if (count < 0 || count > MaxSeriesCount)
            throw new FormatException($"Binary QuantitySeries count {count} exceeds allowed limit ({MaxSeriesCount}).");

        var values = new T[count];
        if (count > 0)
        {
            if (BitConverter.IsLittleEndian &&
                (typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(int) || typeof(T) == typeof(long)))
            {
                int byteLen = checked(count * GetElementByteSize<T>());
                EnsureAvailable(buffer, offset, byteLen);
                buffer.Slice(offset, byteLen).CopyTo(AsWritableBytes(values));
                offset += byteLen;
            }
            else
            {
                for (int i = 0; i < count; i++)
                    values[i] = ReadScalar<T>(buffer, ref offset);
            }
        }

        bytesRead = offset;
        return QuantitySeries<T>.FromData(new QuantitySeriesData<T>(values, unit, spec, origin), environment);
    }

    // --- QuantityVector2<T> & QuantityVector3<T> ---

    /// <summary>Writes a 2-D physical vector <see cref="QuantityVector2{T}"/> in compact binary format.</summary>
    public static void Write<T>(IBufferWriter<byte> writer, QuantityVector2<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(writer);
        byte typeTag = GetNumericTypeTag<T>();
        ValidateFinite(value.X);
        ValidateFinite(value.Y);

        var data = value.ToData(asciiSymbol);
        byte flags = string.IsNullOrEmpty(data.Spec) ? (byte)0 : FlagHasSpec;
        WriteHeader(writer, KindQuantityVector2, typeTag, flags);
        WriteUtf8String(writer, data.Unit);
        if ((flags & FlagHasSpec) != 0) WriteUtf8String(writer, data.Spec!);
        WriteScalar(writer, data.X);
        WriteScalar(writer, data.Y);
    }

    /// <summary>Serializes a <see cref="QuantityVector2{T}"/> to a compact binary byte array.</summary>
    public static byte[] ToBytes<T>(QuantityVector2<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        Write(buffer, value, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads a <see cref="QuantityVector2{T}"/> from a compact binary frame.</summary>
    public static QuantityVector2<T> ReadQuantityVector2<T>(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null) where T : INumber<T>
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindQuantityVector2, GetNumericTypeTag<T>(), out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        T x = ReadScalar<T>(buffer, ref offset);
        T y = ReadScalar<T>(buffer, ref offset);
        ValidateFinite(x);
        ValidateFinite(y);
        bytesRead = offset;
        return QuantityVector2<T>.FromData(new QuantityVector2Data<T>(x, y, unit, spec), environment);
    }

    /// <summary>Writes a 3-D physical vector <see cref="QuantityVector3{T}"/> in compact binary format.</summary>
    public static void Write<T>(IBufferWriter<byte> writer, QuantityVector3<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(writer);
        byte typeTag = GetNumericTypeTag<T>();
        ValidateFinite(value.X);
        ValidateFinite(value.Y);
        ValidateFinite(value.Z);

        var data = value.ToData(asciiSymbol);
        byte flags = string.IsNullOrEmpty(data.Spec) ? (byte)0 : FlagHasSpec;
        WriteHeader(writer, KindQuantityVector3, typeTag, flags);
        WriteUtf8String(writer, data.Unit);
        if ((flags & FlagHasSpec) != 0) WriteUtf8String(writer, data.Spec!);
        WriteScalar(writer, data.X);
        WriteScalar(writer, data.Y);
        WriteScalar(writer, data.Z);
    }

    /// <summary>Serializes a <see cref="QuantityVector3{T}"/> to a compact binary byte array.</summary>
    public static byte[] ToBytes<T>(QuantityVector3<T> value, bool asciiSymbol = false) where T : INumber<T>
    {
        var buffer = new ArrayBufferWriter<byte>(64);
        Write(buffer, value, asciiSymbol);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>Reads a <see cref="QuantityVector3{T}"/> from a compact binary frame.</summary>
    public static QuantityVector3<T> ReadQuantityVector3<T>(ReadOnlySpan<byte> buffer, out int bytesRead, UnitEnvironment? environment = null) where T : INumber<T>
    {
        int offset = 0;
        ReadHeader(buffer, ref offset, KindQuantityVector3, GetNumericTypeTag<T>(), out byte flags);
        string unit = ReadUtf8String(buffer, ref offset);
        string? spec = (flags & FlagHasSpec) != 0 ? ReadUtf8String(buffer, ref offset) : null;
        T x = ReadScalar<T>(buffer, ref offset);
        T y = ReadScalar<T>(buffer, ref offset);
        T z = ReadScalar<T>(buffer, ref offset);
        ValidateFinite(x);
        ValidateFinite(y);
        ValidateFinite(z);
        bytesRead = offset;
        return QuantityVector3<T>.FromData(new QuantityVector3Data<T>(x, y, z, unit, spec), environment);
    }

    // --- Low-level helpers ---

    private static void WriteHeader(IBufferWriter<byte> writer, byte kind, byte typeTag, byte flags)
    {
        var span = writer.GetSpan(4);
        span[0] = FormatVersion;
        span[1] = kind;
        span[2] = typeTag;
        span[3] = flags;
        writer.Advance(4);
    }

    private static void ReadHeader(ReadOnlySpan<byte> buffer, ref int offset, byte expectedKind, byte expectedTypeTag, out byte flags)
    {
        EnsureAvailable(buffer, offset, 4);
        byte version = buffer[offset++];
        byte kind = buffer[offset++];
        byte typeTag = buffer[offset++];
        flags = buffer[offset++];

        if (version != FormatVersion)
            throw new FormatException($"Unsupported SharpUnits binary format version {version} (expected {FormatVersion}).");
        if (kind != expectedKind)
            throw new FormatException($"Unexpected SharpUnits binary frame kind {kind} (expected {expectedKind}).");
        if (typeTag != expectedTypeTag)
            throw new FormatException($"Unexpected SharpUnits numeric type tag {typeTag} (expected {expectedTypeTag}).");
    }

    private static void WriteUtf8String(IBufferWriter<byte> writer, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > MaxStringBytes)
            throw new ArgumentException($"Metadata string exceeds {MaxStringBytes} UTF-8 bytes.", nameof(value));
        var span = writer.GetSpan(2 + byteCount);
        BinaryPrimitives.WriteUInt16LittleEndian(span, (ushort)byteCount);
        Encoding.UTF8.GetBytes(value, span.Slice(2, byteCount));
        writer.Advance(2 + byteCount);
    }

    private static string ReadUtf8String(ReadOnlySpan<byte> buffer, ref int offset)
    {
        EnsureAvailable(buffer, offset, 2);
        ushort byteCount = BinaryPrimitives.ReadUInt16LittleEndian(buffer.Slice(offset, 2));
        offset += 2;
        if (byteCount > MaxStringBytes)
            throw new FormatException($"Metadata string length {byteCount} exceeds maximum {MaxStringBytes}.");
        EnsureAvailable(buffer, offset, byteCount);
        string text = Encoding.UTF8.GetString(buffer.Slice(offset, byteCount));
        offset += byteCount;
        return text;
    }

    private static void WriteInt32(IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(4);
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        writer.Advance(4);
    }

    private static int ReadInt32(ReadOnlySpan<byte> buffer, ref int offset)
    {
        EnsureAvailable(buffer, offset, 4);
        int value = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, 4));
        offset += 4;
        return value;
    }

    private static void WriteDouble(IBufferWriter<byte> writer, double value)
    {
        var span = writer.GetSpan(8);
        BinaryPrimitives.WriteDoubleLittleEndian(span, value);
        writer.Advance(8);
    }

    private static double ReadDouble(ReadOnlySpan<byte> buffer, ref int offset)
    {
        EnsureAvailable(buffer, offset, 8);
        double value = BinaryPrimitives.ReadDoubleLittleEndian(buffer.Slice(offset, 8));
        offset += 8;
        return value;
    }

    private static void WriteScalar<T>(IBufferWriter<byte> writer, T value) where T : INumber<T>
    {
        if (typeof(T) == typeof(double))
        {
            WriteDouble(writer, (double)(object)value);
            return;
        }
        if (typeof(T) == typeof(float))
        {
            var span = writer.GetSpan(4);
            BinaryPrimitives.WriteSingleLittleEndian(span, (float)(object)value);
            writer.Advance(4);
            return;
        }
        if (typeof(T) == typeof(int))
        {
            WriteInt32(writer, (int)(object)value);
            return;
        }
        if (typeof(T) == typeof(long))
        {
            var span = writer.GetSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(span, (long)(object)value);
            writer.Advance(8);
            return;
        }
        if (typeof(T) == typeof(decimal))
        {
            Span<int> bits = stackalloc int[4];
            decimal.GetBits((decimal)(object)value, bits);
            var span = writer.GetSpan(16);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0, 4), bits[0]);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4, 4), bits[1]);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8, 4), bits[2]);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(12, 4), bits[3]);
            writer.Advance(16);
            return;
        }
        throw new NotSupportedException($"Binary serialization is not supported for numeric type '{typeof(T).Name}'.");
    }

    private static T ReadScalar<T>(ReadOnlySpan<byte> buffer, ref int offset) where T : INumber<T>
    {
        if (typeof(T) == typeof(double))
            return (T)(object)ReadDouble(buffer, ref offset);
        if (typeof(T) == typeof(float))
        {
            EnsureAvailable(buffer, offset, 4);
            float v = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset, 4));
            offset += 4;
            return (T)(object)v;
        }
        if (typeof(T) == typeof(int))
            return (T)(object)ReadInt32(buffer, ref offset);
        if (typeof(T) == typeof(long))
        {
            EnsureAvailable(buffer, offset, 8);
            long v = BinaryPrimitives.ReadInt64LittleEndian(buffer.Slice(offset, 8));
            offset += 8;
            return (T)(object)v;
        }
        if (typeof(T) == typeof(decimal))
        {
            EnsureAvailable(buffer, offset, 16);
            Span<int> bits = stackalloc int[4];
            bits[0] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset, 4));
            bits[1] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 4, 4));
            bits[2] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 8, 4));
            bits[3] = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset + 12, 4));
            offset += 16;
            return (T)(object)new decimal(bits);
        }
        throw new NotSupportedException($"Binary deserialization is not supported for numeric type '{typeof(T).Name}'.");
    }

    private static byte GetNumericTypeTag<T>() where T : INumber<T>
    {
        if (typeof(T) == typeof(double)) return TypeDouble;
        if (typeof(T) == typeof(float)) return TypeFloat;
        if (typeof(T) == typeof(decimal)) return TypeDecimal;
        if (typeof(T) == typeof(int)) return TypeInt32;
        if (typeof(T) == typeof(long)) return TypeInt64;
        throw new NotSupportedException($"Binary framing supports double, float, decimal, int, and long; got '{typeof(T).Name}'.");
    }

    private static int GetElementByteSize<T>() where T : INumber<T>
    {
        if (typeof(T) == typeof(double) || typeof(T) == typeof(long)) return 8;
        if (typeof(T) == typeof(float) || typeof(T) == typeof(int)) return 4;
        if (typeof(T) == typeof(decimal)) return 16;
        throw new NotSupportedException($"Unsupported numeric type '{typeof(T).Name}'.");
    }

    private static void ValidateFinite<T>(T value) where T : INumber<T>
    {
        if (typeof(T) == typeof(double) && !double.IsFinite((double)(object)value))
            throw new FormatException("Non-finite double value is not permitted in binary serialization.");
        if (typeof(T) == typeof(float) && !float.IsFinite((float)(object)value))
            throw new FormatException("Non-finite float value is not permitted in binary serialization.");
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> buffer, int offset, int count)
    {
        if (offset < 0 || count < 0 || buffer.Length - offset < count)
            throw new FormatException("Unexpected end of SharpUnits binary frame.");
    }

    private static ReadOnlySpan<byte> AsReadOnlyBytes<T>(ReadOnlySpan<T> span) where T : INumber<T>
    {
        int elemSize = GetElementByteSize<T>();
        return MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
            checked(span.Length * elemSize));
    }

    private static Span<byte> AsWritableBytes<T>(Span<T> span) where T : INumber<T>
    {
        int elemSize = GetElementByteSize<T>();
        return MemoryMarshal.CreateSpan(
            ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
            checked(span.Length * elemSize));
    }
}
