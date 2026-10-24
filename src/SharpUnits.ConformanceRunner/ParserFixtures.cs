using System.Text.Json;
using SharpUnits;

/// <summary>Shared parser observations; admission probes do not equate exception types.</summary>
internal static class ParserFixtures
{
    public static object Evaluate(JsonElement test)
    {
        var registry=UnitRegistry.CreateStandard();
        UnitDefinition Target()=>test.GetProperty("resultUnit").GetString() switch
        {
            "1"=>new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1)),
            "A²"=>new UnitDefinition("A²",Dimension.ElectricCurrent.Pow(2),UnitConversion.Linear(1)),
            "V²"=>new UnitDefinition("V²",Dimension.Voltage.Pow(2),UnitConversion.Linear(1)),
            "Ω²"=>new UnitDefinition("Ω²",Dimension.Resistance.Pow(2),UnitConversion.Linear(1)),
            "A³"=>new UnitDefinition("A³",Dimension.ElectricCurrent.Pow(3),UnitConversion.Linear(1)),
            "pow23A"=>new UnitDefinition("pow23A",Dimension.ElectricCurrent.Pow(23),UnitConversion.Linear(1)),
            "invA"=>new UnitDefinition("invA",Dimension.ElectricCurrent.Pow(-1),UnitConversion.Linear(1)),
            "sqrtA"=>new UnitDefinition("sqrtA",RationalDimension.From(Dimension.ElectricCurrent).Root(2),UnitConversion.Linear(1)),
            "invSqrtA"=>new UnitDefinition("invSqrtA",RationalDimension.From(Dimension.ElectricCurrent).Pow(new Rational(-1,2)),UnitConversion.Linear(1)),
            "AperΩ"=>new UnitDefinition("AperΩ",Dimension.ElectricCurrent-Dimension.Resistance,UnitConversion.Linear(1)),
            "AperV"=>new UnitDefinition("AperV",Dimension.ElectricCurrent-Dimension.Voltage,UnitConversion.Linear(1)),
            var symbol=>registry.Resolve(symbol!)
        };
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
            "unitScale"=>quantity.Unit.Converter.Scale,
            "compatible"=>quantity.Unit.RationalDimension==Target().RationalDimension,
            "converted"=>quantity.To(Target()).Value,
            _=>throw new NotSupportedException("Unknown parser observation")
        };
    }
}
