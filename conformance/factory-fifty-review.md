# Factory compatibility review

Starting implementation: `aff2374`. GitHub CI remains disabled at the user's request.

This review addresses factory display-value, unit and scale preservation through 50
numbered executable scenarios, with .NET 10 compilation and full regression tests
after each ten. These are 50 scenario reviews, not 50 commits or TCK closures.

The pinned `QuantityFactoryTest.testQuantityFactoryScale` (`51-A2`) creates length
quantities through every Java service provider and checks both scale values.
`testQuantityFactoryExists` (`51-A1`) additionally requires factories for all
discovered Java quantity types. A CLR registry factory does not satisfy that
discovery contract or Java generic type identity.

The existing `IQuantityFactory.Create` returns a coherent `Quantity` and cannot
retain a display unit or scale. An additive API will expose the adapter without
changing that existing contract. Differential scenarios will invoke the real
Java `ServiceProvider.current().getQuantityFactory(...).create(...)`, rather than
constructing Java quantities directly.

Source review: pinned `upstream/unit-tck/.../spi/QuantityFactoryTest.java` and
`upstream/indriya/.../quantity/DefaultQuantityFactory.java` (`create` overloads
delegate to `Quantities.getQuantity`). Numeric coverage is limited to double;
this work does not establish arbitrary Java Number precision.

## Factory usage and temperature semantics

```csharp
var registry = UnitRegistry.CreateStandard();
IJavaQuantityFactory factory = new QuantityFactory(registry);
var absolute = factory.CreateJavaQuantity(10, "°C"); // 283.15 K
var interval = factory.CreateJavaQuantity(10, "°C", JavaQuantityScale.Relative); // 10 K interval
```

Both quantities retain magnitude 10 and the registered Celsius unit object. The
two-argument overload defaults to absolute. The existing `IQuantityFactory.Create`
still returns a coherent SI-only `Quantity`; it cannot represent scale identity.
Use the dedicated temperature types to enforce physical temperature arithmetic.

Construction canonicalizes signed zero, matching pinned Indriya's
`NumberQuantity -> Calculator -> DefaultNumberSystem.narrow` path. Two shared
scenarios initially failed because the adapter retained negative zero. Their
before/after outputs are retained in the machine-readable review. Small nonzero
display values remain intact even when their coherent representation rounds.

## Checkpoints

Pending execution. No assertions are formally verified by this review.
