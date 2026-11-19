# Exact graph text implementation plan

Goal: close the observed affine/decimal text gaps with a lossless, bounded native exact codec and typed errors, preserving existing APIs and Java/native policy separation.
Architecture: operate on existing JavaUnit/JavaNumber/JavaNumberQuantity immutable domain objects. Add ExactTextFormat, TextResult/TextError and a bounded reader; retain the legacy parser association/exception contract and use additive fallback only for previously unsupported expressions. Work in the clean existing task branch; no new worktree is necessary for this focused continuation.

Reuse: retain BCL BigInteger, spans, immutable/frozen collections and existing numeric/graph constructors. Existing JavaUnitTextParser flattens to dimensions/double and cannot represent graph identity. Pidgin (https://github.com/benjamin-hodgson/Pidgin) was inspected as a general parser alternative; no version is selected or dependency qualified/added. A small deterministic reader is justified by the fixed grammar, explicit offsets/budgets and integration with existing exact domain constructors; no general parser framework is introduced. Reevaluate before grammar expansion. BCL string escaping uses supported facilities rather than a new quoting format.

Native grammar: ordinary integer/decimal/scientific numbers; exact n÷d rationals; binary(number) for binary64 family. Parentheses, */ and +scalar/-scalar affine shifts, conventional left association, integral/rational powers. Explicit transform(parent;scale(number);offset(number);prefix(radix,exponent);log(number);exp(number)) retains atomic converter order/metadata where ordinary syntax would lose it. Registered unit atoms retain identity; unknown custom atoms need explicit bindings. Quantity text retains numeric family/value, display graph and an @absolute/@relative suffix. This is a documented native exact format, not a claim of complete Java EBNF/locale fidelity. Alternate/unknown graphs unsupported by the chosen text scope return typed errors rather than a fake symbol/projection.

- [ ] Red: decimal/affine legacy gap witnesses; exact graph/value/scale and typed malformed-input tests.
- [ ] Implement TextResult<T> with typed codes/positions/parameter and Match/Bind. No default-success structs or exception swallowing.
- [ ] Implement codec/reader using exact domain constructors and strict full consumption, finite/bounded numbers, graph depth/atom/exponent limits, and bounded output. Reject lossy projections in the exact path.
- [ ] Integrate previously failing compatibility parsing paths, preserving registered aliases and old right-associated successful parses; retain native point/interval policy.
- [ ] Java differential fixtures and analytical exact converter/graph tests; property replay/fuzz, mutation controls and full coverage/regressions under active budgets. Do not count a compiler-failed Stryker run as qualification.
- [ ] StrictDoc/XMLDoc/examples/handoff/evidence, independent review and coherent commits. CI stays off; no formal TCK promotion or publication.

Review focus: noncommuting converter order; numeric representation versus equality; reserved/custom symbols and alias precedence; malformed UTF16/complete consumption; resource failures preserving usable state. A Dafny cursor-progress proof remains a possible later trial, not a dependency of this fix or a proof claimed here.

Ruling2026-11-16: replace only the metric membership HashSet collection-constructor spelling with an equivalent explicit ordinal constructor to enable a current-tool mutation trial. Preserve SDK11/C#15 and frozen/comparer semantics; update PLAT-FROZEN accordingly. A syntax preference is not a domain requirement. Historical compiler-block evidence is retained.
