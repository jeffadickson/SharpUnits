using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SharpUnits.Analyzers;
using Xunit;

namespace SharpUnits.Analyzers.Tests;

/// <summary>Compile-time Roslyn analyzer tests for SU0001-SU0005.</summary>
public sealed class SharpUnitsLiteralAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        var refs = new List<MetadataReference>();
        var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator) ?? [];
        foreach (string path in tpa)
        {
            if (File.Exists(path))
                refs.Add(MetadataReference.CreateFromFile(path));
        }
        refs.Add(MetadataReference.CreateFromFile(typeof(Quantity).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            [tree],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SharpUnitsLiteralAnalyzer()));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    [Fact]
    public async Task ValidUnitAndQuantityLiteralsProduceZeroDiagnostics()
    {
        const string code = """
            using SharpUnits;
            public static class ValidSamples
            {
                public static void Run()
                {
                    var a = Quantity.Parse("250 mA");
                    var b = Quantity.Parse("10 nV/Hz^(1/2)");
                    var c = Quantity.Parse("10 Δ°C");
                    var d = Quantity.Of(12.5, "kOhm").In("Ω");
                    var e = 4.7.WithUnit("uF").AsValueIn("nF");
                    var f = UnitRegistry.Standard.Convert(212, "°F", "°C");
                    var g = UnitEnvironment.Standard.ResolveUnit("m·s⁻²");
                    var h = Quantity.Of(20, Units.Celsius) + Quantity.Delta(5, Units.Celsius);
                    _ = Quantity.TryParse("invalid input is allowed in TryParse", null, out _);
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ReportsSU0001ForUnknownUnitSymbolsAndInvalidPrefixes()
    {
        const string code = """
            using SharpUnits;
            public static class BadUnits
            {
                public static void Run()
                {
                    _ = Quantity.Of(10, "furlongs");
                    _ = 5.0.WithUnit("m°C");
                    _ = UnitRegistry.Standard.Resolve("mkg");
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Equal(3, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("SU0001", d.Id));
    }

    [Fact]
    public async Task ReportsSU0002ForMalformedQuantityLiterals()
    {
        const string code = """
            using SharpUnits;
            public static class BadQuantities
            {
                public static void Run()
                {
                    _ = Quantity.Parse("not a quantity");
                    _ = Quantity.Parse("5 blargons");
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("SU0002", d.Id));
    }

    [Fact]
    public async Task ReportsSU0003ForIncompatibleDimensionalConversions()
    {
        const string code = """
            using SharpUnits;
            public static class BadConversions
            {
                public static void Run()
                {
                    _ = UnitRegistry.Standard.Convert(1, "V", "A");
                    _ = Quantity.Of(5, "V").In("kg");
                    _ = Quantity.Of(10, Units.Meter).AsValueIn("s");
                    _ = Quantity.Parse("250 mA").In(Units.Volt);
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Equal(4, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("SU0003", d.Id));
    }

    [Fact]
    public async Task ReportsSU0004ForAffineUnitInCompoundExpression()
    {
        const string code = """
            using SharpUnits;
            public static class BadAffineExpr
            {
                public static void Run()
                {
                    var p = new UnitExpressionParser(UnitRegistry.Standard);
                    _ = p.Parse("°C*A");
                    _ = Quantity.Parse("10 °F/s");
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal("SU0004", d.Id));
    }

    [Fact]
    public async Task ReportsSU0005WarningForAddingTwoAbsoluteAffineTemperatures()
    {
        const string code = """
            using SharpUnits;
            public static class BadAffineAdd
            {
                public static void Run()
                {
                    _ = Quantity.Of(20, Units.Celsius) + Quantity.Of(25, Units.Celsius);
                    _ = Quantity.Parse("20 °C") + Quantity.Parse("70 °F");
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d =>
        {
            Assert.Equal("SU0005", d.Id);
            Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
        });
    }
}
