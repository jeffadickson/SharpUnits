# Requirements closure policy

Canonical inventory: [Java parity gap analysis](java-parity-gap-analysis.md). JP-001 through JP-030 and EE-001 through EE-007 remain tracked independently of GitHub issue closure.

## Evidence required for each requirement
- Stable requirement ID and acceptance criteria
- Implementation path and commit SHA
- xUnit test identifiers, including positive, negative, and boundary/round-trip tests where applicable
- CI workflow URL showing tests passing on Linux, Windows and macOS
- XML API documentation and DocFX documentation link
- Explicit reviewer approval and status CLOSED

## Status vocabulary
NOT_STARTED, IN_PROGRESS, IMPLEMENTED_UNVERIFIED, VERIFIED, BLOCKED. Never mark VERIFIED from source inspection or tests merely authored.

## Ownership
- #4: JP-003–JP-015, JP-023–JP-025
- #5: JP-016–JP-022, JP-026–JP-028
- #6: EE-002–EE-007, JP-029
- #1: full source import
- #2: independent Java conformance and parity
- #3: electrical tests and AngouriMath
- JP-001, JP-002, JP-030: core CI evidence; JP-017/018 and EE-001 cross-cutting

Issue completion does not automatically close any requirement. Update the inventory after every feature change, and verify test results before closure.
