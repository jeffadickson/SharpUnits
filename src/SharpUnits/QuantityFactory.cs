namespace SharpUnits;
/// <summary>Creates quantities from registered units and numeric magnitudes.</summary>
public interface IQuantityFactory
{
 /// <summary>Creates a coherent SI quantity from a value and named unit.</summary>
 Quantity Create(double value,string unitSymbol);
}
/// <summary>Default registry-backed quantity factory.</summary>
public sealed class QuantityFactory : IQuantityFactory
{
 private readonly UnitRegistry registry;
 /// <summary>Creates a factory using an explicit registry.</summary>
 public QuantityFactory(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
 /// <inheritdoc/>
 public Quantity Create(double value,string unitSymbol)=>registry.Resolve(unitSymbol).ToQuantity(value);
}