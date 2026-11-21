using System.Globalization;
using SharpUnits.Electrical;

// Export independent numerical evidence from the actual SharpUnits.Electrical implementation.
var r = Resistance.FromKiloohms(1);
var c = Capacitance.FromMicrofarads(1);
var l = Inductance.FromMillihenries(10);
var rlc = new SeriesRlcCircuit(new Resistance(10), l, c);
Console.WriteLine("model,hertz,magnitude_db,phase_degrees");
foreach (double f in BodeFrequencyResponse.LogarithmicFrequencies(10, 100000, 501))
{
    var points = new (string Name, BodePoint Point)[]
    {
        ("rc-low", BodeFrequencyResponse.RcLowPass(r, c, f)),
        ("rc-high", BodeFrequencyResponse.RcHighPass(r, c, f)),
        ("rl-low", BodeFrequencyResponse.RlLowPass(r, l, f)),
        ("rl-high", BodeFrequencyResponse.RlHighPass(r, l, f)),
        ("rlc-band", BodeFrequencyResponse.RlcBandPass(rlc, f))
    };
    foreach (var entry in points)
        Console.WriteLine(string.Join(",", entry.Name,
            f.ToString("R", CultureInfo.InvariantCulture),
            entry.Point.MagnitudeDecibels.ToString("R", CultureInfo.InvariantCulture),
            entry.Point.PhaseDegrees.ToString("R", CultureInfo.InvariantCulture)));
}
