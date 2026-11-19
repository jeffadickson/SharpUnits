# Mutation debt reduction

Scope: reduce the recorded core mutation debt without changing compatibility expectations to fit the implementation. Keep all historical reports immutable and CI disabled.

1. Reproduce public-contract blind spots with null/negative inputs, exact boundary literals, native import/export and representation-sensitive observations. Existing behavior tests should pass normally and fail under the corresponding Stryker mutation.
2. Challenge numeric comparison against pinned Java for any semantics that cannot be established independently. Apply production fixes only after a failing independent witness.
3. Freeze source/test hashes, replay the full eight-file Stryker scope, and classify every surviving/uncovered/timeout entry individually. Equivalent/unreachable decisions need explicit invariants; diagnostic or resource/time-limit changes are never silently counted as kills.
4. Retain coverage, seeded/property campaigns, Java differentials, StrictDoc traceability and independent agent review. Commit tested changes and push; do not promote formal TCK assertions or enable CI.
