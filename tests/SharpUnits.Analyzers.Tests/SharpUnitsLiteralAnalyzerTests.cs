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

    [Fact]
    public async Task TracksLocalVariablesForIncompatibleConversionsAndAffineAdditions()
    {
        const string code = """
            using SharpUnits;
            public static class LocalFlow
            {
                public static void Run()
                {
                    var v = Quantity.Of(5, "V");
                    _ = v.In("kg");

                    var t1 = Quantity.Of(20, Units.Celsius);
                    var t2 = Quantity.Of(25, Units.Celsius);
                    _ = t1 + t2;
                }
            }
            """;

        var diagnostics = await AnalyzeAsync(code);
        Assert.Contains(diagnostics, d => d.Id == "SU0003");
        Assert.Contains(diagnostics, d => d.Id == "SU0005");
    }

    [Fact]
    public async Task SuggestsClosestUnitOnTypoAndCodeFixProviderRewritesBothSU0001AndSU0005()
    {
        const string code = """
            using SharpUnits;
            public static class FixMe
            {
                public static void Run()
                {
                    _ = Quantity.Of(10, "kOhn");
                    _ = Quantity.Of(20, Units.Celsius) + Quantity.Of(5, Units.Celsius);
                }
            }
            """;

        using var workspace = new Microsoft.CodeAnalysis.AdhocWorkspace();
        var refs = new List<MetadataReference>();
        var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(Path.PathSeparator) ?? [];
        foreach (string path in tpa)
            if (File.Exists(path)) refs.Add(MetadataReference.CreateFromFile(path));
        refs.Add(MetadataReference.CreateFromFile(typeof(Quantity).Assembly.Location));

        var project = workspace.AddProject("FixProject", LanguageNames.CSharp)
            .WithMetadataReferences(refs)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var doc = project.AddDocument("FixMe.cs", Microsoft.CodeAnalysis.Text.SourceText.From(code));

        var compilation = await doc.Project.GetCompilationAsync();
        var withAnalyzers = compilation!.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SharpUnitsLiteralAnalyzer()));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();

        var typoDiag = Assert.Single(diagnostics, d => d.Id == "SU0001");
        Assert.Equal("kOhm", typoDiag.Properties["SuggestedUnit"]);
        Assert.Contains("Did you mean 'kOhm'?", typoDiag.GetMessage());

        var affineDiag = Assert.Single(diagnostics, d => d.Id == "SU0005");

        var fixer = new SharpUnitsCodeFixProvider();
        var actions1 = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(
            doc, typoDiag, (a, _) => actions1.Add(a), CancellationToken.None));
        Assert.Single(actions1);
        var ops1 = await actions1[0].GetOperationsAsync(CancellationToken.None);
        var changedDoc1 = ((Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation)ops1.Single()).ChangedSolution.GetDocument(doc.Id)!;
        string fixedText1 = (await changedDoc1.GetTextAsync()).ToString();
        Assert.Contains("\"kOhm\"", fixedText1);

        var actions2 = new List<Microsoft.CodeAnalysis.CodeActions.CodeAction>();
        await fixer.RegisterCodeFixesAsync(new Microsoft.CodeAnalysis.CodeFixes.CodeFixContext(
            doc, affineDiag, (a, _) => actions2.Add(a), CancellationToken.None));
        Assert.Single(actions2);
        var ops2 = await actions2[0].GetOperationsAsync(CancellationToken.None);
        var changedDoc2 = ((Microsoft.CodeAnalysis.CodeActions.ApplyChangesOperation)ops2.Single()).ChangedSolution.GetDocument(doc.Id)!;
        string fixedText2 = (await changedDoc2.GetTextAsync()).ToString();
        Assert.Contains("Quantity.Delta(5, Units.Celsius)", fixedText2);
    }
}
