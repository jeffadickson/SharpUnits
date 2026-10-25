using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges isolation, immutable discovery, concurrent caching and rejected service inputs.</summary>
public sealed class JavaServiceRobustnessTests
{
    // @relation(JP-020, JP-021, scope=file)
    private static JavaUnitSystem System=>JavaServiceProvider.Current.SystemOfUnitsService.GetSystemOfUnits();

    [Fact]
    public void CatalogAndDiscoveryCannotBeMutatedThroughCollectionCasts()
    {
        Assert.Throws<NotSupportedException>(()=>((IList<JavaServiceProvider>)JavaServiceProvider.Available).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<JavaUnitEntry>)System.Entries).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<UnitDefinition>)System.Units).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<JavaUnitSystem>)JavaServiceProvider.Current.SystemOfUnitsService.AvailableSystems).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<JavaPrefix>)JavaServiceProvider.Current.SystemOfUnitsService.BinaryPrefixes).Clear());
        Assert.Throws<NotSupportedException>(()=>((IList<string>)JavaServiceProvider.Current.FormatService.QuantityFormatNames).Clear());
    }

    [Fact]
    public void FreshRegistriesCannotPolluteOtherServices()
    {
        var first=System.CreateRegistry();var second=System.CreateRegistry();
        first.Register(new UnitDefinition("private",Dimension.None,UnitConversion.Linear(2)));
        Assert.Throws<KeyNotFoundException>(()=>second.Resolve("private"));
        Assert.Null(System.GetUnit("private"));
        Assert.Throws<KeyNotFoundException>(()=>UnitRegistry.CreateStandard().Resolve("m"));
    }

    [Fact]
    public void FactoryCacheIsStableUnderConcurrentAccess()
    {
        var factories=new JavaTypedQuantityFactory[256];
        Parallel.For(0,factories.Length,i=>factories[i]=JavaServiceProvider.Current.GetQuantityFactory("Length"));
        Assert.All(factories,factory=>Assert.Same(factories[0],factory));
        Assert.Same(System.GetUnit("m"),factories[0].SystemUnit);
    }

    [Fact]
    public void NullServiceArgumentsAreRejected()
    {
        var provider=JavaServiceProvider.Current;var formats=provider.FormatService;
        Assert.Throws<ArgumentNullException>(()=>formats.GetQuantityFormat(null!));
        Assert.Throws<ArgumentNullException>(()=>formats.GetUnitFormat(null!));
        Assert.Throws<ArgumentNullException>(()=>formats.GetUnitFormat(null!,"default"));
        Assert.Throws<ArgumentNullException>(()=>provider.SystemOfUnitsService.GetSystemOfUnits(null!));
        Assert.Throws<ArgumentNullException>(()=>System.GetUnit(null!));
        Assert.Null(System.GetUnitForQuantity(null));
        Assert.Throws<ArgumentNullException>(()=>provider.GetQuantityFactory(null!));
        Assert.Throws<ArgumentException>(()=>provider.GetQuantityFactory(" "));
        Assert.Throws<ArgumentNullException>(()=>formats.GetUnitFormat().Format(null!));
        Assert.Throws<ArgumentNullException>(()=>formats.GetUnitFormat().Parse(null!));
    }

    [Fact]
    public void UnmappedCustomQuantityTypesHaveNoSystemUnit()
    {
        var factory=JavaServiceProvider.Current.GetQuantityFactory("custom.quantity");
        Assert.Null(factory.SystemUnit);Assert.Null(System.GetUnitForQuantity("custom.quantity"));
        Assert.Equal("custom.quantity",factory.QuantityType);
        Assert.Same(factory,JavaServiceProvider.Current.GetQuantityFactory("custom.quantity"));
    }

    [Fact]
    public void UnsupportedJavaFormatFamiliesAreNotAdvertised()
    {
        var formats=JavaServiceProvider.Current.FormatService;
        foreach(var family in new[]{"EBNF","LOCAL","NUMBERDELIMITER","NUMBERSPACE","ASCII"})
        {
            Assert.Null(formats.GetQuantityFormat(family));Assert.Null(formats.GetUnitFormat(family));
            Assert.DoesNotContain(family,formats.QuantityFormatNames);Assert.DoesNotContain(family,formats.UnitFormatNames);
        }
    }

    [Property(MaxTest=300,Replay="385,521")]
    public bool KilometresPerHourKeepTheExactFiveEighteenthsFactor(int value)
    {
        // Both integer products are exactly representable in binary64; the oracle is
        // the reduced physical ratio 5/18, independent of the projected converter scale.
        var quantity=JavaServiceProvider.Current.GetQuantityFactory("Speed").Create(18d*value,System.GetUnit("km/h")!);
        return quantity.To(System.GetUnit("m/s")!).Value==5d*value;
    }

    [Property(MaxTest=300,Replay="385,523")]
    public bool UnknownUnitNamesNeverResolve(int value)
    {
        var key=FormattableString.Invariant($"missing/{value}");
        return System.GetUnit(key) is null&&JavaServiceProvider.Current.SystemOfUnitsService.GetSystemOfUnits(key) is null;
    }

    [Fact]
    public void SeededMalformedNameFuzzingDoesNotCreateRegistrations()
    {
        const int seed=385521;var random=new Random(seed);var before=System.Units.Count;
        for(var i=0;i<1000;i++)
        {
            var key="missing/"+new string(Enumerable.Range(0,random.Next(1,100)).Select(_=>(char)random.Next(65536)).ToArray());
            Assert.Null(System.GetUnit(key));
            Assert.Null(JavaServiceProvider.Current.FormatService.GetQuantityFormat(key));
            Assert.Null(JavaServiceProvider.Current.FormatService.GetUnitFormat(key));
        }
        Assert.Equal(before,System.Units.Count);
    }

    [Fact]
    public void TypedFactoryRejectsNonfiniteValuesAndInvalidScales()
    {
        var factory=JavaServiceProvider.Current.GetQuantityFactory("Length");
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.Create(double.NaN,System.GetUnit("m")!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.Create(double.PositiveInfinity,System.GetUnit("m")!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>factory.Create(1,System.GetUnit("m")!,(JavaQuantityScale)99));
    }
}
