# TCK verification protocol

## Evidence baseline

The cross-platform .NET 10 run https://github.com/jeffadickson/SharpUnits/actions/runs/38832777901 succeeded. Fourteen dimension/converter assertion mappings have been advanced to `CI_PASS_BEHAVIORAL_REVIEW_PENDING` in the closure ledger.

This status is intentionally **not** `VERIFIED`. For example, Java's `UnitDimensionTest.testRoot` uses reflection to assert method availability; our xUnit test exercises root semantics but does not establish exact Java API signature parity.

## Required for VERIFIED

1. Read the exact pinned Java TCK method and setup fixture.
2. Record the Java specification assertion ID, preconditions and complete expected behavior.
3. Show the corresponding C# API and xUnit test satisfy the complete assertion or document intentional .NET semantic differences.
4. Execute the Java reference case on the pinned upstream revision, or provide independently reviewable upstream TCK evidence.
5. Attach .NET 10 CI run with passing Linux, Windows and macOS jobs.
6. Record reviewer and verification date.

Do not treat partial behavioral adaptations as full JSR 385 conformance certification.
