using System.Text.Json;
using SharpUnits;

/// <summary>Shared parser observations; admission probes do not equate exception types.</summary>
internal static class ParserFixtures
{
    public static object Evaluate(JsonElement test)
    {
        var registry=UnitRegistry.CreateStandard();
        var formatter=new QuantityTextFormat(registry);
        if(test.GetProperty("operation").GetString()=="parseAccepts")
        {
            try{formatter.ParseJavaQuantity(test.GetProperty("text").GetString()!);return true;}
            catch(Exception ex) when(ex is FormatException or KeyNotFoundException or DivideByZeroException or ArgumentException){return false;}
        }
        var quantity=formatter.ParseJavaQuantity(test.GetProperty("text").GetString()!);
        return test.GetProperty("observe").GetString() switch
        {
            "scale"=>quantity.Scale.ToString().ToUpperInvariant(),
            "negativeZero"=>BitConverter.DoubleToInt64Bits(quantity.Value)==long.MinValue,
            "unitMatches"=>quantity.Unit.Equals(test.GetProperty("resultUnit").GetString()=="1"
                ?new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1)):registry.Resolve(test.GetProperty("resultUnit").GetString()!)),
            "magnitude"=>quantity.Value,
            _=>throw new NotSupportedException("Unknown parser observation")
        };
    }
}
