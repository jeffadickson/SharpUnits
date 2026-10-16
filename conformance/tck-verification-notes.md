# TCK verification evidence and limitations

Baseline GitHub Actions run: https://github.com/jeffadickson/SharpUnits/actions/runs/38830675629
All three OS jobs succeeded at commit 0660c9cf. This establishes successful .NET test execution, not Java TCK certification.

The 113 assertion ledger distinguishes implemented tests from verified specification assertions. Many official TCK cases are Java reflection or service-provider contract tests; behavioral resemblance alone is not sufficient to close them. Do not count the new behavioral test methods as verified TCK assertions without checking each upstream method's complete assertions, its setup/fixtures, and Java reference behavior.

Current milestone remains open. Missing capabilities include Java-equivalent numeric overloads, complete formatters, provider semantics, alternate unit identities, and full TCK assertion mapping.
