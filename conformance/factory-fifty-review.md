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

| Reviews | Focus | .NET tests passing | Shared cases | Mismatches |
|---|---|---:|---:|---:|
| 1–10 | Both scales, including the TCK's length unit | 583 | 218 | 0 |
| 11–20 | Display precision, subnormal values and signed zero | 595 | 228 | 0 |
| 21–30 | Affine/linear conversion after factory construction | 605 | 238 | 0 |
| 31–40 | Default absolute scale | 615 | 248 | 0 |
| 41–50 | Exact supplied unit object retained | 625 | 258 | 0 |

Baseline: 571 passing .NET tests. Checkpoints compile with .NET SDK 10.0.402.
All 50 new observations match Java exactly, with zero numeric tolerance. Existing
cases retain their documented tolerances. Two signed-zero mismatches were corrected;
the factory API itself was absent before this review, so no fabricated pre-change
factory output is recorded. Existing XML documentation warnings remain; builds have
zero errors.

Pinned Java TCK: **112 tests, 0 failures/errors/skips**, including both factory
methods and all 16 quantity interface methods. Python comparator/expectation checks:
10 passing tests. Independent Fraction oracle: 200 cases, zero mismatches.

Review commits: `42f7d81` (initial review), `7fd18f8` (1–10), `a2c44cd` (11–20),
`0746c3b` (21–30), `4ee92d1` (31–40); the final increment contains 41–50 and this
evidence. The [machine-readable record](factory-review-50.json) retains Java/.NET
observations, the signed-zero before results, source fingerprints and checkpoints.

## Traceability and remaining incompatibilities

`51-A2` advances from GAP to PARTIAL_BEHAVIORAL_MAPPING. `51-A1` remains GAP:
automatic Java provider discovery and factories for all Java quantity subtypes are
absent. Ledger totals: **46 GAP, 50 IMPLEMENTED_UNVERIFIED,
17 PARTIAL_BEHAVIORAL_MAPPING, 0 VERIFIED**. No formal assertion closures.

The factory accepts double magnitudes and registered symbols. It does not retain
Java Number wrapper identity or arbitrary decimal precision, enforce a Java generic
quantity type, expose factory system-unit discovery, or create mixed quantities.
The adapter requires a finite coherent double even where a Java quantity could
defer conversion; coherent overflow remains an incompatibility. Both implementations
reject nonfinite input values. Registry and provider availability are explicit in
the CLR implementation. Formatting, Java parser grammar, general compound-unit
identity and service-loader compatibility remain outstanding.

Reproduce using the existing pinned Java runner and
`SharpUnits.ConformanceRunner` against `conformance/java-reference/cases.json`,
then run `tools/compare_java_dotnet.py` and `tools/check_fixture_expectations.py`
on both outputs. Run `dotnet test tests/SharpUnits.Tests -c Release` with .NET 10,
`tools/run_pinned_tck.py` with the configured Java 17 toolchain,
`tools/validate_tck_ledger.py`, and `tools/audit_tck_test_links.py`.

Both GitHub workflows were confirmed `disabled_manually`; source remains manual
dispatch only. No remote Ubuntu/Windows CI proof or reviewer closure is claimed.
