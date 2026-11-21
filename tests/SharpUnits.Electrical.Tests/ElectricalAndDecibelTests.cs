using SharpUnits;
using SharpUnits.Electrical;
using Xunit;

namespace SharpUnits.Electrical.Tests;

/// <summary>Tests for enriched electrical operators, prefixed factories, and decibel/RF levels.</summary>
public sealed class ElectricalAndDecibelTests
{
    [Fact]
    public void ElectricalStructsSupportFullArithmeticAndPrefixedFactories()
    {
        var v1 = Voltage.FromMillivolts(500);
        var v2 = Voltage.FromVolts(1.5);
        Assert.Equal(2.0, (v1 + v2).Volts, 12);
        Assert.Equal(1.0, (v2 - v1).Volts, 12);
        Assert.True(v2 > v1);
        Assert.Equal(v2, Voltage.FromQuantity(v2.ToQuantity()));

        var rParallel = Resistance.Parallel(Resistance.FromKiloohms(10), Resistance.FromKiloohms(10));
        Assert.Equal(5.0, rParallel.Kiloohms, 12);

        var tauRc = Resistance.FromKiloohms(10) * Capacitance.FromMicrofarads(1);
        Assert.Equal(10.0, tauRc.Milliseconds, 12);

        var tauRl = Inductance.FromMillihenries(20) / Resistance.FromOhms(100);
        Assert.Equal(200.0, tauRl.Microseconds, 12);

        var p = v2 * Current.FromMilliamperes(100);
        Assert.Equal(150.0, p.Milliwatts, 12);
        Assert.Equal(100.0, (p / v2).Milliamperes, 12);
    }

    [Fact]
    public void DecibelsPowerAndVoltageLevelsRoundTrip()
    {
        Assert.Equal(3.0102999566, Decibels.FromPowerRatio(2).Value, 6);
        Assert.Equal(6.0205999133, Decibels.FromAmplitudeRatio(2).Value, 6);
        Assert.Equal(10.0, new Decibels(10).ToPowerRatio(), 9);
        Assert.Equal(10.0, new Decibels(20).ToAmplitudeRatio(), 9);

        var zeroDbm = PowerLevelDbm.FromPower(ElectricPower.FromMilliwatts(1));
        Assert.Equal(0.0, zeroDbm.Value, 12);
        Assert.Equal(0.22360679775, zeroDbm.ToRmsVoltage().Volts, 9); // 1 mW into 50 Ω

        var boosted = zeroDbm + new Decibels(30);
        Assert.Equal(1.0, boosted.ToPower().Watts, 9);
        Assert.Equal(30.0, (boosted - zeroDbm).Value, 12);

        var dbv = VoltageLevelDbv.FromVoltage(Voltage.FromMillivolts(100));
        Assert.Equal(-20.0, dbv.Value, 12);
        Assert.Equal(0.1, dbv.ToVoltage().Volts, 12);
    }

    [Fact]
    public void FluentElectricalExtensionsComposeWithoutCoreAmbiguity()
    {
        var v = 250.0.Milliamperes() * 4.0.Kiloohms();
        Assert.Equal(1000.0, v.Volts, 12);

        var tau = 10.0.Kiloohms() * 1.0.Microfarads();
        Assert.Equal(10.0, tau.Milliseconds, 12);

        var tx = 0.0.Dbm() + 30.0.Decibels();
        Assert.Equal(1.0, tx.ToPower().Watts, 9);
    }
}
