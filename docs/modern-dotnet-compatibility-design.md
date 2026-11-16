# Modern .NET compatibility direction

Audit recommendation,2026-11-16. The user requests full useful JSR semantics without slavish Java interface imitation. This document proposes acceptance and implementation choices; it does not claim they are already implemented or establish Java certification. Existing stable API compatibility and electrical/symbolic functionality remain constraints.

## Recommended boundary

Use an idiomatic .NET domain as the normal entry point, with existing Java-named APIs retained as explicit compatibility adapters. Share well-specified arithmetic, dimensions and converter primitives where policies agree; keep representation/promotion, point-versus-interval and pinned-implementation policies explicit. Do not force every native operation through a double adapter, and do not merge two behaviors just because both are called Quantity.

Current affected chain: exact JavaNumber/JavaUnit -> JavaNumberQuantity/exact factories -> compatibility observations; legacy UnitDefinition/JavaQuantityAdapter -> QuantityTextFormat/JavaUnitTextFormat -> string/finite-double parser -> registry/services. The exact core is richer than the formatting/parser boundary. Fixing the graph-to-text-to-graph seam requires producers, formatter/parser, factory entry points and consumers to agree on identity, converter ordering, numeric precision, display unit and scale. Replacing a method name cannot repair that seam.

## Preserve, adapt and decline

| Concern | Recommended native behavior | Compatibility treatment |
| --- | --- | --- |
| Dimensions, products, roots, compatibility | Preserve rational dimensions, structural graphs and independent numerical witnesses | Preserve semantics; retain explicit resource limits |
| Value precision | Explicit exact-rational/decimal/binary policies; retain input precision until a requested projection | Java numeric-family/promotion fidelity belongs to the adapter policy; never route exact input through double |
| Shifted units | Point/interval types and scale-aware conversion; absolute minus absolute yields an interval in the native temperature domain | Retain permissive pinned arithmetic only under its documented compatibility boundary |
| Converter composition | Specify application order in native terms and test noncommuting operations | Java concatenate ordering remains tested inside its adapter; do not export its ambiguity as the native design |
| Equality | Distinguish structural/representation equality from physical equivalence and approximate comparison; hash consistent with equality | Do not copy an upstream inconsistent equality/hash implementation |
| Quantity kinds | Reuse existing QuantityKind/SemanticQuantity rather than equating every identical dimension | CLR generic/type mapping need not reproduce Java Class reflection |
| Format and parse | Explicit formatter/culture contract; complete consumption or cursor result; typed diagnostic offsets; structural/value/scale roundtrip | Missing affine/decimal graph parsing is real work; Java method reflection is a separate adaptation |
| Providers | Explicit instance-scoped registration/selection and deterministic defaults | Java ServiceLoader and its TCK configuration are not native domain requirements |
| Failures | Extend the existing typed-error approach to recoverable parser/converter/factory boundaries without swallowing faults or cancellation | Keep existing throwing APIs as compatible wrappers |
| Very large values | Exact results within declared limits, otherwise typed budget failure | The observed large-factor negative Java text is not a safe default to emulate |

## Reuse assessment

No new dependency is selected or installed for this audit. Reuse the existing .NET11 SDK, immutable/frozen BCL collections, Rational, NumericResult, xUnit/FsCheck and differential harness. Public interface changes need a bounded consumer prototype and review before adoption.

- **Generic math:** BCL operator interfaces can expose reusable operations. Choose narrow interfaces where dimensionally valid. Implementing INumber<Quantity> wholesale would impose scalar-like arithmetic contracts that do not naturally express changing dimensions or point/interval rules. A numeric backend constrained to appropriate interfaces is a candidate, not a reason to rewrite every quantity. Reference: https://learn.microsoft.com/en-us/dotnet/standard/generics/math
- **Parsing:** evaluate ISpanParsable/ISpanFormattable for suitable culture-aware whole-value convenience APIs, alongside a richer typed ParseResult carrying cursor/consumption/error data. Standard TryParse's Boolean/out contract alone does not provide the railway diagnostics already requested. Preserve exact lexical values; spans do not solve precision or grammar. Reference: https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1.tryparse
- **Registration:** constructor-injected registry/factory dependencies can remain dependency-free in the core. Evaluate an optional Microsoft.Extensions.DependencyInjection integration after defining registration/lifetime requirements. It is not an automatic replacement for missing extension semantics, and a process-global locator is not the native target. Reference: https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines
- **Results/language:** existing NumericResult prevents invalid default-success state and preserves cancellation. Do not replace it with a package or C# union for novelty; evaluate default state, allocation, serialization, diagnostics and tool support first.
- **Parsing proof candidate:** a small bounded token/cursor routine could prove monotonic progress and offsets within0..inputLength in Dafny. No routine is selected, verified or generated here; proof would not establish grammar correctness or interoperability. Retain malformed-input/fuzz tests regardless.

## What to implement next, in dependency order

1. **Normative contract inventory.** Extend beyond the113 TCK occurrences to the targeted unit-api2.2 public contracts and their specification semantics. Turn each adaptation into an explicit native acceptance test, and separate JSR requirements from Indriya-specific feature requests. Pin the target before claiming completeness.
2. **Exact graph/text vertical slice.** Support a retained exact graph through formatting and parsing, first closing the observed Fahrenheit/affine expression and decimal-factor gaps. Test value/unit/scale/converter jointly. Reuse an existing parser or span facilities only after a representative comparison; do not begin a general parser rewrite without that decision.
3. **Typed public recovery.** Extend existing error composition to conversion/parsing/factories, preserving state and legacy exceptions. Do not add general command/MCP infrastructure to this library merely because application defaults mention commands.
4. **Registry and supported-type coverage.** Verify all seven bases, every supported quantity-kind binding, whole-catalog roundtrips and configurable providers through native interfaces. Keep Java test registration in the harness. Avoid unsupported format-name advertising.
5. **Qualification and release.** Resolve C#15 mutation tooling, test core/harness blind spots, run actual Windows/Ubuntu qualification and independent review, then pack/install-test the package. CI stays off; formal CI-dependent closures remain blocked until policy changes. No new feature unrelated to this path is a release dependency.

The113 audit classifications are recommendations about exact pinned test occurrences, not a completeness score for the entire specification. An intentional API adaptation is not a waived semantic defect. Full semantic coverage remains the release objective; literal JVM interoperability/certification is not silently claimed.
