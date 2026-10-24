using System.Text.Json;
using SharpUnits;

/// <summary>Shared parser observations; admission probes do not equate exception types.</summary>
internal static class ParserFixtures
{
    public static object Evaluate(JsonElement test)
    {
        var formatter=new QuantityTextFormat(UnitRegistry.CreateStandard());
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
            "unit"=>quantity.Unit.Symbol,
            "magnitude"=>quantity.Value,
            _=>throw new NotSupportedException("Unknown parser observation")
        };
    }
}
