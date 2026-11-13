# Typed numeric errors implementation plan

Approved design: typed-error-survivors-design.md (user approval 2026-11-12).
Implementation is inline in the existing checkout; preserve the pending design/StrictDoc changes.

1. Add tests in TypedNumericResultTests for stable error codes, positions, legacy exceptions, exact successes, composition, malformed inputs and property replays. Observe missing API failures first.
2. Add NumericResult.cs: a sealed reference result, typed NumericError, explicit stable NumericErrorCode values, Match and Bind. No broad exception capture, default success, or third-party result framework.
3. Share JavaNumber parsing/conversion validation with ParseDecimal and ConvertNumeric result entry points. Preserve Decimal/FromNumeric exception types, parameter names, numeric representation and promotion provenance. Check default Rational explicitly.
4. Run the full xUnit suite with Coverlet; run focused Stryker on changed source under an enforced serial Windows process-tree memory budget. Retain bulk output on E:, exact source hashes, limits and outcomes. Re-run the pinned differential corpora to challenge success compatibility.
5. Update StrictDoc traceability, documentation/example and survivor follow-up evidence. Keep historical reports immutable, classify newly revised statements separately from kills, keep CI disabled, review and commit coherent verified work.

Review focus: malformed exponent position, +/-4096 boundary, huge coefficients after scale normalization, null result/delegate rejection, cancellation and unexpected delegate failures. Error objects must not retain caller objects or use their ToString methods. Stable error codes are the recovery contract; display text remains localizable/non-normative.
