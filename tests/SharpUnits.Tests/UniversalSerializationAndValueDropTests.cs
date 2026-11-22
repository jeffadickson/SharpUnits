using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests value drop-down helpers, serializer-agnostic DTOs, platform contracts (TypeConverter/UTF-8/Span), and compact binary framing.</summary>
public sealed class UniversalSerializationAndValueDropTests
{
    [Fact]
    public void ValueDropDownAndDeconstructWorkAcrossAllTypes()
    {
        var q = Quantity.Of(250, Units.Millivolt);
        Assert.Equal(0.25, q.ValueIn(Units.Volt), 12);
        Assert.Equal(250, q.ValueIn("mV"), 12);
        Assert.Equal(q, Quantity.FromValueIn(250, "mV"));
        Assert.Equal(q, Quantity.FromSiValue(0.25, Units.Millivolt));

        var (qVal, qSym) = q;
        Assert.Equal(250, qVal, 12);
        Assert.Equal("mV", qSym);

        var (qVal2, qUnit, qSpec) = q;
        Assert.Equal(250, qVal2, 12);
        Assert.Equal(Units.Millivolt, qUnit);
        Assert.Equal(Isq.Voltage, qSpec);

        var gq = Quantity<decimal>.Of(12.5m, Units.Millivolt, Isq.Voltage);
        Assert.Equal(0.0125m, gq.ValueIn(Units.Volt));
        Assert.Equal(gq, Quantity<decimal>.FromValueIn(12.5m, Units.Millivolt, Isq.Voltage));
        var (gVal, gSymUnit) = gq;
        Assert.Equal(12.5m, gVal);
        Assert.Equal(Units.Millivolt, gSymUnit);

        var pt = QuantityPoint<double>.FromGaugePressure(2.0, Units.Bar);
        Assert.Equal(3.01325, pt.ValueIn(Units.Bar, PointOrigins.Vacuum), 5);
        var (pVal, pUnit, pOrigin) = pt;
        Assert.Equal(2.0, pVal, 12);
        Assert.Equal(Units.Bar, pUnit);
        Assert.Equal(PointOrigins.StandardAtmosphere, pOrigin);

        var series = QuantitySeries<double>.FromValues([0.1, 0.25, 0.5], Units.Volt);
        double[] mv = series.ToArrayIn("mV");
        Assert.Equal([100.0, 250.0, 500.0], mv);
    }

    [Fact]
    public void SerializerAgnosticDtosRoundTripWithPlainDefaultJsonSerializer()
    {
        // Intentionally uses default JsonSerializerOptions with ZERO SharpUnits converters registered!
        // Proves QuantityData / QuantityPointData / QuantitySeriesData work with any POCO serializer (MessagePack, MemoryPack, Protobuf, YAML, XML).
        var q = Quantity.Of(12.5, Units.Millivolt, spec: Isq.Voltage);
        string qJson = JsonSerializer.Serialize(q.ToData());
        var qBack = Quantity.FromData(JsonSerializer.Deserialize<QuantityData>(qJson));
        Assert.Equal(12.5, qBack.Value, 12);
        Assert.Equal(Units.Millivolt, qBack.Unit);
        Assert.Equal(Isq.Voltage, qBack.Spec);

        var gq = Quantity<decimal>.Of(0.125m, Units.Millivolt, Isq.Voltage);
        string gqJson = JsonSerializer.Serialize(gq.ToData());
        var gqBack = Quantity<decimal>.FromData(JsonSerializer.Deserialize<QuantityData<decimal>>(gqJson));
        Assert.Equal(gq, gqBack);

        var pt = QuantityPoint<double>.FromCelsius(23.5);
        string ptJson = JsonSerializer.Serialize(pt.ToData());
        var ptBack = QuantityPoint<double>.FromData(JsonSerializer.Deserialize<QuantityPointData<double>>(ptJson));
        Assert.Equal(23.5, ptBack.Value, 12);
        Assert.Equal(PointOrigins.IcePoint, ptBack.Origin);

        var series = QuantitySeries<float>.FromGaugePressure([1.0f, 2.0f, 3.5f], Units.Bar);
        string sJson = JsonSerializer.Serialize(series.ToData());
        var sBack = QuantitySeries<float>.FromData(JsonSerializer.Deserialize<QuantitySeriesData<float>>(sJson));
        Assert.Equal([1.0f, 2.0f, 3.5f], sBack.Values.ToArray());
        Assert.Equal(Units.Bar, sBack.Unit);
        Assert.Equal(PointOrigins.StandardAtmosphere, sBack.Origin);
    }

    [Fact]
    public void PlatformContractsSpanUtf8AndTypeConvertersWork()
    {
        // ISpanParsable + IUtf8SpanFormattable + IUtf8SpanParsable on Quantity
        var q = Quantity.Parse("2.5 mV".AsSpan(), CultureInfo.InvariantCulture);
        Assert.Equal(0.0025, q.SiValue, 12);

        Span<byte> utf8 = stackalloc byte[32];
        Assert.True(q.TryFormat(utf8, out int written, "eng", CultureInfo.InvariantCulture));
        string formatted = Encoding.UTF8.GetString(utf8[..written]);
        Assert.Equal("2.5 mV", formatted);

        Assert.True(Quantity.TryParse(utf8[..written], CultureInfo.InvariantCulture, out var parsedFromUtf8));
        Assert.True(q.IsEquivalentTo(parsedFromUtf8, 1e-12));

        // TypeConverter on Quantity (IConfiguration / Blazor / Avalonia / ASP.NET binding)
        var qConv = TypeDescriptor.GetConverter(typeof(Quantity));
        Assert.True(qConv.CanConvertFrom(typeof(string)));
        var converted = (Quantity)qConv.ConvertFromString("4.7 kOhm")!;
        Assert.Equal(4700.0, converted.SiValue, 9);
        Assert.Equal("4.7 kΩ", qConv.ConvertToString(null, CultureInfo.InvariantCulture, converted));

        // ISpanParsable + IUtf8SpanFormattable + TypeConverter on SymbolicUnit
        var u = SymbolicUnit.Parse("m/s^2".AsSpan(), CultureInfo.InvariantCulture);
        Assert.Equal(Units.Meter / Units.Second.Pow(2), u);

        Span<byte> uUtf8 = stackalloc byte[32];
        Assert.True(u.TryFormat(uUtf8, out int uLen, "ascii", CultureInfo.InvariantCulture));
        Assert.Equal("m/s^2", Encoding.UTF8.GetString(uUtf8[..uLen]));
        Assert.True(SymbolicUnit.TryParse(uUtf8[..uLen], CultureInfo.InvariantCulture, out var uBack));
        Assert.Equal(u, uBack);

        var uConv = TypeDescriptor.GetConverter(typeof(SymbolicUnit));
        Assert.Equal(Units.Millivolt, (SymbolicUnit)uConv.ConvertFromString("mV")!);
    }

    [Fact]
    public void BinaryFramingRoundTripsAllTypesAndStreamsSeriesWithMinimalOverhead()
    {
        var q = Quantity.Of(3.3, Units.Volt, spec: Isq.Voltage);
        byte[] qBytes = SharpUnitsBinary.ToBytes(q);
        var qBack = SharpUnitsBinary.ReadQuantity(qBytes, out int qRead);
        Assert.Equal(qBytes.Length, qRead);
        Assert.True(q.IsEquivalentTo(qBack, 1e-12));
        Assert.Equal(Isq.Voltage, qBack.Spec);

        var decQ = Quantity<decimal>.Of(123.456789m, Units.Millivolt, Isq.Voltage);
        byte[] decBytes = SharpUnitsBinary.ToBytes(decQ);
        Assert.Equal(decQ, SharpUnitsBinary.ReadQuantity<decimal>(decBytes, out _));

        var intQ = Quantity<int>.Of(1500, Units.Millivolt);
        Assert.Equal(intQ, SharpUnitsBinary.ReadQuantity<int>(SharpUnitsBinary.ToBytes(intQ), out _));

        var longQ = Quantity<long>.Of(42_000_000L, Units.Hertz);
        Assert.Equal(longQ, SharpUnitsBinary.ReadQuantity<long>(SharpUnitsBinary.ToBytes(longQ), out _));

        var pt = QuantityPoint<double>.FromCelsius(36.6);
        byte[] ptBytes = SharpUnitsBinary.ToBytes(pt);
        var ptBack = SharpUnitsBinary.ReadQuantityPoint<double>(ptBytes, out _);
        Assert.Equal(36.6, ptBack.Value, 12);
        Assert.Equal(PointOrigins.IcePoint, ptBack.Origin);

        // Bulk 10k-sample waveform streaming via IBufferWriter<byte>
        var samples = new double[10_000];
        for (int i = 0; i < samples.Length; i++) samples[i] = Math.Sin(i * 0.01) * 5.0;
        var series = QuantitySeries<double>.FromValues(samples, Units.Millivolt, Isq.Voltage);
        var writer = new ArrayBufferWriter<byte>(samples.Length * 8 + 64);
        SharpUnitsBinary.Write(writer, series);

        // Header overhead for 80,000-byte payload should be tiny (< 64 bytes)
        Assert.True(writer.WrittenCount - samples.Length * 8 < 64);

        var seriesBack = SharpUnitsBinary.ReadQuantitySeries<double>(writer.WrittenSpan, out int sRead);
        Assert.Equal(writer.WrittenCount, sRead);
        Assert.Equal(series.Unit, seriesBack.Unit);
        Assert.Equal(series.Spec, seriesBack.Spec);
        Assert.Equal(samples, seriesBack.Values.ToArray());

        // Affine Celsius decimal series binary round-trip
        var cSeries = QuantitySeries<decimal>.FromCelsius([20.1m, 21.5m, 22.9m]);
        var cBack = SharpUnitsBinary.ReadQuantitySeries<decimal>(SharpUnitsBinary.ToBytes(cSeries), out _);
        Assert.Equal(PointOrigins.IcePoint, cBack.Origin);
        Assert.Equal([20.1m, 21.5m, 22.9m], cBack.Values.ToArray());

        // Rejects truncated or bad-version frames cleanly
        Assert.Throws<FormatException>(() => SharpUnitsBinary.ReadQuantity(qBytes.AsSpan(0, 3), out _));
        byte[] badVer = (byte[])qBytes.Clone();
        badVer[0] = 99;
        Assert.Throws<FormatException>(() => SharpUnitsBinary.ReadQuantity(badVer, out _));
        Assert.Throws<ArgumentException>(() => SharpUnitsBinary.ToBytes(Quantity.Of(double.NaN, Units.Volt)));
    }
}
