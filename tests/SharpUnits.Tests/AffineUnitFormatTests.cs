using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Checks the bounded Fahrenheit adapter without changing native temperature conventions.</summary>
[Collection("Mutable unit-format compatibility")]
public sealed class AffineUnitFormatTests
{
    // @relation(JP-UF-FAHRENHEIT, scope=file)
    private static readonly Dimension Temperature=new(0,0,0,0,1,0,0);

    [Fact]
    public void StandardRegistryDoesNotImplicitlyLabelJavaFahrenheit()
    {
        var registry=UnitRegistry.CreateStandard();var unit=registry.Resolve("°F");
        var services=new JavaFormatService(registry);
        Assert.Equal("((K*5)/9)+459.67",services.GetUnitFormat().Format(unit));
        Assert.Equal("((K*5)/9)+459.67",services.GetUnitFormat("ASCII")!.Format(unit));
        Assert.Equal("2 ((K*5)/9)+459.67",services.GetQuantityFormat().FormatJavaQuantity(new JavaQuantityAdapter(2,unit)));
        Assert.Same(unit,services.GetUnitFormat().Parse("°F")); // Retained native registry extension.
        Assert.Equal("°F",unit.Symbol);
        Assert.EndsWith(" °F",new QuantityTextFormat(registry).Format(unit.ToQuantity(32),"°F"));
        Assert.Equal(32,unit.FromQuantity(unit.ToQuantity(32)),10);
        Assert.Equal(273.15,registry.Convert(32,"°F","K"),10);
    }

    [Fact]
    public void ExplicitLabelsOverrideExpressionAndRemovalRestoresIt()
    {
        var registry=UnitRegistry.CreateStandard();var unit=registry.Resolve("°F");var format=new JavaUnitTextFormat(registry);
        format.Label(unit,"fahrenheit");Assert.Equal("fahrenheit",format.Format(unit));
        format.Alias(unit,"alternateF");Assert.Same(unit,format.Parse("alternateF"));
        format.RemoveLabel(unit);Assert.Equal("((K*5)/9)+459.67",format.Format(unit));
        Assert.Throws<KeyNotFoundException>(()=>format.Parse("alternateF"));
    }

    [Fact]
    public void SymbolAndDimensionAloneDoNotEstablishFahrenheitIdentity()
    {
        var format=new JavaUnitTextFormat(new UnitRegistry());
        Assert.Equal("customF",format.Format(new UnitDefinition("customF",Temperature,UnitConversion.Fahrenheit)));
        Assert.Equal("°F",format.Format(new UnitDefinition("°F",Dimension.ElectricCurrent,UnitConversion.Fahrenheit)));
        Assert.Equal("°F",format.Format(new UnitDefinition("°F",Temperature,UnitConversion.Linear(5d/9d))));
    }

    [Property(MaxTest=300,Replay="385,617")]
    public bool NearbyConvertersAreNotReconstructedAsFahrenheit(int input)
    {
        // Bounded, exactly nonzero perturbations challenge accidental symbol-only recognition.
        var delta=1+(input&1023);var format=new JavaUnitTextFormat(new UnitRegistry());
        var wrongScale=new UnitDefinition("°F",Temperature,new UnitConversion(UnitConversion.Fahrenheit.Scale+delta,UnitConversion.Fahrenheit.Offset));
        var wrongOffset=new UnitDefinition("°F",Temperature,new UnitConversion(UnitConversion.Fahrenheit.Scale,UnitConversion.Fahrenheit.Offset+delta));
        return format.Format(wrongScale)=="°F"&&format.Format(wrongOffset)=="°F";
    }
}
