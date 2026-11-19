using System.Text;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges writer ownership, mutable-map invariants, exact prefix factors and hostile labels.</summary>
[Collection("Mutable unit-format compatibility")]
public sealed class UnitFormatRobustnessTests
{
    // @relation(JP-015, scope=file)
    private static JavaUnitSystem Units=>JavaServiceProvider.Current.SystemOfUnitsService.GetSystemOfUnits();
    private static UnitDefinition Unit(string field)=>Units.Entries.Single(entry=>entry.Field==field).Unit;
    private static JavaUnitTextFormat Fresh(JavaUnitFormatFlavor flavor=JavaUnitFormatFlavor.Default)=>new(Units.CreateRegistry(),flavor);

    [Fact]
    public void WriterOwnershipAndIoErrorsRemainWithCaller()
    {
        var format=Fresh();var writer=new ProbeWriter();writer.Write("head|");
        Assert.Same(writer,format.Format(Unit("OHM"),writer));Assert.Equal("head|Ω",writer.ToString());
        Assert.False(writer.Flushed);Assert.False(writer.Closed);
        writer.Reject=true;Assert.Throws<IOException>(()=>format.Format(Unit("OHM"),writer));
        Assert.False(writer.Flushed);Assert.False(writer.Closed);
    }

    [Fact]
    public void NullArgumentsAndUnknownFlavorAreRejected()
    {
        var format=Fresh();var unit=Unit("AMPERE");
        Assert.Throws<ArgumentNullException>(()=>new JavaUnitTextFormat(null!));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Fresh((JavaUnitFormatFlavor)99));
        Assert.Throws<ArgumentNullException>(()=>format.Format(null!));
        Assert.Throws<ArgumentNullException>(()=>format.Format(unit,(StringBuilder)null!));
        Assert.Throws<ArgumentNullException>(()=>format.Format(unit,(TextWriter)null!));
        Assert.Throws<ArgumentNullException>(()=>format.Parse(null!));
        Assert.Throws<ArgumentNullException>(()=>format.Label(null!,"valid"));
        Assert.Throws<ArgumentNullException>(()=>format.Alias(null!,"valid"));
        Assert.Throws<ArgumentNullException>(()=>format.RemoveLabel(null!));
        Assert.Throws<ArgumentNullException>(()=>format.RemoveAlias(null!,"valid"));
        Assert.Throws<ArgumentNullException>(()=>format.RemoveAliases(null!));
    }

    [Fact]
    public void FreshLabelStateDoesNotMutateUnitOrRegistryMetadata()
    {
        var registry=Units.CreateRegistry();var format=new JavaUnitTextFormat(registry);var unit=Unit("AMPERE");
        format.Label(unit,"scopedAmp");
        Assert.Equal("A",unit.Symbol);Assert.Same(unit,registry.Resolve("A"));
        Assert.Throws<KeyNotFoundException>(()=>registry.Resolve("scopedAmp"));
        Assert.Equal("A",Fresh().Format(unit));Assert.Same(unit,Units.GetUnit("A"));
        var custom=new UnitDefinition("nΩ",Dimension.Voltage,UnitConversion.Linear(2));
        var explicitRegistry=new UnitRegistry();explicitRegistry.Register(Unit("OHM"));explicitRegistry.Register(custom);
        Assert.Same(custom,new JavaUnitTextFormat(explicitRegistry).Parse("nΩ"));
    }

    [Fact]
    public void ConcurrentRelabelingPreservesEveryFormerAlias()
    {
        var format=Fresh();var unit=Unit("AMPERE");
        Parallel.For(0,128,index=>format.Label(unit,"thread"+index));
        format.Label(unit,"finish");
        for(var i=0;i<128;i++)Assert.Same(unit,format.Parse("thread"+i));
        Assert.Equal("finish",format.Format(unit));Assert.Same(unit,format.Parse("finish"));
    }

    [Property(MaxTest=300,Replay="385,611")]
    public bool RelabelingKeepsOldParseNames(int value)
    {
        var format=Fresh();var unit=Unit("AMPERE");
        var old=FormattableString.Invariant($"u_{value}");var current=FormattableString.Invariant($"v_{value}");
        format.Label(unit,old);format.Label(unit,current);
        return format.Parse(old)==unit&&format.Parse(current)==unit&&format.Format(unit)==current;
    }

    [Property(MaxTest=300,Replay="385,613")]
    public bool ArbitraryBuilderPrefixesRemainOpaque(NonNull<string> prefix)
    {
        var format=Fresh();var unit=Unit("AMPERE");format.Label(unit,"unit");
        var builder=new StringBuilder(prefix.Get);
        return ReferenceEquals(builder,format.Format(unit,builder))&&builder.ToString()==prefix.Get+"unit";
    }

    [Fact]
    public void SeededInvalidInitialCharacterFuzzingLeavesMapsUnchanged()
    {
        const int seed=385615;var random=new Random(seed);var format=Fresh();var unit=Unit("AMPERE");
        const string invalid="0123456789*/()[]¹²³^+-· \t\r\n";
        for(var sample=0;sample<1000;sample++)
        {
            var label=invalid[random.Next(invalid.Length)]+new string(Enumerable.Range(0,random.Next(1,80)).Select(_=>(char)random.Next(65536)).ToArray());
            Assert.Throws<ArgumentException>(()=>format.Label(unit,label));
            Assert.Throws<ArgumentException>(()=>format.Alias(unit,label));
        }
        Assert.Equal("A",format.Format(unit));Assert.Same(unit,format.Parse("A"));
    }

    [Fact]
    public void AsciiMicroGramAndLitreKeepTheirExactFactors()
    {
        var format=Fresh(JavaUnitFormatFlavor.Ascii);
        Assert.Equal(1,new JavaQuantityAdapter(1e9,format.Parse("microg")).To(Unit("KILOGRAM")).Value);
        Assert.Equal(1,new JavaQuantityAdapter(1e9,format.Parse("microL")).To(Unit("CUBIC_METRE")).Value);
    }

    [Fact]
    public void AliasingAComponentChangesProductDimensions()
    {
        var format=Fresh();format.Alias(Unit("AMPERE"),"m");
        Assert.Equal(new Dimension(0,0,-1,1,0,0,0),format.Parse("m/s").Dimension);
    }

    [Fact]
    public void NullAliasRemovalAndCurrentLabelPreservationFollowJava()
    {
        var format=Fresh();var unit=Unit("AMPERE");format.Label(unit,"amps");format.Alias(unit,"other");
        format.RemoveAlias(unit,null);Assert.Same(unit,format.Parse("other"));
        format.RemoveAliases(unit);Assert.Same(unit,format.Parse("amps"));
        Assert.Throws<KeyNotFoundException>(()=>format.Parse("other"));
    }

    [Fact]
    public void DefaultProductGraphsObserveChangedComponentLabels()
    {
        var format=Fresh();format.Label(Unit("METRE"),"met");format.Label(Unit("SECOND"),"sec");
        Assert.Equal("met/sec",format.Format(Unit("METRE_PER_SECOND")));
        Assert.Equal("met/sec²",format.Format(Unit("METRE_PER_SQUARE_SECOND")));
    }

    [Fact]
    public void AsciiValidationCoversTheWholeLabelWhileDefaultOnlyChecksItsStart()
    {
        var unit=Unit("AMPERE");var format=Fresh();var ascii=Fresh(JavaUnitFormatFlavor.Ascii);
        foreach(var label in new[]{"a/Ω","a β","a\u0000β"})
        {
            format.Label(unit,label);Assert.Equal(label,format.Format(unit));
            Assert.Throws<ArgumentException>(()=>ascii.Label(unit,label));
        }
    }

    private sealed class ProbeWriter:StringWriter
    {
        internal bool Reject,Flushed,Closed;
        public override void Write(string? value){if(Reject)throw new IOException("Deliberate write rejection");base.Write(value);}
        public override void Flush(){Flushed=true;base.Flush();}
        protected override void Dispose(bool disposing){Closed=true;base.Dispose(disposing);}
    }
}
