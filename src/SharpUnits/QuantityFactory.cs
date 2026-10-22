namespace SharpUnits;
/// <summary>Creates quantities from registered units and numeric magnitudes.</summary>
public interface IQuantityFactory
{
 /// <summary>Creates a coherent SI quantity from a value and named unit.</summary>
 Quantity Create(double value,string unitSymbol);
}
/// <summary>Default registry-backed quantity factory.</summary>
public sealed class QuantityFactory : IQuantityFactory, IJavaQuantityFactory
{
 private readonly UnitRegistry registry;
 /// <summary>Creates a factory using an explicit registry.</summary>
 public QuantityFactory(UnitRegistry registry)=>this.registry=registry??throw new ArgumentNullException(nameof(registry));
 /// <inheritdoc/>
 public Quantity Create(double value,string unitSymbol)=>registry.Resolve(unitSymbol).ToQuantity(value);
 /// <inheritdoc/>
 public JavaQuantityAdapter CreateJavaQuantity(double value,string unitSymbol,JavaQuantityScale scale=JavaQuantityScale.Absolute)
  =>new(value,registry.Resolve(unitSymbol),scale);
}

/// <summary>Creates adapter quantities retaining a display magnitude, unit and scale.</summary>
/// <remarks>A CLR contract for registered symbols; Java service discovery and generic quantity types
/// are not represented. Numeric inputs use double precision.</remarks>
public interface IJavaQuantityFactory
{
 /// <summary>Creates an absolute quantity by default, or a relative interval when requested.</summary>
 /// <remarks>Relative values ignore the unit offset. Relative 10 degrees Celsius represents
 /// a 10-kelvin interval; absolute 10 degrees Celsius represents 283.15 kelvin.</remarks>
 /// <exception cref="KeyNotFoundException">The symbol is not registered.</exception>
 /// <exception cref="ArgumentOutOfRangeException">The magnitude, coherent value or scale is invalid.</exception>
 JavaQuantityAdapter CreateJavaQuantity(double value,string unitSymbol,JavaQuantityScale scale=JavaQuantityScale.Absolute);
}
