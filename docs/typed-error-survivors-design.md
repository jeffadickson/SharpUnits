# Typed failures and mutation survivors

Status: approved by the user 2026-11-12. Design date: 2026-11-11.

## Outcome and scope

Give callers stable, actionable failure categories without inspecting English exception messages. Start with JavaNumber decimal parsing and numeric-object conversion, where retained survivors include malformed digits, missing digits, invalid exponents, scale/bit budgets and unsupported numeric types. Preserve existing throwing methods and Java exception mappings. This is an additive CLR usability contract, not Java API-surface conformance.

## Approaches

1. Assert exact exception messages: inexpensive but couples applications to wording and offers little recovery benefit.
2. Add typed results alongside existing APIs: recommended. Explicit codes and parameter/location metadata support field validation and recovery while preserving callers of throwing APIs.
3. Replace every public exception with results: breaking and unnecessarily broad for this iteration.

Use the existing .NET types and a small domain-specific result contract; no dependency or general-purpose railway framework is needed. Keep arithmetic and internal helpers ordinary functions. Do not add command/MCP infrastructure to this library.

## Proposed contract

- A closed success/failure result carries either a non-null value or a typed error. No default instance may masquerade as success.
- Stable error codes distinguish null input, token length, decimal syntax, exponent syntax, scale budget, numeric budget, nonfinite input and unsupported numeric type. Human-readable guidance is separate from code identity.
- Parse errors retain the input parameter and, where known, zero-based character offset. Do not retain arbitrary unsupported objects or require parsing exception text.
- Bind composes successful stages and forwards a failure without invoking downstream work. Unexpected faults and cancellation propagate; no catch-all conversion into expected failures.
- Existing throwing methods retain their public exception types and parameter names. Prefer a shared validated core; preserve original compatibility behavior with explicit translation at the throwing boundary.
- Unit/quantity result boundaries are a subsequent extension unless explicitly selected for this iteration.

## Verification and traceability

Add independent malformed/boundary witnesses before implementation, including null, whitespace, repeated dots, missing mantissa/exponent, scale +/-4096 and overflow, nonfinite primitives and unsupported objects. Check exact codes/metadata, legacy exception compatibility, successful exact values and Bind short-circuit behavior. Use seeded property tests for malformed tokens and success equivalence, and Stryker for error-code substitutions and removed validation.

Requalify changed source with fresh coverage and mutation evidence. Preserve historical 66 survivor dispositions as historical; revised statements are not automatically killed mutants. Equivalent/hash variations remain explicit; two historical timeouts remain inconclusive unless actual replay resolves them. Formal TCK closures remain unchanged and GitHub CI stays disabled.

Before long runs, configure and confirm process-tree/aggregate memory watchdog limits, inspect available memory and disk space, and route supported bulk outputs to a unique writable E:/Artifacts run directory (or CODEX_ARTIFACT_ROOT). Preserve existing evidence and unknown files.

StrictDoc: VER-RAILWAY-ERRORS and VER-SURVIVOR-REPLAY track acceptance separately from formal Java conformance. See the follow-up evidence report for actual execution and scope.

## Proportionate reuse check

The primary-source [FluentResults documentation](https://github.com/altmann/FluentResults) and [CSharpFunctionalExtensions typed-result implementation](https://github.com/vkhorikov/CSharpFunctionalExtensions/blob/master/CSharpFunctionalExtensions/Result/ResultTE.cs) were inspected on 2026-11-12. Both offer broader composition facilities. This iteration keeps the approved, domain-specific contract: one closed NumericError type, no implicit conversions or arbitrary error hierarchy, no default struct success and only synchronous Match/Bind. That is the concrete reason for a small local boundary type instead of introducing a general functional dependency into the units library. No third-party source is copied, package is installed, or candidate is claimed qualified by execution. Revisit reuse before extending this into a general result framework or adding asynchronous combinators.

A possible future Dafny candidate is decimal cursor progress and the invariant that reported invalid-character offsets are within the UTF-16 token (or its end for missing input). A proof would help prevent out-of-range diagnostics. This iteration reuses the established parser loop and tests its result boundaries; no Dafny trial, generated implementation or proof is claimed.
