# TCK verification protocol

## Reviewed local qualification (2026-11-19)

The owner requested local closures with GitHub CI off. VER-LOCAL-001 adds an
explicit LOCAL_REVIEWED provenance path alongside the existing CI-attested path.
Both require a clean tested commit, complete tracked Git-blob inventory, exact
pinned methods, raw passing Windows/Ubuntu TRX and Java Surefire reports, exact
assertion/API adaptation review, and an independent reviewer bound to artifact
hashes. The C# producer measures its runtime/platform and records Ubuntu/WSL
context. Local capture plus independent review establishes reproducible local
evidence; it does not authenticate an independent operator or physical host.
The validator rejects stale hashes, wrong roles/platforms, missing/skipped or
conflicting method records and inconsistent counters. Java binary compatibility
and full JSR certification are separate claims. Historical CI-only rules and
results below remain historical. Current target is .NET11, not .NET10.

## Current qualification (2026-10-29)

The [verification audit](verification-audit.md) finds the current evidence unqualified for closure. Ledger accounting is 14 GAP, 99 PARTIAL and zero VERIFIED. Its validator checks accounting, not the evidence gate below. GitHub CI is disabled; the historical CI results below are not current platform evidence.

## Historical evidence baseline

The cross-platform .NET 10 run https://github.com/jeffadickson/SharpUnits/actions/runs/38832777901 succeeded. Fourteen dimension/converter assertion mappings have been advanced to `CI_PASS_BEHAVIORAL_REVIEW_PENDING` in the closure ledger.

This status is intentionally **not** `VERIFIED`. For example, Java's `UnitDimensionTest.testRoot` uses reflection to assert method availability; our xUnit test exercises root semantics but does not establish exact Java API signature parity.

## Required for VERIFIED

1. Read the exact pinned Java TCK method and setup fixture.
2. Record the Java specification assertion ID, preconditions and complete expected behavior.
3. Show the corresponding C# API and xUnit test satisfy the complete assertion or document intentional .NET semantic differences.
4. Execute the Java reference case on the pinned upstream revision, or provide independently reviewable upstream TCK evidence.
5. Attach .NET 10 CI run with passing Ubuntu and Windows jobs.
6. Record reviewer and verification date.

Do not treat partial behavioral adaptations as full JSR 385 conformance certification.
