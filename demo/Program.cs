using System.Globalization;
using SharpUnits;
// Export independent numerical evidence from the actual SharpUnits implementation.
var r=new Resistance(1000);
var c=new Capacitance(1e-6);
var l=new Inductance(.01);
var rlc=new SeriesRlcCircuit(new Resistance(10),l,c);
Console.WriteLine("model,hertz,magnitude_db,phase_degrees");
foreach(double f in BodeFrequencyResponse.LogarithmicFrequencies(10,100000,501))
{
 var points=new (string Name,BodePoint Point)[]
 {
  ("rc-low",BodeFrequencyResponse.RcLowPass(r,c,f)),
  ("rc-high",BodeFrequencyResponse.RcHighPass(r,c,f)),
  ("rl-low",BodeFrequencyResponse.RlLowPass(r,l,f)),
  ("rl-high",BodeFrequencyResponse.RlHighPass(r,l,f)),
  ("rlc-band",BodeFrequencyResponse.RlcBandPass(rlc,f))
 };
 foreach(var entry in points)
  Console.WriteLine(string.Join(",",entry.Name,
   f.ToString("R",CultureInfo.InvariantCulture),
   entry.Point.MagnitudeDecibels.ToString("R",CultureInfo.InvariantCulture),
   entry.Point.PhaseDegrees.ToString("R",CultureInfo.InvariantCulture)));
}
