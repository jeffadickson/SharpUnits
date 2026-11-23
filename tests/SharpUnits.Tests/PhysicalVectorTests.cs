using System.Numerics;
using System.Text.Json;
using SharpUnits;
using Xunit;

namespace SharpUnits.Tests;

/// <summary>Tests 2-D and 3-D physical vectors, affine spatial points, quaternion/polar rotations, and serialization.</summary>
public sealed class PhysicalVectorTests
{
    [Fact]
    public void Vector2AlgebraPolarRotationAndDropDownWork()
    {
        var d1 = QuantityVector2<double>.Of(3, 0, Units.Meter);
        var d2 = QuantityVector2<double>.Of(0, 4000, "mm");
        var sum = d1 + d2; // Promotes to finer common unit "mm": (3000, 4000) mm
        Assert.Equal("mm", sum.Unit.Symbol);
        Assert.Equal(5000.0, sum.Magnitude().Value, 9);
        Assert.Equal(5.0, sum.In(Units.Meter).Magnitude().Value, 12);

        // MagnitudeSquared preserves squared unit
        var magSq = d1.MagnitudeSquared();
        Assert.Equal(9.0, magSq.Value, 12);
        Assert.Equal(Units.Meter.Pow(2), magSq.Unit);

        // Polar construction and 90° rotation
        var polar = QuantityVector2<double>.FromPolar(Quantity<double>.Of(10, Units.Newton), Quantity<double>.Of(90, Units.Degree));
        Assert.Equal(0.0, polar.X, 12);
        Assert.Equal(10.0, polar.Y, 12);
        var rotated = QuantityVector2<double>.Of(1, 0, Units.Meter).Rotate(Quantity<double>.Of(90, Units.Degree));
        Assert.Equal(0.0, rotated.X, 12);
        Assert.Equal(1.0, rotated.Y, 12);
        Assert.Equal(Math.PI / 2.0, QuantityVector2<double>.AngleBetween(d1, d2).Value, 12);

        // 2-D CrossZ on Length x Force infers Torque!
        var r = QuantityVector2<double>.Of(2, 0, Units.Meter);
        var f = QuantityVector2<double>.Of(0, 5, Units.Newton);
        var torqueZ = QuantityVector2<double>.CrossZ(r, f);
        Assert.Equal(10.0, torqueZ.Value, 12);
        Assert.Equal(Isq.Torque, torqueZ.Spec);

        // Frictionless drop to System.Numerics.Vector2 and back
        Vector2 raw = sum.ToVector2In(Units.Meter);
        Assert.Equal(3f, raw.X, 4);
        Assert.Equal(4f, raw.Y, 4);
        var lifted = QuantityVector2<float>.FromVector2(raw, Units.Meter);
        var (lx, ly, lUnit) = lifted;
        Assert.Equal(3f, lx, 4);
        Assert.Equal(4f, ly, 4);
        Assert.Equal(Units.Meter, lUnit);
    }

    [Fact]
    public void Vector3PhysicsDotCrossTorqueLorentzAndQuaternionRotationWork()
    {
        // Work = Force · Displacement => 10 N · 3 m = 30 N·m (Energy dimension)
        var force = QuantityVector3<double>.Of(10, 0, 0, Units.Newton);
        var disp = QuantityVector3<double>.Of(3, 4, 0, Units.Meter);
        var work = QuantityVector3<double>.Dot(force, disp);
        Assert.Equal(30.0, work.Value, 12);
        Assert.Equal(RationalDimension.Energy, work.Dimension);

        // Torque = r × F => (0, 0, 10) N·m tagged with Isq.Torque!
        var lever = QuantityVector3<double>.Of(2, 0, 0, Units.Meter);
        var forceY = QuantityVector3<double>.Of(0, 5, 0, Units.Newton);
        var torque = QuantityVector3<double>.Cross(lever, forceY);
        Assert.Equal(0.0, torque.X, 12);
        Assert.Equal(0.0, torque.Y, 12);
        Assert.Equal(10.0, torque.Z, 12);
        Assert.Equal(Isq.Torque, torque.Spec);

        // Lorentz E = v × B => V/m (ElectricFieldStrength!)
        var vel = QuantityVector3<double>.Of(100, 0, 0, Units.Meter / Units.Second);
        var bField = QuantityVector3<double>.Of(0, 0.5, 0, Units.Tesla);
        var eField = QuantityVector3<double>.Cross(vel, bField);
        Assert.Equal(50.0, eField.Z, 12);
        Assert.Equal(RationalDimension.ElectricFieldStrength, eField.Dimension);

        // Physical scalar * vector: Mass * AccelerationVector => ForceVector
        var mass = Quantity<double>.Of(2.0, Units.Kilogram);
        var accel = QuantityVector3<double>.Of(0, 0, 1.0, Units.StandardGravity).In(Units.Meter / Units.Second.Pow(2));
        var weight = mass * accel;
        Assert.Equal(RationalDimension.Force, weight.Dimension);
        Assert.Equal(19.6133, weight.Z, 4);

        // Vector / Duration => VelocityVector
        var velocity = disp / Quantity<double>.Of(2.0, Units.Second);
        Assert.Equal(1.5, velocity.X, 12);
        Assert.Equal(2.0, velocity.Y, 12);
        Assert.Equal(RationalDimension.Velocity, velocity.Dimension);

        // 90-degree Quaternion rotation around Z: (1, 0, 0) m -> (0, 1, 0) m
        var rotZ90 = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2f);
        var rotated = QuantityVector3<double>.Of(1, 0, 0, Units.Meter).Rotate(rotZ90);
        Assert.Equal(0.0, rotated.X, 6);
        Assert.Equal(1.0, rotated.Y, 6);
        Assert.Equal(0.0, rotated.Z, 6);
        Assert.Equal(Units.Meter, rotated.Unit);

        // Spherical construction & Vector3 drop-down
        var sph = QuantityVector3<double>.FromSpherical(
            Quantity<double>.Of(10, Units.Meter),
            Quantity<double>.Of(90, Units.Degree),
            Quantity<double>.Of(0, Units.Degree));
        Vector3 v3 = sph.ToVector3In(Units.Meter);
        Assert.Equal(10f, v3.X, 4);
        Assert.Equal(0f, v3.Y, 4);
        Assert.Equal(0f, v3.Z, 4);
    }

    [Fact]
    public void AffineSpatialPointsEnforceFramesAndGeometry()
    {
        var p1 = QuantityPoint3<double>.At(1, 2, 3, Units.Meter, SpatialOrigins.Enu);
        var p2 = QuantityPoint3<double>.At(4000, 6000, 3000, SiPrefixes.Apply("m", Units.Meter), SpatialOrigins.Enu);
        var delta = p2 - p1; // Promotes cleanly to finer unit!
        Assert.Equal(5.0, delta.In(Units.Meter).Magnitude().Value, 9);
        Assert.Equal(5.0, QuantityPoint3<double>.Distance(p1, p2).In(Units.Meter).Value, 9);

        var mid = QuantityPoint3<double>.Midpoint(p1, QuantityPoint3<double>.At(5, 6, 7, Units.Meter, SpatialOrigins.Enu));
        Assert.Equal(3.0, mid.X, 12);
        Assert.Equal(4.0, mid.Y, 12);
        Assert.Equal(5.0, mid.Z, 12);

        // Different frames (ENU vs NED) cannot be subtracted!
        var pNed = QuantityPoint3<double>.At(1, 2, 3, Units.Meter, SpatialOrigins.Ned);
        Assert.Throws<InvalidOperationException>(() => p1 - pNed);
    }

    [Fact]
    public void PhysicalVectorsRoundTripThroughJsonDtosAndBinaryFraming()
    {
        var opts = new JsonSerializerOptions().AddSharpUnits();

        var v2 = QuantityVector2<double>.Of(1.5, -2.5, Units.Millivolt, Isq.Voltage);
        var v2JsonBack = JsonSerializer.Deserialize<QuantityVector2<double>>(JsonSerializer.Serialize(v2, opts), opts);
        Assert.Equal(v2, v2JsonBack);
        Assert.Equal(v2, SharpUnitsBinary.ReadQuantityVector2<double>(SharpUnitsBinary.ToBytes(v2), out _));

        var v3 = QuantityVector3<float>.Of(1f, 2f, 3f, Units.Meter / Units.Second);
        var v3JsonBack = JsonSerializer.Deserialize<QuantityVector3<float>>(JsonSerializer.Serialize(v3, opts), opts);
        Assert.Equal(v3, v3JsonBack);
        Assert.Equal(v3, SharpUnitsBinary.ReadQuantityVector3<float>(SharpUnitsBinary.ToBytes(v3), out _));

        // Plain POCO DTO round-trip with default serializer (no SharpUnits converters)
        var pt3 = QuantityPoint3<double>.At(10, 20, 30, Units.Meter, SpatialOrigins.Body);
        var pt3JsonBack = JsonSerializer.Deserialize<QuantityPoint3<double>>(JsonSerializer.Serialize(pt3, opts), opts);
        Assert.Equal(pt3, pt3JsonBack);
        string dtoJson = JsonSerializer.Serialize(pt3.ToData());
        Assert.Equal(pt3, QuantityPoint3<double>.FromData(JsonSerializer.Deserialize<QuantityPoint3Data<double>>(dtoJson)));
    }
}
