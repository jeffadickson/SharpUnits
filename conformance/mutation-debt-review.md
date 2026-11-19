# Independent agent review — mutation debt reduction

Reviewer: separate Codex agent `/root/verification_reviewer`, read-only. This is independent agent review, not external human acceptance or certification. The reviewer did not execute live platform CI or independently rerun every campaign.

The reviewer identified and challenged whitespace parsing, same-base nonlinear composition, identity special-value conversion, mixed numeric comparison and decimal128 boundary gaps. It verified the signed-bit-length asymmetry in pinned DefaultNumberSystem, requested normalized chains, and confirmed the chosen negation rule including Long.MIN_VALUE and asymmetric double negation. It distinguished Calculator accumulator state from normalized public-operation results.

Further reviewer witnesses caught exact fractional-decade scale preservation and typed unsupported-root self conversion. Actual Java capture also confirmed that getConverterToAny differs from typed getConverterTo; both paths are explicitly retained and tested. No expectation was silently rewritten from CLR output.

The reviewer inspected every outstanding disposition and checked contributing report hashes. It concurred with source-bounded positive-decade normalization, private scale-budget profile rejection, closed prefix/catalog invariants, delegated validation and the conditional round-trip invariant. Diagnostics and hash values remain observable variations, not universal equivalence. The hash-quality threshold is a separate, explicitly Draft engineering choice; distinct converters, permitted collisions, generated inputs/buckets and dictionary comparison counts are retained.

The reviewer initially retained one extreme checked-scale guard gap. It subsequently concurred with the production-used internal CombineDecimalScales extraction and direct9boundary/overflow cases plus the seeded500-trial BigInteger oracle. It inspected raw mutants1196(remove checked) and1197(addition-to-subtraction), both killed by the complete final JavaNumber replay. No fabricated private object state or billions-operation public chain was used.

**Final decision: bounded acceptance of the scale-guard repair and mutation-debt dispositions.** The measured954 killed/66 survived/6uncovered/2timeouts is combined evidence, not a single-run score. No unresolved OPEN classification is hidden. Conditional/source-bound reasoning,26 diagnostic variations, one hash-algorithm variation and two inconclusive timeouts remain explicit. No formal proof, exhaustive mutation qualification, JSR conformance, live Windows/Ubuntu CI or external human acceptance is endorsed. All113 TCK occurrences retain14GAP/99PARTIAL/0VERIFIED; CI remains disabled.

## Final artifact verification

The independent reviewer matched all218 canonical source hashes,31 raw-evidence hashes and eight merged mutation-file hashes against the checkout. It confirmed that raw CRLF source bytes and LF-canonical source hashes are intentionally separate, and verified the final3206-test count, coverage,2670 shared cases plus16chains,387 StrictDoc IDs and mutation totals. No stale count or substantive evidence mismatch was found; bounded concurrence stands with zero formal VERIFIED assertions.
