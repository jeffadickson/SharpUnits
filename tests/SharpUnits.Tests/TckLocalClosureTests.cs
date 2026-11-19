using Xunit;

namespace SharpUnits.Tests;

/// <summary>Whole-provider/catalog witnesses for three pinned behavioral TCK methods.</summary>
public sealed class TckLocalClosureTests
{
    [Fact]
    public void FactoryScaleMatchesEveryAvailableProvider()
    {
        // 51-A2 uses each provider's Length factory with the CURRENT provider's unit.
        var providers=JavaServiceProvider.Available;
        Assert.NotEmpty(providers); // Prevent vacuous success beyond the Java assertion.
        var unit=JavaServiceProvider.Current.SystemOfUnitsService.GetSystemOfUnits().GetUnitForQuantity("Length");
        Assert.NotNull(unit);
        foreach(var provider in providers)
        foreach(var scale in Enum.GetValues<JavaQuantityScale>())
        {
            var quantity=provider.GetQuantityFactory("Length").Create(10d,unit,scale);
            Assert.Equal(scale,quantity.Scale);
            Assert.Equal(10d,quantity.Value);
            Assert.Equal(unit,quantity.Unit);
        }
    }

    [Fact]
    public void EverySystemUnitParsesFromItsServiceText()
    {
        var provider=JavaServiceProvider.Current;
        var format=provider.FormatService.GetUnitFormat();
        var systems=provider.SystemOfUnitsService.AvailableSystems;
        Assert.NotEmpty(systems);
        foreach(var system in systems)
        {
            Assert.NotEmpty(system.Units);
            foreach(var unit in system.Units)
            {
                Assert.NotNull(unit);
                // 552-A1: Java u.toString() maps to the CLR service formatter,
                // not UnitDefinition's diagnostic record ToString().
                Assert.Equal(unit,format.Parse(format.Format(unit)));
            }
        }
    }

    [Fact]
    public void EverySystemQuantityRetainsDisplayUnitAndOne()
    {
        var provider=JavaServiceProvider.Current;
        var unitFormat=provider.FormatService.GetUnitFormat();
        var quantityFormat=provider.FormatService.GetQuantityFormat();
        var systems=provider.SystemOfUnitsService.AvailableSystems;
        Assert.NotEmpty(systems);
        foreach(var system in systems)
        {
            Assert.NotEmpty(system.Units);
            foreach(var unit in system.Units)
            {
                Assert.NotNull(unit);
                // 561-A4 asserts display value, not the coherent SI projection.
                var quantity=quantityFormat.ParseJavaQuantity("1 "+unitFormat.Format(unit));
                Assert.Equal(unit,quantity.Unit);
                Assert.Equal(1d,quantity.Value);
            }
        }
    }
}
