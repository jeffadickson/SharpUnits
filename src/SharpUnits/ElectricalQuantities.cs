namespace SharpUnits;
/// <summary>Scale-aware electrical quantity factories.</summary>
public static class ElectricalQuantities
{
 public static Current Milliamperes(double value)=>new(value/1000d);
 public static Resistance Kiloohms(double value)=>new(value*1000d);
}
/// <summary>Electrical current in amperes.</summary>
public readonly record struct Current(double Amperes)
{
 public static Voltage operator *(Current a,Resistance b)=>new(a.Amperes*b.Ohms);
 public static ElectricPower operator *(Current a,Voltage b)=>new(a.Amperes*b.Volts);
}
/// <summary>Resistance in ohms.</summary>
public readonly record struct Resistance(double Ohms)
{
 public static Voltage operator *(Resistance a,Current b)=>b*a;
}
/// <summary>Potential difference in volts.</summary>
public readonly record struct Voltage(double Volts)
{
 public static Current operator /(Voltage a,Resistance b)=>new(a.Volts/b.Ohms);
 public static Resistance operator /(Voltage a,Current b)=>new(a.Volts/b.Amperes);
 public static ElectricPower operator *(Voltage a,Current b)=>b*a;
}
/// <summary>Instantaneous electric power in watts.</summary>
public readonly record struct ElectricPower(double Watts);