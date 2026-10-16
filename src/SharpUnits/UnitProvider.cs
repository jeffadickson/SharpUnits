namespace SharpUnits;
/// <summary>Provides unit lookup and quantity creation for a named system of units.</summary>
public interface IUnitProvider
{
 /// <summary>Returns the provider name.</summary>
 string Name {get;}
 /// <summary>Resolves a registered unit symbol.</summary>
 UnitDefinition Resolve(string symbol);
 /// <summary>Creates a quantity from a numeric magnitude and unit symbol.</summary>
 Quantity CreateQuantity(double value,string symbol);
}
/// <summary>Simple isolated provider backed by a unit registry.</summary>
public sealed class RegistryUnitProvider : IUnitProvider
{
 private readonly UnitRegistry registry;
 /// <inheritdoc/>
 public string Name {get;}
 /// <summary>Creates a named provider.</summary>
 public RegistryUnitProvider(string name,UnitRegistry registry)
 {
  if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Provider name required",nameof(name));
  Name=name;
  this.registry=registry??throw new ArgumentNullException(nameof(registry));
 }
 /// <inheritdoc/>
 public UnitDefinition Resolve(string symbol)=>registry.Resolve(symbol);
 /// <inheritdoc/>
 public Quantity CreateQuantity(double value,string symbol)=>Resolve(symbol).ToQuantity(value);
}
/// <summary>Thread-safe provider registration and lookup.</summary>
public sealed class UnitProviderCatalog
{
 private readonly System.Collections.Concurrent.ConcurrentDictionary<string,IUnitProvider> providers=new(StringComparer.Ordinal);
 /// <summary>Registers a unique provider name.</summary>
 public bool Register(IUnitProvider provider)
 {
  ArgumentNullException.ThrowIfNull(provider);
  return providers.TryAdd(provider.Name,provider);
 }
 /// <summary>Resolves a named provider.</summary>
 public IUnitProvider Resolve(string name)=>providers.TryGetValue(name,out var p)?p:throw new KeyNotFoundException(name);
 /// <summary>Returns a snapshot of registered provider names.</summary>
 public IReadOnlyList<string> Names=>providers.Keys.OrderBy(n=>n,StringComparer.Ordinal).ToArray();
}