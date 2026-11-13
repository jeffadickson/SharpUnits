using System.Collections.Frozen;
using System.Collections.Immutable;

namespace SharpUnits;
/// <summary>SI decimal prefixes used to derive linear units without changing their dimensions.</summary>
public static class SiPrefixes
{
 private static readonly FrozenDictionary<string,int> Exponents;
 private static readonly ImmutableArray<KeyValuePair<string,int>> OrderedDefinitions;
 static SiPrefixes()
 {
 var values=new OrderedDictionary<string,int>(StringComparer.Ordinal)
 {
  ["Q"]=30,["R"]=27,["Y"]=24,["Z"]=21,["E"]=18,["P"]=15,["T"]=12,["G"]=9,["M"]=6,["k"]=3,["h"]=2,["da"]=1,
  ["d"]=-1,["c"]=-2,["m"]=-3,["µ"]=-6,["n"]=-9,["p"]=-12,["f"]=-15,["a"]=-18,["z"]=-21,["y"]=-24,["r"]=-27,["q"]=-30
 };
 // A frozen map does not preserve insertion order. Keep an immutable snapshot
 // for the public MetricPrefixes sequence and parser/formatter enumeration.
 OrderedDefinitions=[..values];
 Exponents=values.ToFrozenDictionary(StringComparer.Ordinal);
 }
 // Shared internally so the Java parser and explicit prefix API use one symbol catalog.
 internal static IEnumerable<KeyValuePair<string,int>> Definitions=>OrderedDefinitions;
 /// <summary>Creates a prefixed linear unit. Affine units cannot be prefixed.</summary>
 public static UnitDefinition Apply(string prefix,UnitDefinition unit)
 {
  ArgumentNullException.ThrowIfNull(prefix);
  ArgumentNullException.ThrowIfNull(unit);
  if(!Exponents.TryGetValue(prefix,out int exponent))throw new ArgumentException("Unknown SI prefix",nameof(prefix));
  return ApplyExact(prefix,exponent,unit);
 }
 // Preserve parser/core exact factors rather than multiplying their early-rounded
 // double projections. The native offset-unit guard remains unchanged.
 internal static UnitDefinition ApplyExact(string symbol,int exponent,UnitDefinition unit)
 {
  ArgumentNullException.ThrowIfNull(unit);
  if(unit.Converter.Offset!=0)throw new InvalidOperationException("Cannot prefix an affine unit.");
  var power=System.Numerics.BigInteger.Pow(10,Math.Abs(exponent));
  var factor=exponent>=0?new Rational(power,1):new Rational(1,power);
  var result=UnitDefinition.FromExactLinear(symbol+unit.Symbol,unit.RationalDimension,JavaDoubleArithmetic.ScaleOf(unit)*factor);
  return unit.JavaModel is null?result:result with {JavaModel=unit.JavaModel.Transform(JavaUnitConverterAdapter.ForPrefix(10,exponent))};
 }
 /// <summary>Returns a decimal prefix's exponent.</summary>
 public static int Exponent(string prefix)=>Exponents.TryGetValue(prefix,out var exponent)?exponent:throw new ArgumentException("Unknown SI prefix",nameof(prefix));
}
