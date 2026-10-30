using System.Globalization;
using SharpUnits;

// Observation keys describe the retained graph, not a formatter's display string
// or a sample-only approximation. Length/hex encoding avoids symbol collisions.
internal static partial class CoreFixtures
{
    internal static Dictionary<string,object?> QuantityObservation(JavaNumberQuantity quantity)
    {
        object conversion;
        try
        {
            var converter=quantity.Unit.SystemConverter;
            conversion=new Dictionary<string,object?>{{"available",true},{"operations",ConverterIdentity(converter)},
                {"zero",ObserveNumber(converter.Convert(JavaNumber.Zero))},{"one",ObserveNumber(converter.Convert(JavaNumber.One))}};
        }
        catch(NotSupportedException){conversion=new Dictionary<string,object?>{{"available",false},{"error","UNSUPPORTED"}};}
        return new(){{"value",ObserveNumber(quantity.Value)},{"scale",quantity.Scale.ToString().ToUpperInvariant()},
            {"unit",UnitIdentity(quantity.Unit)},{"converter",conversion}};
    }
    private static string SymbolKey(string? symbol)=>symbol is null?"null":string.Join("",symbol.Select(c=>((int)c).ToString("X4",CultureInfo.InvariantCulture)));
    private static string DimensionKey(RationalDimension d)=>string.Join(",",new[]{d.Length,d.Mass,d.Time,d.Current,d.Temperature,d.Amount,d.Luminosity}.Select(p=>$"{p.Numerator}/{p.Denominator}"));
    internal static string ConverterIdentity(JavaUnitConverterAdapter converter)=>converter.IsIdentity?"I":string.Join(";",converter.Operations.Select(
        step=>step.Kind=="Power"?$"Power({step.Radix},{step.Exponent})":$"{step.Kind}({ObserveNumber(step.Parameter)})"));
    internal static string UnitIdentity(JavaUnit unit)=>unit.Kind switch
    {
        JavaUnitKind.Base=>$"B({SymbolKey(unit.Symbol)},{DimensionKey(unit.Dimension)})",
        JavaUnitKind.Alternate=>$"A({SymbolKey(unit.Symbol)},{UnitIdentity(unit.ParentUnit!)})",
        JavaUnitKind.Transformed=>$"T({UnitIdentity(unit.ParentUnit!)},{ConverterIdentity(unit.TransformConverter!)})",
        _=>"P["+string.Join(";",unit.Factors.Select(p=>$"{UnitIdentity(p.Key)}^{p.Value.Numerator}/{p.Value.Denominator}").OrderBy(x=>x,StringComparer.Ordinal))+"]"
    };
}
