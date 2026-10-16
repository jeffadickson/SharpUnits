namespace SharpUnits;
/// <summary>Immutable decimal SI prefix with Java JSR 385-style public metadata.</summary>
public sealed record SiPrefix
{
 /// <summary>Human-readable prefix name.</summary>
 public string Name {get;}
 /// <summary>Prefix symbol.</summary>
 public string Symbol {get;}
 /// <summary>Decimal exponent of ten.</summary>
 public int Exponent {get;}
 /// <summary>Prefix numeric factor in double precision.</summary>
 public double Value=>Math.Pow(10,Exponent);
 /// <summary>Constructs a valid SI prefix.</summary>
 public SiPrefix(string name,string symbol,int exponent)
 {
  if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Name required.",nameof(name));
  if(string.IsNullOrWhiteSpace(symbol))throw new ArgumentException("Symbol required.",nameof(symbol));
  if(exponent is < -30 or > 30)throw new ArgumentOutOfRangeException(nameof(exponent));
  Name=name;Symbol=symbol;Exponent=exponent;
 }
 /// <summary>Applies this prefix to a linear unit.</summary>
 public UnitDefinition Apply(UnitDefinition unit)=>SiPrefixes.Apply(Symbol,unit);
}
/// <summary>Canonical SI prefix metadata including the 2022 additions.</summary>
public static class SiPrefixCatalog
{
 private static readonly SiPrefix[] Items=
 {
  new("quetta","Q",30),new("ronna","R",27),new("yotta","Y",24),new("zetta","Z",21),
  new("exa","E",18),new("peta","P",15),new("tera","T",12),new("giga","G",9),
  new("mega","M",6),new("kilo","k",3),new("hecto","h",2),new("deca","da",1),
  new("deci","d",-1),new("centi","c",-2),new("milli","m",-3),new("micro","µ",-6),
  new("nano","n",-9),new("pico","p",-12),new("femto","f",-15),new("atto","a",-18),
  new("zepto","z",-21),new("yocto","y",-24),new("ronto","r",-27),new("quecto","q",-30)
 };
 /// <summary>Returns all canonical prefixes.</summary>
 public static IReadOnlyList<SiPrefix> All=>Array.AsReadOnly(Items);
 /// <summary>Looks up a prefix by exact case-sensitive symbol.</summary>
 public static SiPrefix Resolve(string symbol)=>Items.FirstOrDefault(p=>p.Symbol==symbol)??throw new KeyNotFoundException(symbol);
}
