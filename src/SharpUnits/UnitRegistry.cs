using System.Collections.Concurrent;
namespace SharpUnits;
/// <summary>Thread-safe registry for unit symbols and compatible conversion.</summary>
public sealed class UnitRegistry
{
 private readonly ConcurrentDictionary<string,UnitDefinition> units=new(StringComparer.Ordinal);
 /// <summary>Registers a symbol; rejects collisions.</summary>
 public bool Register(UnitDefinition unit)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(string.IsNullOrWhiteSpace(unit.Symbol)) throw new ArgumentException("Unit symbol required",nameof(unit));
  return units.TryAdd(unit.Symbol,unit);
 }
 /// <summary>Resolves an exact symbol.</summary>
 public UnitDefinition Resolve(string symbol)=>units.TryGetValue(symbol,out var unit)?unit:throw new KeyNotFoundException($"Unknown unit: {symbol}");
 /// <summary>Converts between dimensionally compatible registered units.</summary>
 public double Convert(double value,string from,string to)
 {
  var source=Resolve(from);var target=Resolve(to);
  if(source.Dimension!=target.Dimension)throw new InvalidOperationException("Incompatible dimensions");
  return target.Converter.FromSi(source.Converter.ToSi(value));
 }
 /// <summary>Creates an SI registry containing electrical and temperature units.</summary>
 public static UnitRegistry CreateStandard()
 {
  var r=new UnitRegistry();
  var temp=new Dimension(0,0,0,0,1,0,0);
  foreach(var u in new[]{
   new UnitDefinition("A",Dimension.ElectricCurrent,UnitConversion.Linear(1)),
   new UnitDefinition("mA",Dimension.ElectricCurrent,UnitConversion.Linear(.001)),
   new UnitDefinition("V",Dimension.Voltage,UnitConversion.Linear(1)),
   new UnitDefinition("mV",Dimension.Voltage,UnitConversion.Linear(.001)),
   new UnitDefinition("Ω",Dimension.Resistance,UnitConversion.Linear(1)),
   new UnitDefinition("kΩ",Dimension.Resistance,UnitConversion.Linear(1000)),
   new UnitDefinition("K",temp,UnitConversion.Linear(1)),
   new UnitDefinition("°C",temp,UnitConversion.Celsius),
   new UnitDefinition("°F",temp,UnitConversion.Fahrenheit)
  })r.Register(u);
  return r;
 }
}