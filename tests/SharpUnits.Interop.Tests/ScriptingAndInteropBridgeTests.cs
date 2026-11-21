using System.Numerics;
using SharpUnits;
using SharpUnits.Interop.Lua;
using SharpUnits.Interop.Native;
using SharpUnits.Interop.Starlark;
using Xunit;

namespace SharpUnits.Interop.Tests;

/// <summary>Tests for StarLark-cs host values, Teal/Lua bridge, and NativeAOT handle table.</summary>
public sealed class ScriptingAndInteropBridgeTests
{
    [Fact]
    public void StarlarkHostValuesSupportBigIntegerReflectedOpsAffinePointsAndSeries()
    {
        long budgetedBytes = 0;
        var units = new StarUnitsModule(onAllocateBytes: bytes => budgetedBytes += bytes);

        // Simulate Starlark: 250 * unit("mA") * (4 * unit("kOhm"))
        var mA = units.Unit("mA");
        var kOhm = units.Unit("kOhm");

        Assert.True(mA.TryBinary(StarBinOp.Multiply, new BigInteger(250), reflected: true, out var currentObj));
        Assert.True(kOhm.TryBinary(StarBinOp.Multiply, new BigInteger(4), reflected: true, out var resObj));

        var current = Assert.IsType<StarQuantityObject>(currentObj);
        var res = Assert.IsType<StarQuantityObject>(resObj);
        Assert.True(current.TryBinary(StarBinOp.Multiply, res, reflected: false, out var voltObj));

        var voltage = Assert.IsType<StarQuantityObject>(voltObj);
        var inKv = voltage.To("kV");
        Assert.Equal(1.0, inKv.Quantity.Value, 12);

        // Consistent physical equality and hashing across units
        var oneVolt = units.Qty(new BigInteger(1), "V");
        var thousandMv = units.Qty(new BigInteger(1000), "mV");
        Assert.True(oneVolt.EqualsValue(thousandMv));
        Assert.Equal(oneVolt.HashValue(), thousandMv.HashValue());

        // Affine points: point - point works, point + point is rejected
        var t1 = units.Point(25.0, "°C");
        var t2 = units.Point(212.0, "degF");
        Assert.False(t1.TryBinary(StarBinOp.Add, t2, reflected: false, out _));
        Assert.True(t2.TryBinary(StarBinOp.Subtract, t1, reflected: false, out var deltaObj));
        var delta = Assert.IsType<StarQuantityObject>(deltaObj);
        Assert.Equal(75.0, delta.To("K").Quantity.Value, 9);

        // Series with MemoryBudget callback, negative Python indexing, iteration, auto_scale
        var series = units.Series([new BigInteger(0), 0.125, 0.25], "V");
        Assert.Equal(3 * sizeof(double), budgetedBytes);
        Assert.Equal(new BigInteger(3), series.Length());

        Assert.True(series.TryIndex(new BigInteger(-1), out var lastObj));
        Assert.Equal(0.25, Assert.IsType<StarQuantityObject>(lastObj).Quantity.Value, 12);
        Assert.Equal(3, series.Iterate().Count());

        Assert.True(series.TryGetAttribute("auto_scale", out var autoFn));
        var scaled = ((Func<StarQuantitySeriesObject>)autoFn!)();
        Assert.Equal("mV", scaled.Series.Unit.Symbol);
    }

    [Fact]
    public void LuaTealBridgeUsesOneBasedIndexingAndMetamethodSemantics()
    {
        var lua = new LuaUnitsBridge();

        var i = lua.Qty(250, "mA");
        var r = lua.Qty(4, "kOhm");
        var v = lua.InUnit((Quantity<double>)lua.Mul(i, r), "kV");
        Assert.Equal(1.0, v.Value, 12);
        Assert.True(lua.Eq(v, lua.Qty(1000, "V")));
        Assert.True(lua.Lt(lua.Qty(500, "mV"), v));

        var gauge = lua.Point(2.0, "bar", "gauge");
        var abs = lua.PointRelativeTo(gauge, "vacuum");
        Assert.Equal(3.01325, abs.Value, 9);

        var trace = lua.Series([-0.05, 0.0, 0.25], "V").AutoScale();
        Assert.Equal("mV", trace.Unit.Symbol);
        Assert.Equal(-50.0, lua.SeriesAt(trace, 1).Value, 9);
        Assert.Equal(250.0, lua.SeriesAt(trace, 3).Value, 9);
        Assert.Throws<ArgumentOutOfRangeException>(() => lua.SeriesAt(trace, 0));
    }

    [Fact]
    public void NativeHandleTableSupportsFullLifecycleErrorReportingAndZeroLeaks()
    {
        var api = new NativeUnitHandleTable();
        var allocated = new List<long>();
        long Track(long h) { Assert.NotEqual(0, h); allocated.Add(h); return h; }

        long env = Track(api.CreateEnvironment());
        long i = Track(api.CreateQuantity(env, 250, "mA"));
        long r = Track(api.CreateQuantity(env, 4, "kOhm"));
        long v = Track(api.QuantityMul(i, r));
        long vKv = Track(api.QuantityIn(env, v, "kV"));
        Assert.Equal(1.0, api.QuantityGetValue(vKv), 12);
        Assert.Equal("1 kV", api.QuantityFormat(vKv, "eng"));

        // Invalid addition sets LastError and returns 0 handle
        Assert.Equal(0, api.QuantityAdd(i, r));
        Assert.NotEmpty(api.LastError);

        // Affine points across origins
        long pGauge = Track(api.CreatePoint(env, 2.0, "bar", "gauge"));
        long pAbs = Track(api.PointRelativeTo(env, pGauge, "vacuum"));
        Assert.Equal(3.01325, api.PointGetValue(pAbs), 9);

        // SIMD series through handles
        long s = Track(api.CreateSeries(env, [-0.05, 0.0, 0.125, 0.25], "V"));
        long sScaled = Track(api.SeriesAutoScale(s));
        Assert.Equal("mV", api.SeriesGetUnitSymbol(sScaled));
        long rms = Track(api.SeriesRms(sScaled));
        Assert.True(api.QuantityGetValue(rms) > 0);

        foreach (long h in allocated) Assert.True(api.FreeHandle(h));
        Assert.Equal(0, api.ActiveHandleCount);
    }
}
