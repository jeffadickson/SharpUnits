using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Standard registry lookup, conversion, and collision tests.</summary>
public sealed class RegistryTests
{
    [Fact] public void MilliampsToAmps() => Assert.Equal(0.25, UnitRegistry.CreateStandard().Convert(250, "mA", "A"), 10);
    [Fact] public void FahrenheitToCelsius() => Assert.Equal(100, UnitRegistry.CreateStandard().Convert(212, "°F", "°C"), 9);
    [Fact] public void RejectIncompatible() => Assert.Throws<InvalidOperationException>(() => UnitRegistry.CreateStandard().Convert(1, "V", "A"));

    [Fact]
    public void RejectDuplicateSymbol()
    {
        var r = UnitRegistry.CreateStandard();
        Assert.False(r.Register(new UnitDefinition("V", Dimension.Voltage, UnitConversion.Linear(1))));
    }
}