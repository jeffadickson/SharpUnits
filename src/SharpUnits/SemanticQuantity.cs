using System.Collections.ObjectModel;
namespace SharpUnits;
/// <summary>Stable semantic identity for physically distinct quantity kinds.</summary>
public readonly record struct QuantityKind
{
 /// <summary>Stable nonempty identifier suitable for serialization.</summary>
 public string Id {get;}
 /// <summary>Constructs a kind identifier.</summary>
 public QuantityKind(string id)
 {
  if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Kind ID required.",nameof(id));
  Id=id;
 }
 /// <summary>Energy.</summary>
 public static QuantityKind Energy=>new("si.energy");
 /// <summary>Torque.</summary>
 public static QuantityKind Torque=>new("si.torque");
 /// <summary>Voltage.</summary>
 public static QuantityKind Voltage=>new("si.voltage");
 /// <summary>Current.</summary>
 public static QuantityKind Current=>new("si.current");
 /// <summary>Resistance.</summary>
 public static QuantityKind Resistance=>new("si.resistance");
}
/// <summary>Registers physical quantity kinds independently of their dimensions.</summary>
public sealed class QuantityKindRegistry
{
 private readonly Dictionary<QuantityKind,RationalDimension> definitions=new();
 /// <summary>Registers a new kind.</summary>
 public bool Register(QuantityKind kind,RationalDimension dimension)
 {
  if(string.IsNullOrWhiteSpace(kind.Id))throw new ArgumentException("Invalid kind.",nameof(kind));
  return definitions.TryAdd(kind,dimension);
 }
 /// <summary>Gets the registered dimensions.</summary>
 public RationalDimension DimensionOf(QuantityKind kind)=>definitions.TryGetValue(kind,out var d)?d:throw new KeyNotFoundException($"Unknown kind: {kind.Id}");
 /// <summary>Creates a standard electrical and mechanical registry.</summary>
 public static QuantityKindRegistry CreateStandard()
 {
  var r=new QuantityKindRegistry();
  var energy=RationalDimension.From(new Dimension(2,1,-2,0,0,0,0));
  r.Register(QuantityKind.Energy,energy);
  r.Register(QuantityKind.Torque,energy);
  r.Register(QuantityKind.Voltage,RationalDimension.From(Dimension.Voltage));
  r.Register(QuantityKind.Current,RationalDimension.From(Dimension.ElectricCurrent));
  r.Register(QuantityKind.Resistance,RationalDimension.From(Dimension.Resistance));
  return r;
 }
}
/// <summary>Quantity carrying both dimensions and semantic physical identity.</summary>
public readonly record struct SemanticQuantity
{
 /// <summary>Coherent SI value.</summary>
 public Quantity Value {get;}
 /// <summary>Physical kind.</summary>
 public QuantityKind Kind {get;}
 /// <summary>Creates a semantically checked quantity.</summary>
 public SemanticQuantity(Quantity value,QuantityKind kind,QuantityKindRegistry registry)
 {
  ArgumentNullException.ThrowIfNull(registry);
  if(value.RationalDimension!=registry.DimensionOf(kind))throw new InvalidOperationException("Kind and dimensions disagree.");
  Value=value;Kind=kind;
 }
 /// <summary>Adds quantities with identical semantic kind.</summary>
 public SemanticQuantity Add(SemanticQuantity other,QuantityKindRegistry registry)
 {
  if(Kind!=other.Kind)throw new InvalidOperationException("Different physical kinds.");
  return new(Value+other.Value,Kind,registry);
 }
 /// <summary>Subtracts quantities with identical semantic kind.</summary>
 public SemanticQuantity Subtract(SemanticQuantity other,QuantityKindRegistry registry)
 {
  if(Kind!=other.Kind)throw new InvalidOperationException("Different physical kinds.");
  return new(Value-other.Value,Kind,registry);
 }
 /// <summary>Multiplies quantities with an explicitly chosen result kind.</summary>
 public SemanticQuantity MultiplyAs(SemanticQuantity other,QuantityKind resultKind,QuantityKindRegistry registry)=>new(Value*other.Value,resultKind,registry);
}
