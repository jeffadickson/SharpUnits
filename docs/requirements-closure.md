# Requirements closure policy

Canonical requirements use [StrictDoc capability records](../requirements/capabilities.sdoc),
[parsing requirements](../requirements/quantity-parsing.sdoc), and
[TCK assertion records](../requirements/tck-assertions.sdoc). Stable JP/EE IDs are
preserved from the [Java parity gap analysis](java-parity-gap-analysis.md).
The Markdown matrix summarizes dispositions; the ledger retains assertion evidence.
StrictDoc `Active` denotes requirement lifecycle, not verified implementation.
Export using StrictDoc 0.30.4 and `tools/Export-Requirements.ps1`.
Requirements remain independent of GitHub issue closure.

The [remaining grammar requirements](../requirements/parser-grammar.sdoc) add stable
IDs for textual powers, roots, integer factors and prefix inference. Exact linear
factor metadata is not a claim of arbitrary Java Number storage or precision.

## Evidence required for each requirement
- Stable requirement ID and acceptance criteria
- Implementation path and commit SHA
- xUnit test identifiers, including positive, negative, and boundary/round-trip tests where applicable
- CI workflow URL showing tests passing on Ubuntu and Windows
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
