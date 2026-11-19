using System.Text;
using System.Text.Json;
using SharpUnits;

/// <summary>Observes stateful public CLR unit-format APIs with isolated formatter instances.</summary>
internal static class UnitFormatFixtures
{
    public static object Evaluate(JsonElement test)
    {
        try{return Observe(test);}
        catch(Exception error) when(error is ArgumentException or FormatException or KeyNotFoundException){return "INVALID";}
    }
    private static object Observe(JsonElement test)
    {
        var system=JavaServiceProvider.Current.SystemOfUnitsService.GetSystemOfUnits();
        UnitDefinition Unit(string text)=>text switch {
            "__NULL__"=>null!,"FAHRENHEIT"=>UnitRegistry.CreateStandard().Resolve("°F"),
            "__FOREIGN__"=>new UnitDefinition("privateβ",Dimension.ElectricCurrent,UnitConversion.Linear(1)),
            _=>system.GetUnit(text)??system.Entries.Single(entry=>entry.Field==text).Unit};
        var kind=test.GetProperty("kind").GetString();
        var flavor=test.TryGetProperty("ascii",out var ascii)&&ascii.GetBoolean()?JavaUnitFormatFlavor.Ascii:JavaUnitFormatFlavor.Default;
        var format=new JavaUnitTextFormat(system.CreateRegistry(),flavor);
        if(kind=="identifierBmp")
        {
            var bitmap=new StringBuilder(65536);var accepted=0;var target=Unit("A");
            for(var code=0;code<65536;code++)
            {
                try{format.Label(target,((char)code).ToString());bitmap.Append('1');accepted++;}
                catch(ArgumentException){bitmap.Append('0');}
            }
            return accepted.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(bitmap.ToString())));
        }
        if(kind=="asciiUnit")return new JavaUnitTextFormat(system.CreateRegistry(),JavaUnitFormatFlavor.Ascii).Format(Unit(test.GetProperty("field").GetString()!));
        if(kind=="asciiPrefix")return new JavaUnitTextFormat(system.CreateRegistry(),JavaUnitFormatFlavor.Ascii)
            .Format(SiPrefixes.Apply(test.GetProperty("prefixSymbol").GetString()!,Unit("Ω")));
        var observed=Unit(test.GetProperty("unit").GetString()!);
        foreach(var action in test.GetProperty("actions").EnumerateArray())
        {
            var unit=Unit(action.GetProperty("unit").GetString()!);
            var name=action.GetProperty("name").GetString();
            switch(action.GetProperty("op").GetString())
            {
                case "label":format.Label(unit,name!);break;
                case "alias":format.Alias(unit,name!);break;
                case "removeLabel":format.RemoveLabel(unit);break;
                case "removeAlias":format.RemoveAlias(unit,name);break;
                case "removeAliases":format.RemoveAliases(unit);break;
                default:throw new NotSupportedException();
            }
        }
        if(kind=="globalLookup")
        {
            var shared=JavaServiceProvider.Current.FormatService.GetUnitFormat();var target=Unit("A");var original=shared.Format(target);
            try{shared.Label(target,"ufSharedA");return system.GetUnit("ufSharedA")==target&&system.GetUnit("A") is null;}
            finally{shared.RemoveAlias(target,"ufSharedA");shared.Label(target,original);}
        }
        if(kind is "defaultAreaRemoval" or "defaultVolumeRemoval")
        {
            var target=Unit(kind=="defaultVolumeRemoval"?"CUBIC_METRE":"SQUARE_METRE");
            var services=new JavaFormatService(system.CreateRegistry());services.GetUnitFormat().RemoveLabel(target);
            return services.GetUnitFormat("ASCII")!.Format(target);
        }
        if(kind is "quantityFormat" or "quantityParse")
        {
            var services=new JavaFormatService(system.CreateRegistry());
            services.GetUnitFormat().Label(Unit("A"),"amps");
            return kind=="quantityFormat"?services.GetQuantityFormat().FormatJavaQuantity(new JavaQuantityAdapter(2,Unit("A")))
                :services.GetQuantityFormat().ParseJavaQuantity("2 amps").Unit==Unit("A");
        }
        if(kind=="builder")
        {
            var builder=new StringBuilder(test.GetProperty("prefix").GetString());var returned=format.Format(observed,builder);
            return test.GetProperty("identity").GetBoolean()?ReferenceEquals(returned,builder):builder.ToString();
        }
        if(kind=="writer")
        {
            using var writer=new StringWriter();writer.Write(test.GetProperty("prefix").GetString());var returned=format.Format(observed,writer);
            return test.GetProperty("identity").GetBoolean()?ReferenceEquals(returned,writer):writer.ToString();
        }
        if(kind=="parseDimension")
        {
            var d=format.Parse(test.GetProperty("text").GetString()!).Dimension;
            return "["+string.Join(", ",new[]{d.Length,d.Mass,d.Time,d.Current,d.Temperature,d.Amount,d.Luminosity})+"]";
        }
        return kind switch
        {
            "format"=>format.Format(observed),"accepted"=>true,"parse"=>format.Parse(test.GetProperty("text").GetString()!)==observed,
            "parseFormat"=>format.Format(format.Parse(test.GetProperty("text").GetString()!)),
            "isolated"=>new JavaUnitTextFormat(system.CreateRegistry(),flavor).Format(observed),
            "otherFlavor"=>new JavaUnitTextFormat(system.CreateRegistry(),JavaUnitFormatFlavor.Ascii).Format(observed),
            _=>throw new NotSupportedException(kind)
        };
    }
}
