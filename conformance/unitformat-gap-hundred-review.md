# Mutable unit formatting and ASCII implementation

Baseline `8cbdf09`: 1,140 passing .NET tests; both GitHub workflows disabled.

One hundred scoped missing obligations are inventoried before implementation:
32 mutable-label/alias/appendable/integration contracts, 44 ASCII catalog bindings,
and 24 ASCII prefixed-ohm bindings. These are finer-grained requirements, not
100 TCK assertions or formal closures. Each has a stable JP-UF requirement and
an independently executed Java observation. Compile/full tests run every ten.

Pinned source review targets UnitFormatTest 45-A3 and 45-A5. Java's collision,
removal, first-character validation, format-family fallback and shared quantity
formatter behavior must be measured rather than replaced with preferred semantics.
General unit identity, arbitrary Number storage, EBNF/LOCAL formats and external
provider discovery remain outside this batch. GitHub CI stays disabled.
