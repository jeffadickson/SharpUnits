using System.Text;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Challenges destination ownership, invalid arguments and arbitrary caller prefixes.</summary>
public sealed class FormattingRobustnessTests
{
    // @relation(JP-015-DISPLAY, JP-015-APPENDABLE, scope=file)
    private static QuantityTextFormat Formatter()=>new(UnitRegistry.CreateStandard());

    [Property(MaxTest=300,Replay="385,415")]
    public bool ArbitraryPrefixesArePreserved(NonNull<string> prefix,int value)
    {
        var formatter=Formatter();
        var quantity=formatter.ParseJavaQuantity(value.ToString(System.Globalization.CultureInfo.InvariantCulture)+" mA");
        var builder=new StringBuilder(prefix.Get);
        return ReferenceEquals(builder,formatter.FormatJavaQuantity(quantity,builder))
            &&builder.ToString()==prefix.Get+formatter.FormatJavaQuantity(quantity);
    }

    [Fact]
    public void NullArgumentsAreRejected()
    {
        var formatter=Formatter();var quantity=formatter.ParseJavaQuantity("1 A");
        Assert.Throws<ArgumentNullException>(()=>formatter.FormatJavaQuantity(null!));
        Assert.Throws<ArgumentNullException>(()=>formatter.FormatJavaQuantity(quantity,(StringBuilder)null!));
        Assert.Throws<ArgumentNullException>(()=>formatter.FormatJavaQuantity(quantity,(TextWriter)null!));
    }

    [Fact]
    public void WriterOwnershipAndFailuresStayWithCaller()
    {
        var formatter=Formatter();var quantity=formatter.ParseJavaQuantity("0.25 mA");
        var writer=new ProbeWriter();
        Assert.Same(writer,formatter.FormatJavaQuantity(quantity,writer));
        Assert.Equal("0.25 mA",writer.ToString());
        Assert.False(writer.Flushed);Assert.False(writer.Closed);
        writer.Reject=true;
        Assert.Throws<IOException>(()=>formatter.FormatJavaQuantity(quantity,writer));
        Assert.False(writer.Flushed);Assert.False(writer.Closed);
    }

    [Fact]
    public void SeededPrefixFuzzPreservesMalformedUtf16()
    {
        // A destination prefix is opaque text, including unmatched surrogates and controls.
        const int seed=385415;var random=new Random(seed);var formatter=Formatter();
        var quantity=formatter.ParseJavaQuantity("20 °C");
        for(var sample=0;sample<256;sample++)
        {
            var prefix=new string(Enumerable.Range(0,random.Next(1,80)).Select(_=>(char)random.Next(65536)).ToArray());
            var builder=new StringBuilder(prefix);formatter.FormatJavaQuantity(quantity,builder);
            Assert.Equal(prefix+"20 ℃",builder.ToString());
        }
    }

    private sealed class ProbeWriter:StringWriter
    {
        internal bool Reject,Flushed,Closed;
        public override void Write(string? value){if(Reject)throw new IOException("Deliberate destination failure");base.Write(value);}
        public override void Flush(){Flushed=true;base.Flush();}
        protected override void Dispose(bool disposing){Closed=true;base.Dispose(disposing);}
    }
}
