using System.Text.Json;
using SharpUnits;

/// <summary>Observes CLR service APIs without substituting fixture expectations for implementation.</summary>
internal static class ServiceFixtures
{
    public static object Evaluate(JsonElement test)
    {
        var provider=JavaServiceProvider.Current;
        var system=provider.SystemOfUnitsService.GetSystemOfUnits();
        var argument=test.GetProperty("argument").GetString()!;
        if(test.GetProperty("operation").GetString()=="serviceUnit")
        {
            var entry=system.Entries.Single(entry=>entry.Field==argument);var unit=entry.Unit;var d=unit.Dimension;
            return new {label=unit.Symbol,name=entry.Name,dimension=new[]{d.Length,d.Mass,d.Time,d.Current,d.Temperature,d.Amount,d.Luminosity},
                zero=unit.Converter.ToSi(0),one=unit.Converter.ToSi(1)};
        }
        if(test.GetProperty("operation").GetString()=="serviceType")return new {
            system=system.GetUnitForQuantity(argument)?.Symbol??"<null>",factory=provider.GetQuantityFactory(argument).SystemUnit?.Symbol??"<null>"};
        var formats=provider.FormatService;var systems=provider.SystemOfUnitsService;
        return argument switch
        {
            "available"=>JavaServiceProvider.Available.Count>0,"current"=>provider is not null,"priority"=>provider.Priority,
            "quantityDefault"=>formats.GetQuantityFormat() is not null,
            "quantityNamed"=>ReferenceEquals(formats.GetQuantityFormat("simple"),formats.GetQuantityFormat()),
            "quantityNames"=>formats.QuantityFormatNames.Contains("SIMPLE"),
            "unitDefault"=>formats.GetUnitFormat() is not null,
            "unitNamed"=>ReferenceEquals(formats.GetUnitFormat("DEFAULT"),formats.GetUnitFormat()),
            "unitNames"=>formats.UnitFormatNames.Contains("SIMPLE_DEFAULT"),
            "unitVariant"=>ReferenceEquals(formats.GetUnitFormat("simple","default"),formats.GetUnitFormat()),
            "systemDefault"=>systems.GetSystemOfUnits() is not null,
            "systemNamed"=>ReferenceEquals(systems.GetSystemOfUnits("Units"),system),
            "systemsAvailable"=>systems.AvailableSystems.Count,"systemName"=>system.Name,"systemUnits"=>system.Units.Count,
            "unitByString"=>system.GetUnit("A")!.Symbol,
            "unitsByDimension"=>system.GetUnits(system.GetUnit("s")!.RationalDimension).Count,
            "unitByType"=>system.GetUnitForQuantity("Temperature")!.Symbol,
            "typedFactory"=>provider.GetQuantityFactory("Temperature").Create(20,system.GetUnit("℃")!).Value,
            "metricPrefixes"=>systems.MetricPrefixes.Count,"binaryPrefixes"=>systems.BinaryPrefixes.Count,
            "unitFormatFormat"=>formats.GetUnitFormat().Format(system.GetUnit("℃")!),
            "unitFormatParse"=>formats.GetUnitFormat().Format(formats.GetUnitFormat().Parse("μA")),
            _=>throw new NotSupportedException(argument)
        };
    }
}
