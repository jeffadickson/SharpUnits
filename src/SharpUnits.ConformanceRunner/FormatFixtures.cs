using System.Globalization;
using System.Text;
using System.Text.Json;
using SharpUnits;

/// <summary>Observes the public Java formatting API through the same finite-double inputs as Indriya.</summary>
internal static class FormatFixtures
{
    public static object Evaluate(JsonElement test)
    {
        var previous=CultureInfo.CurrentCulture;
        try
        {
            if(test.TryGetProperty("locale",out var locale))CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(locale.GetString()!);
            var registry=UnitRegistry.CreateStandard();var format=new QuantityTextFormat(registry);
            var operation=test.GetProperty("operation").GetString();
            if(operation=="formatLocale")return format.IsLocaleSensitive;
            var symbol=test.GetProperty("from").GetString()!;
            var unit=symbol switch
            {
                "1"=>new UnitDefinition("1",Dimension.None,UnitConversion.Linear(1)),
                "A²"=>new UnitDefinition("A²",Dimension.ElectricCurrent.Pow(2),UnitConversion.Linear(1)),
                "V²"=>new UnitDefinition("V²",Dimension.Voltage.Pow(2),UnitConversion.Linear(1)),
                "Ω²"=>new UnitDefinition("Ω²",Dimension.Resistance.Pow(2),UnitConversion.Linear(1)),
                _=>format.ParseJavaQuantity("1 "+symbol).Unit
            };
            var scale=test.TryGetProperty("scale",out var kind)?Enum.Parse<JavaQuantityScale>(kind.GetString()!,true):JavaQuantityScale.Absolute;
            var quantity=test.TryGetProperty("text",out var text)?format.ParseJavaQuantity(text.GetString()!):new JavaQuantityAdapter(test.GetProperty("value").GetDouble(),unit,scale);
            if(operation=="formatLegacy") {registry.Register(unit);return format.Format(quantity.SiQuantity,unit.Symbol);}
            if(operation=="formatRoundTrip")
            {
                var restored=format.ParseJavaQuantity(format.FormatJavaQuantity(quantity));
                return test.TryGetProperty("observe",out var observed)&&observed.GetString()=="scale"
                    ?restored.Scale==quantity.Scale:restored.Value==quantity.Value&&restored.Unit.RationalDimension==quantity.Unit.RationalDimension;
            }
            var prefix=test.TryGetProperty("prefix",out var specified)?specified.GetString()!:"";
            var identity=test.TryGetProperty("observe",out var observation)&&observation.GetString()=="identity";
            if(operation=="formatAppendable")
            {
                var destination=new StringBuilder(prefix);var returned=format.FormatJavaQuantity(quantity,destination);
                return identity?ReferenceEquals(destination,returned):destination.ToString();
            }
            if(operation=="formatWriter")
            {
                using var destination=new StringWriter(CultureInfo.CurrentCulture);destination.Write(prefix);
                var returned=format.FormatJavaQuantity(quantity,destination);
                return identity?ReferenceEquals(destination,returned):destination.ToString();
            }
            return format.FormatJavaQuantity(quantity);
        }
        finally{CultureInfo.CurrentCulture=previous;}
    }
}
