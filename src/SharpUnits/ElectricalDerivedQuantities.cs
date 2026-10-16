namespace SharpUnits;
/// <summary>Typed electrical charge measured in coulombs.</summary>
public readonly record struct ElectricCharge(double Coulombs)
{
 /// <summary>Charge divided by time produces current.</summary>
 public static Current operator /(ElectricCharge charge,Duration duration)=>new(charge.Coulombs/duration.Seconds);
 /// <summary>Charge divided by voltage produces capacitance.</summary>
 public static Capacitance operator /(ElectricCharge charge,Voltage voltage)=>new(charge.Coulombs/voltage.Volts);
}
/// <summary>Elapsed time measured in seconds.</summary>
public readonly record struct Duration(double Seconds)
{
 /// <summary>Current times duration produces charge.</summary>
 public static ElectricCharge operator *(Duration duration,Current current)=>new(duration.Seconds*current.Amperes);
}
/// <summary>Capacitance measured in farads.</summary>
public readonly record struct Capacitance(double Farads)
{
 /// <summary>Capacitance times voltage produces charge.</summary>
 public static ElectricCharge operator *(Capacitance capacitance,Voltage voltage)=>new(capacitance.Farads*voltage.Volts);
}
/// <summary>Inductance measured in henries.</summary>
public readonly record struct Inductance(double Henries)
{
 /// <summary>Inductance times current produces magnetic flux linkage in webers.</summary>
 public static FluxLinkage operator *(Inductance inductance,Current current)=>new(inductance.Henries*current.Amperes);
}
/// <summary>Magnetic flux linkage measured in webers (single-turn equivalent).</summary>
public readonly record struct FluxLinkage(double Webers);
/// <summary>Electrical conductance measured in siemens.</summary>
public readonly record struct Conductance(double Siemens)
{
 /// <summary>Conductance times voltage produces current.</summary>
 public static Current operator *(Conductance conductance,Voltage voltage)=>new(conductance.Siemens*voltage.Volts);
}
/// <summary>Additional typed electrical algebra.</summary>
public static class ElectricalRelations
{
 /// <summary>Current integrated over a duration produces charge.</summary>
 public static ElectricCharge Charge(Current current,Duration duration)=>duration*current;
 /// <summary>Charge per unit time produces current.</summary>
 public static Current Current(ElectricCharge charge,Duration duration)=>charge/duration;
 /// <summary>Voltage divided by current produces resistance.</summary>
 public static Resistance Resistance(Voltage voltage,Current current)=>voltage/current;
 /// <summary>Resistance inverted produces conductance.</summary>
 public static Conductance Conductance(Resistance resistance)=>new(1d/resistance.Ohms);
 /// <summary>Capacitance times voltage produces charge.</summary>
 public static ElectricCharge Charge(Capacitance capacitance,Voltage voltage)=>capacitance*voltage;
}
