# Quantity parsing review

Starting commit: `098acf2`. Review date: 2026-10-24. GitHub CI stays disabled.

Scope: add an adapter-returning parser preserving the display unit and absolute
scale, while retaining the existing SI-only parser contract. Compare decimal,
whitespace, rational and malformed inputs with the pinned Indriya
`SimpleQuantityFormat.parse` and `RationalNumberScanner`. Full Java unit grammar,
parse cursors, localization and arbitrary Number retention remain outside this increment.

There will be 50 numbered executable scenario reviews, with compilation and full
regression testing after every ten. These are not 50 commits or formal closures.
Coverage, deliberate mutations, negative cases and seeded randomized tests will
supplement the Java differential evidence. Requirements are maintained in StrictDoc
with stable IDs and implementation/test/evidence links. No verification result is
claimed before execution.
