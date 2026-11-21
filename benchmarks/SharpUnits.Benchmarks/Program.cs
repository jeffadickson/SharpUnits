using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using SharpUnits;

Console.WriteLine($"SharpUnits Benchmarks (.NET {Environment.Version}, Vector<double>.Count={Vector<double>.Count}, SIMD={QuantitySeries<double>.IsSimdAccelerated})");
Console.WriteLine(new string('-', 88));

const int N = 1_000_000;
var raw = new double[N];
for (int i = 0; i < N; i++) raw[i] = Math.Sin(i * 0.001) * 0.25;
var series = QuantitySeries<double>.FromValues(raw, Units.Volt);

var dst = new double[N];

// Warmup
series.CopyConvertedTo(Units.Millivolt, dst);
_ = series.Rms();

Measure("QuantitySeries.CopyConvertedTo [1M SIMD]", 40, () =>
{
    series.CopyConvertedTo(Units.Millivolt, dst);
    return dst[^1];
});

Measure("Raw double[] scalar scale      [1M]", 40, () =>
{
    for (int i = 0; i < N; i++) dst[i] = raw[i] * 1000.0;
    return dst[^1];
});

Measure("QuantitySeries<double>.Rms()   [1M SIMD]", 40, () => series.Rms().Value);

Measure("Raw double[] scalar RMS        [1M]", 40, () =>
{
    double sum = 0;
    for (int i = 0; i < N; i++) sum += raw[i] * raw[i];
    return Math.Sqrt(sum / N);
});

// Zero-allocation ISpanFormattable tick formatting
var tickQty = Quantity.Of(0.0025, Units.Volt);
Span<char> scratch = stackalloc char[32];
tickQty.TryFormat(scratch, out _, "G4", CultureInfo.InvariantCulture); // JIT & culture warmup
long allocBefore = GC.GetAllocatedBytesForCurrentThread();
int totalChars = 0;
for (int i = 0; i < 100_000; i++)
{
    tickQty.TryFormat(scratch, out int written, "G4", CultureInfo.InvariantCulture);
    totalChars += written;
}
long allocAfter = GC.GetAllocatedBytesForCurrentThread();
Console.WriteLine($"{"Quantity.TryFormat (100k ticks)",-42} | allocated: {allocAfter - allocBefore} bytes (chars={totalChars})");

// JSON SoA series round-trip (10k samples)
var jsonOpts = new JsonSerializerOptions().AddSharpUnits();
var smallSeries = QuantitySeries<double>.FromValues(raw.AsSpan(0, 10_000), Units.Volt).AutoScale();
Measure("STJ QuantitySeries round-trip  [10k pts]", 10, () =>
{
    string json = JsonSerializer.Serialize(smallSeries, jsonOpts);
    var back = JsonSerializer.Deserialize<QuantitySeries<double>>(json, jsonOpts)!;
    return back.Count;
});

static void Measure(string name, int iterations, Func<double> action)
{
    // Warmup
    double sink = action();
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < iterations; i++) sink += action();
    sw.Stop();
    double ms = sw.Elapsed.TotalMilliseconds / iterations;
    Console.WriteLine($"{name,-42} | {ms,8:F3} ms/op (sink={sink:G4})");
}
