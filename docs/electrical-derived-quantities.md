# Typed electrical quantities

SharpUnits models electrical operations using C# value types, rather than relying exclusively on runtime dimensional checks.

| Equation | Typed result |
|---|---|
| I × R = V | Voltage |
| V / I = R | Resistance |
| I × t = Q | ElectricCharge |
| C × V = Q | ElectricCharge |
| G × V = I | Current |
| L × I = Ψ | FluxLinkage |

These operations currently use `double` magnitudes in coherent SI units. They preserve physical type relationships but do not enforce nonzero denominators or finite results in every operator; such numerical domain constraints remain separate requirements. `FluxLinkage` is a single-turn equivalent in webers and is not a full magnetic-circuit model.

The new `ElectricalDerivedQuantitiesTests` exercise these identities. XML API comments are included in the source. Java TCK assertion closure remains separate from these .NET-specific typed extensions.
