using SharpUnits;
using SharpUnits.Metrology;
using SharpUnits.SysMl;
using Xunit;

namespace SharpUnits.SysMl.Tests;

/// <summary>Tests for Cameo / SysML QUDV import/export, FMI XML, parametric constraint validation, and requirement verification.</summary>
public sealed class CameoSysMlBridgeTests
{
    [Fact]
    public void QudvCatalogRoundTripsJsonAndImportsCustomUnitsIncludingSourceOffsetAndRationalDimensions()
    {
        var catalog = new QudvCatalog
        {
            Name = "TestBench.Cameo.QUDV",
            QuantityKinds =
            [
                new QudvQuantityKind
                {
                    Id = "custom.noise_density",
                    Name = "Voltage Noise Spectral Density",
                    Dimensions = QudvDimensionExponents.From(RationalDimension.Voltage - RationalDimension.Frequency.Root(2))
                }
            ],
            Units =
            [
                new QudvUnit
                {
                    Symbol = "nV_rtHz",
                    Name = "nanovolt per root hertz",
                    Kind = QudvUnitKind.Derived,
                    QuantityKindId = "custom.noise_density",
                    Expression = "nV/Hz^(1/2)"
                },
                new QudvUnit
                {
                    Symbol = "°F_src",
                    Name = "Fahrenheit (source-unit offset convention)",
                    Kind = QudvUnitKind.AffineConversion,
                    QuantityKindId = "isq.temperature",
                    Factor = 5d / 9d,
                    Offset = 459.67d,
                    OffsetConvention = QudvOffsetConvention.SourceUnitOffset
                },
                new QudvUnit
                {
                    Symbol = "kbar",
                    Name = "kilobar",
                    Kind = QudvUnitKind.Prefixed,
                    PrefixSymbol = "k",
                    BaseUnitSymbol = "bar",
                    QuantityKindId = "isq.pressure"
                }
            ]
        };

        string json = QudvBridge.ToJson(catalog);
        var restored = QudvBridge.FromJson(json);
        var (env, specs) = QudvBridge.Import(restored);

        Assert.True(specs.ContainsKey("custom.noise_density"));
        var noiseUnit = env.ResolveUnit("nV_rtHz");
        Assert.Equal(RationalDimension.Voltage - RationalDimension.Frequency.Root(2), noiseUnit.Dimension);
        Assert.Equal(1.0, noiseUnit.ScaleToSi * 1e9, 12);

        // SourceUnitOffset: (212 + 459.67) * 5/9 = 373.15 K
        var fUnit = env.Registry.Resolve("°F_src");
        Assert.Equal(373.15, fUnit.Converter.ToSi(212), 9);

        // Prefixed QUDV unit: 1 kbar = 10^8 Pa
        Assert.Equal(1e8, env.Registry.Resolve("kbar").Converter.Scale, 6);

        // Standard catalog export round-trips cleanly
        var stdJson = QudvBridge.ToJson(QudvBridge.ExportStandardCatalog());
        var stdRestored = QudvBridge.FromJson(stdJson);
        Assert.True(stdRestored.Units.Count >= 50);
    }

    [Fact]
    public void FmiUnitDefinitionsXmlRoundTripsLinearAndAffineUnits()
    {
        UnitDefinition[] exported = [Units.Volt, Units.Millivolt, Units.Celsius, Units.Fahrenheit, Units.Bar, Units.Kiloohm];
        var xml = FmiUnitXml.ExportUnitDefinitions(exported);

        Assert.Equal("UnitDefinitions", xml.Name.LocalName);
        var reimported = FmiUnitXml.ImportUnitDefinitions(xml);
        Assert.Equal(exported.Length, reimported.Count);

        var cRe = reimported.Single(u => u.Symbol == "°C");
        Assert.Equal(373.15, cRe.Converter.ToSi(100), 9);

        var fRe = reimported.Single(u => u.Symbol == "°F");
        Assert.Equal(373.15, fRe.Converter.ToSi(212), 9);

        var kOhmRe = reimported.Single(u => u.Symbol == "kΩ");
        Assert.Equal(RationalDimension.Resistance, kOhmRe.RationalDimension);
        Assert.Equal(1000d, kOhmRe.Converter.Scale, 9);
    }

    [Fact]
    public void CameoConstraintBlockValidationInfersUnknownDimensionsAndRejectsMismatches()
    {
        var env = UnitEnvironment.CreateStandard();

        var validBlock = new QudvConstraintBlock
        {
            Name = "OhmsLawAndPower",
            Parameters =
            [
                new QudvConstraintParameter("v", "isq.voltage", "V"),
                new QudvConstraintParameter("i", "isq.current", "A"),
                new QudvConstraintParameter("r"), // inferred!
                new QudvConstraintParameter("p")  // inferred!
            ],
            Equations = ["v = i * r", "p = v * i"]
        };

        var result = CameoModelValidator.ValidateConstraintBlock(validBlock, env);
        Assert.True(result.IsValid);
        Assert.Equal(RationalDimension.Resistance, result.Symbols["r"].Dimension);
        Assert.Equal(RationalDimension.Power, result.Symbols["p"].Dimension);

        var invalidBlock = new QudvConstraintBlock
        {
            Name = "BadSum",
            Parameters =
            [
                new QudvConstraintParameter("v", "isq.voltage", "V"),
                new QudvConstraintParameter("i", "isq.current", "A")
            ],
            Equations = ["v = v + i"]
        };

        Assert.False(CameoModelValidator.ValidateConstraintBlock(invalidBlock, env).IsValid);
    }

    [Fact]
    public void CameoRequirementVerificationAppliesUnitScalingGuardBandAndCalibration()
    {
        var env = UnitEnvironment.CreateStandard();
        var req = new QudvRequirementLimit
        {
            RequirementId = "REQ-PSU-001",
            QuantityKindId = QuantityKind.Voltage.Id,
            UnitSymbol = "mV",
            Minimum = 4900,
            Maximum = 5100,
            GuardBand = 10, // 10 mV guard band -> guarded [4.91 V, 5.09 V]
            CoverageFactor = 2.0
        };

        var cal = new CalibrationRecord(
            "CAL-2026",
            "DMM-01",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2027-01-01T00:00:00Z"));

        var passObs = new InstrumentMeasurement(
            new SemanticQuantity(SiQuantities.Volts(5.00), QuantityKind.Voltage, env.Kinds),
            standardUncertainty: 0.01, // expanded = 0.02 -> [4.98, 5.02] inside [4.91, 5.09]
            "DMM-01",
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            "CAL-2026");

        var guardBandObs = new InstrumentMeasurement(
            new SemanticQuantity(SiQuantities.Volts(5.085), QuantityKind.Voltage, env.Kinds),
            standardUncertainty: 0.005, // expanded = 0.01 -> [5.075, 5.095] overlaps 5.09 guard limit
            "DMM-01",
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            "CAL-2026");

        var failObs = new InstrumentMeasurement(
            new SemanticQuantity(SiQuantities.Volts(5.20), QuantityKind.Voltage, env.Kinds),
            standardUncertainty: 0.01,
            "DMM-01",
            DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            "CAL-2026");

        Assert.Equal(MeasurementDisposition.Pass, CameoModelValidator.VerifyRequirement(req, passObs, cal, env));
        Assert.Equal(MeasurementDisposition.Indeterminate, CameoModelValidator.VerifyRequirement(req, guardBandObs, cal, env));
        Assert.Equal(MeasurementDisposition.Fail, CameoModelValidator.VerifyRequirement(req, failObs, cal, env));
    }
}
