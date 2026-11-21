using System.Collections.Concurrent;
using System.Globalization;

namespace SharpUnits.Interop.Native;

/// <summary>
/// Thread-safe opaque handle table for NativeAOT C-ABI / LuaJIT FFI / Lua C-module hosting of SharpUnits.
/// Every returned non-zero handle must be released with <see cref="FreeHandle"/> (for example via a Lua <c>__gc</c> metamethod).
/// </summary>
public sealed class NativeUnitHandleTable
{
    private long nextHandle;
    private readonly ConcurrentDictionary<long, object> handles = new();
    private readonly ThreadLocal<string> lastError = new(() => string.Empty);

    /// <summary>Number of currently live handles (useful for leak detection in host tests).</summary>
    public int ActiveHandleCount => handles.Count;

    /// <summary>Last error message recorded on the calling thread.</summary>
    public string LastError => lastError.Value ?? string.Empty;

    /// <summary>Allocates a fresh standard <see cref="UnitEnvironment"/> and returns its opaque handle.</summary>
    public long CreateEnvironment() => Alloc(UnitEnvironment.CreateStandard());

    /// <summary>Releases any handle previously allocated by this table.</summary>
    public bool FreeHandle(long handle) => handles.TryRemove(handle, out _);

    /// <summary>Resolves a unit expression in <paramref name="envHandle"/>; returns 0 on error.</summary>
    public long ResolveUnit(long envHandle, string expression) => Guard(() =>
        Alloc(Get<UnitEnvironment>(envHandle).ResolveUnit(expression)));

    /// <summary>Creates a <see cref="Quantity{T}"/> handle; returns 0 on error.</summary>
    public long CreateQuantity(long envHandle, double value, string unitExpression) => Guard(() =>
        Alloc(Get<UnitEnvironment>(envHandle).CreateQuantity(value, unitExpression)));

    /// <summary>Converts a quantity handle to <paramref name="targetUnit"/>; returns 0 on error.</summary>
    public long QuantityIn(long envHandle, long qtyHandle, string targetUnit) => Guard(() =>
        Alloc(Get<UnitEnvironment>(envHandle).Convert(Get<Quantity<double>>(qtyHandle), targetUnit)));

    /// <summary>Adds two quantity handles; returns 0 on error.</summary>
    public long QuantityAdd(long aHandle, long bHandle) => Guard(() =>
        Alloc(Get<Quantity<double>>(aHandle) + Get<Quantity<double>>(bHandle)));

    /// <summary>Subtracts two quantity handles; returns 0 on error.</summary>
    public long QuantitySub(long aHandle, long bHandle) => Guard(() =>
        Alloc(Get<Quantity<double>>(aHandle) - Get<Quantity<double>>(bHandle)));

    /// <summary>Multiplies two quantity handles; returns 0 on error.</summary>
    public long QuantityMul(long aHandle, long bHandle) => Guard(() =>
        Alloc(Get<Quantity<double>>(aHandle) * Get<Quantity<double>>(bHandle)));

    /// <summary>Divides two quantity handles; returns 0 on error.</summary>
    public long QuantityDiv(long aHandle, long bHandle) => Guard(() =>
        Alloc(Get<Quantity<double>>(aHandle) / Get<Quantity<double>>(bHandle)));

    /// <summary>Reads the numeric value in the quantity's current display unit.</summary>
    public double QuantityGetValue(long qtyHandle) => Get<Quantity<double>>(qtyHandle).Value;

    /// <summary>Formats a quantity handle using invariant culture and optional format specifier.</summary>
    public string QuantityFormat(long qtyHandle, string? format = null) =>
        Get<Quantity<double>>(qtyHandle).ToQuantity().ToString(format, CultureInfo.InvariantCulture);

    /// <summary>Creates an affine <see cref="QuantityPoint{T}"/> handle; returns 0 on error.</summary>
    public long CreatePoint(long envHandle, double value, string unitExpression, string? originName = null) => Guard(() =>
        Alloc(Get<UnitEnvironment>(envHandle).CreatePoint(value, unitExpression, originName)));

    /// <summary>Subtracts two affine point handles, returning a displacement quantity handle; returns 0 on error.</summary>
    public long PointSub(long aHandle, long bHandle) => Guard(() =>
        Alloc(Get<QuantityPoint<double>>(aHandle) - Get<QuantityPoint<double>>(bHandle)));

    /// <summary>Re-anchors an affine point handle to a named origin; returns 0 on error.</summary>
    public long PointRelativeTo(long envHandle, long pointHandle, string originName) => Guard(() =>
    {
        var env = Get<UnitEnvironment>(envHandle);
        return Alloc(Get<QuantityPoint<double>>(pointHandle).RelativeTo(env.ResolveOrigin(originName)));
    });

    /// <summary>Reads the numeric value of an affine point relative to its origin.</summary>
    public double PointGetValue(long pointHandle) => Get<QuantityPoint<double>>(pointHandle).Value;

    /// <summary>Creates a SIMD-backed <see cref="QuantitySeries{T}"/> handle from raw samples; returns 0 on error.</summary>
    public long CreateSeries(long envHandle, ReadOnlySpan<double> samples, string unitExpression)
    {
        try
        {
            var env = Get<UnitEnvironment>(envHandle);
            var series = QuantitySeries<double>.FromValues(samples, env.ResolveUnit(unitExpression));
            ClearError();
            return Alloc(series);
        }
        catch (Exception ex)
        {
            lastError.Value = ex.Message;
            return 0;
        }
    }

    /// <summary>Auto-scales a series handle to its optimal engineering SI prefix; returns 0 on error.</summary>
    public long SeriesAutoScale(long seriesHandle) => Guard(() =>
        Alloc(Get<QuantitySeries<double>>(seriesHandle).AutoScale()));

    /// <summary>Computes the RMS quantity handle of a series; returns 0 on error.</summary>
    public long SeriesRms(long seriesHandle) => Guard(() =>
        Alloc(Get<QuantitySeries<double>>(seriesHandle).Rms()));

    /// <summary>Returns the unit symbol of a series handle.</summary>
    public string SeriesGetUnitSymbol(long seriesHandle) =>
        Get<QuantitySeries<double>>(seriesHandle).Unit.Symbol;

    private long Alloc(object instance)
    {
        long id = Interlocked.Increment(ref nextHandle);
        handles[id] = instance;
        return id;
    }

    private T Get<T>(long handle)
    {
        if (!handles.TryGetValue(handle, out var obj))
            throw new KeyNotFoundException($"Invalid or freed SharpUnits handle: {handle}.");
        if (obj is not T typed)
            throw new InvalidOperationException($"Handle {handle} holds {obj.GetType().Name}, expected {typeof(T).Name}.");
        return typed;
    }

    private long Guard(Func<long> action)
    {
        try
        {
            long handle = action();
            ClearError();
            return handle;
        }
        catch (Exception ex)
        {
            lastError.Value = ex.Message;
            return 0;
        }
    }

    private void ClearError() => lastError.Value = string.Empty;
}
