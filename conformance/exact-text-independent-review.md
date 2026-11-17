# Independent static review of exact text

Review scope: ExactTextFormat reader/writer/public boundary and graph matcher, TextResult, legacy parser/formatter changes, ExactTextFormatTests, docs/exact-text-format.md and requirements/exact-text.sdoc. This is an independent agent perspective within the same assistant session, not an external expert review. No builds, tests, mutation campaigns or Java workloads were run by this reviewer.

## Findings and dispositions

All findings raised by this reviewer are addressed in the final inspected source:

- Expression aliases cannot intercept generated syntax. unit() is an alias-independent dimensionless unit and supports affine transforms.
- Graph matching memoizes reference pairs and caps comparison attempts, including repeated cached comparisons.
- Ordinary m^2/s division works; rational exponents require parentheses.
- Binding names require well-formed UTF16 and deferred JSON decoding failures become typed failures.
- Prefix radix uses the supported Int32 domain separately from bounded exponents. Rational powers format invariantly.
- Exact raw fractions are compared separately from JavaNumber projected equality.
- Emitted syntax depth: the original writer added a redundant group around powered product factors. Alternating powered/rooted products and scalar transforms could exceed the reader expression-depth budget while satisfying the writer graph-depth budget. The coordinating agent reports the new witness first failed, then passed after removing that redundant group. Static reinspection confirms every emitted factor is already a primary (registered identifier, quoted unit expression, transform expression or grouped product/transform), so appending its exponent preserves precedence without adding a group. Product and transform grouping now track their corresponding graph levels. The retained24-iteration regression exercises the reported counterexample.

No remaining blocking correctness defect was identified within this bounded static review. This is concurrence on the inspected implementation and documented scope, not an independent runtime certification.

## Qualification limits

The coordinating agent reports the final3293-test suite passed,500 seeded property trials and a65536-case Unicode escape sweep, with independent Java observations. This reviewer did not execute or independently inspect those run artifacts. Stryker5.1 qualification is still running after an earlier compiler-incompatibility attempt; final mutation and coverage results must be reported from actual completed artifacts. The equivalent HashSet constructor syntax change removes that compilation obstacle without changing ordinal comparison semantics on source inspection.

No full normative JSR/TCK conformance, CI execution, Linux runtime qualification, formal complexity proof or Dafny proof is asserted. Documentation distinguishes native grammar differences, public numeric representation from hidden promotion provenance, and floating logarithm/exponential backend operations from exact arithmetic.
