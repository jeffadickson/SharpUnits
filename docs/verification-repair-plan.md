# Verification repairs

Scope approved by the request to complete the audit repairs: implement VER-001 through VER-006 from the independently reviewed audit. Keep CI disabled and all formal TCK closures gated. Existing dedicated development branch is clean at 9f692ee.

The existing Python verification CLIs will share one strict standard-library contract rather than introduce another validator/dependency. C# remains the library/helper implementation language. Java and CLR independently produce complete observations; Java expected outputs are retained only after execution. Raw Java runtime representation is recorded separately from its declared CLR normalization mapping. Known Indriya quirks have explicit provenance and are never specification evidence.

- [x] Strict comparator/fixture schema: first reproduce large integer, malformed outcome, inventory, tolerance, duplicate-key and Unicode failures; share exact typed comparison and require an explicit expected inventory on qualification CLI calls. Update every existing caller and make historical omitted tolerances explicit.
- [x] Complete quantity observations: jointly observe value, scale, structural unit, dimension and converter availability; capture raw Java representation and failure stage/category separately. Test independent corruption of each field, retain actual executed Java expectations and challenge mismatches.
- [x] Provenance and traceability: classify each oracle, require pinned/source/artifact hashes, add all16 exact quantity surface mappings and behavioral case IDs, and reject fabricated closure evidence with isolated negative tests. Do not manufacture CI/reviewer evidence.
- [x] Mutation qualification: replay historical ordering faults, execute final-source bounded Stryker scopes, classify survivors/uncovered code and record source AND test hashes. Use isolated outputs and one worker to avoid the prior VSTest infrastructure problem.
- [x] Requalification: rerun .NET/Java corpora, strict validator tests, negative/property campaigns, coverage, StrictDoc and pinned TCK; update the audit diagnostics/dispositions and request independent agent review of repairs. Commit coherent increments and preserve the open cross-platform/human/formal gates.

Review focus: two quantities with identical magnitudes in different units; raw Java versus normalized number families; exact integers above 2^53; malformed or contradictory errors and nonfinite tolerances; fabricated evidence and stale/missing test/case references.

Ruling: the audited repair criteria and user's explicit completion instruction define this bugfix scope; no new feature/dependency or unrelated electrical/temperature model is introduced. Existing compatibility APIs retain their profile while default data-loss arithmetic is addressed explicitly rather than silently blessing it as specification behavior.

Completion is bounded by the final repair report: mutation debt is retained rather than equated with kills, and Windows/Ubuntu/live Java CI plus external human/formal acceptance remain open. No VERIFIED assertion promotion is made.
