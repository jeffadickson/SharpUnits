using System.Text.Json;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges factory results against fixtures executed by the pinned Java provider.</summary>
public sealed class FactoryReferenceFixtureTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"quantity-reference-cases.json")));
        foreach(var test in document.RootElement.GetProperty("cases").EnumerateArray())
            if(test.GetProperty("operation").GetString()!.StartsWith("factory",StringComparison.Ordinal))
                yield return new object[]{test.GetProperty("id").GetString()!,test.GetRawText()};
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchesIndriyaFactoryFixture(string id,string json)
    {
        using var document=JsonDocument.Parse(json);
        var test=document.RootElement;
        var actual=FactoryFixtures.Evaluate(test,UnitRegistry.CreateStandard());
        var expected=test.GetProperty("expected");
        if(expected.ValueKind==JsonValueKind.Number)
            Assert.Equal(expected.GetDouble(),Assert.IsType<double>(actual));
        else if(expected.ValueKind==JsonValueKind.String)
            Assert.Equal(expected.GetString(),Assert.IsType<string>(actual));
        else
            Assert.Equal(expected.GetBoolean(),Assert.IsType<bool>(actual));
    }

    [Fact]
    public void PreservesLegacyFactoryContract()
    {
        var registry=UnitRegistry.CreateStandard();
        var factory=new QuantityFactory(registry);
        IQuantityFactory legacy=factory;
        IJavaQuantityFactory adapter=factory;
        Assert.Equal(283.15,legacy.Create(10,"°C").SiValue);
        Assert.Equal(10,adapter.CreateJavaQuantity(10,"°C",JavaQuantityScale.Relative).SiQuantity.SiValue);
        Assert.Same(registry.Resolve("°C"),adapter.CreateJavaQuantity(10,"°C").Unit);
    }

    [Fact]
    public void RejectsInvalidFactoryInputs()
    {
        IJavaQuantityFactory factory=new QuantityFactory(UnitRegistry.CreateStandard());
        Assert.Throws<KeyNotFoundException>(()=>factory.CreateJavaQuantity(1,"missing"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.CreateJavaQuantity(double.NaN,"A"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.CreateJavaQuantity(double.PositiveInfinity,"A"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.CreateJavaQuantity(1,"A",(JavaQuantityScale)99));
    }
}
