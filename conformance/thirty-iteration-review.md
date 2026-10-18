# Thirty local Java-parity iterations

Thirty distinct regression scenarios were developed in **30 separate local Git commits**. No remote was configured and no intermediate pushes occurred. After completing all 30 iterations, the tests were imported into the public repository in three ten-case batches.

The tests are **CLR behavioral analogues**, not direct execution of the official Java TCK. They cover conversion, affine composition, conversion-step retention, dimensional algebra, prefixes, and unit API compatibility. Their upstream assertion identifiers are traceability hints, **not closure evidence**. No ledger assertion is marked VERIFIED by this change.

## Validation

- Offline structural review: 30 unique xUnit test methods, 30 local commits, and balanced C# braces.
- Numerical reference correction: `new UnitConversion(2,3).Then(new UnitConversion(5,7)).ToSi(0)` produces **22**.
- .NET SDK was unavailable locally. Actual compilation and execution must be verified through GitHub Actions.

The earlier ten-case local package is superseded by this suite. Existing repository tests have not been overwritten.

## Test-to-TCK behavioral traceability

| Iterations | Focus | TCK section |
| --- | --- | --- |
| 1-14 | identity, prefixes, affine temperature conversion | 4.2.3 |
| 15-20 | inverse, composition, steps, linearity | 4.2.3 |
| 21-23 | dimensions and compatibility | 4.2.1 |
| 24-30 | products, quotients, roots, prefixes, alternates, shifts | 4.2.1 |

**Do not mark any Java TCK assertion VERIFIED based only on these tests.** A full closure requires a source-method review, exact Java reference evidence, and a passing equivalent .NET test.
