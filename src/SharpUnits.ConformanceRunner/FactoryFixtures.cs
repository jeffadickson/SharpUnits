using System.Text.Json;
using SharpUnits;

/// <summary>Executes shared fixtures through the public registry-backed factory contract.</summary>
internal static class FactoryFixtures
{
    public static object Evaluate(JsonElement test, UnitRegistry registry)
    {
        // Explicit fixture units do not silently expand the standard registry.
        registry.Register(new UnitDefinition("m",new Dimension(1,0,0,0,0,0,0),UnitConversion.Linear(1)));
        registry.Register(new UnitDefinition("mm",new Dimension(1,0,0,0,0,0,0),UnitConversion.Linear(.001)));
        registry.Register(new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1)));
        IJavaQuantityFactory factory=new QuantityFactory(registry);
        var symbol=test.GetProperty("from").GetString()!;
        var value=test.GetProperty("value").GetDouble();
        var quantity=test.GetProperty("operation").GetString()=="factoryCreateDefault"
            ?factory.CreateJavaQuantity(value,symbol)
            :factory.CreateJavaQuantity(value,symbol,Enum.Parse<JavaQuantityScale>(test.GetProperty("scale").GetString()!,true));
        return test.GetProperty("observe").GetString() switch
        {
            "scale"=>quantity.Scale.ToString().ToUpperInvariant(),
            "magnitude"=>quantity.Value,
            "negativeZero"=>BitConverter.DoubleToInt64Bits(quantity.Value)==long.MinValue,
            "unitEquals"=>ReferenceEquals(quantity.Unit,registry.Resolve(symbol)),
            "converted"=>quantity.To(registry.Resolve(test.GetProperty("to").GetString()!)).Value,
            _=>throw new NotSupportedException("Unknown factory observation")
        };
    }
}
