# Remaining Java grammar implementation review

Baseline: `e982406`. GitHub CI remains disabled by user request.

This batch targets four concrete parser gaps: superscript/double-star powers,
rational powers/roots, integer numeric unit factors, and automatic decimal prefixes.
It comprises 100 numbered executable scenario reviews, with compilation and full
regression tests every ten, coherent commits, coverage, seeded generated/fuzz tests
and targeted mutations. Review counts are not commit counts or formal TCK closures.

Pinned SimpleUnitFormat source must guide implementation. It treats zero exponent
and root literals as default-one sentinels, and handles immediate integer factors
differently from recursively parsed unit operands. Those conventions must be
tested, rather than inferred from conventional algebra. Native unit APIs and the
legacy CLR parser retain their existing contracts.

Arbitrary Java Number storage, full locale/cursor/appendable formatting and service
discovery remain separate outstanding requirements. No new behavior is claimed
before actual Java and .NET execution.
