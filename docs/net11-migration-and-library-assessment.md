# .NET11 migration and reuse decisions — 2026-11-13

The user requested migration to .NET11 and appropriate language/library reuse, followed by five completion work packages. This is an SDK/target migration plus bounded immutable-lookup modernization, not a rewrite of units arithmetic.

## Selection and acceptance

Use installed stable SDK11.0.100 and runtime11.0.0, not the user's separate prerelease SDK. All four projects target net11.0. C#15 is the target's default language; no preview switches are enabled. Runtime consumers now require .NET11; this increment does not multi-target .NET10. Public quantity and typed-result signatures remain intact. Historical .NET10 evidence stays historical. Manual-only Windows/Ubuntu workflows are updated but remain disabled; macOS is not introduced.

Acceptance: build/test library, runner and demo; execute all existing paired Java corpora; retain line/branch coverage, seeded/negative tests and a source-bound mutation attempt; validate StrictDoc; map every inventoried TCK assertion exactly once to a delivery owner. Formal assertion closure remains gated by the existing qualification policy.

## Reuse choices

| Candidate | Decision and reason |
| --- | --- |
| .NET11.0.100/C#15 | Installed stable SDK is reused. [Release/download](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) and [language documentation](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-15) support the selection; actual compilation/tests are the qualification evidence. |
| BCL FrozenDictionary/FrozenSet, OrderedDictionary and ImmutableArray | Adopt frozen lookups/membership plus an ordered immutable prefix snapshot. [Frozen collections](https://learn.microsoft.com/en-us/dotnet/api/system.collections.frozen?view=net-11.0) provide immutable read-heavy storage without a package dependency; ordered construction preserves the public prefix sequence. These collections predate .NET11; no speedup is claimed without measurements. |
| C#15 collection-expression arguments | Use with(StringComparer.Ordinal) when constructing metric membership, then freeze it. Explicit comparer identity is preserved. Source compiles under the actual installed SDK and formatting differential cases challenge behavior. |
| Native unions/closed hierarchies | Candidate for future result-state and parser models. Do not replace the public NumericResult class merely for syntax: default union state, allocation cost, serialization and mutation-tool support require a bounded comparison. The current closed/non-null API remains intact. |
| System.Text.Json11 JSONL and Process11 helpers | Evaluate in WP2/WP5 against current observer schema, streaming memory, cancellation and owned-process limits before replacing integration code. Documented availability is not execution qualification. [BCL additions](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/libraries). |
| Existing xUnit2.9.2/FsCheck3.2.0/TestSdk17.11.1/Coverlet6.0.4 | Retain because the migrated full suite and coverage execute. Evaluated current packages: [TestSdk18.12.0](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.12.0), [Coverlet10.1.0](https://www.nuget.org/packages/coverlet.collector/10.1.0), [FsCheck3.6.0](https://www.nuget.org/packages/FsCheck.Xunit/3.6.0), [runner4.0.1](https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.1). These are development dependencies with potential adapter/seed changes, not automatic upgrades justified by metadata alone. WP5 owns qualification and relevant license/transitive checks before adoption. |
| Stryker5.1.0 | Pin as a local development tool for a .NET11/C#15 compatibility attempt. [Package](https://www.nuget.org/packages/dotnet-stryker/5.1.0). Keep older external5.0 tools/evidence intact. A completed source-bound run, not installation, establishes supported scope. |

No third-party result framework or source copy is added. Built-in facilities meet the bounded need. Revisit reuse before expanding custom parsing or creating a general result/command framework. A future Dafny candidate is bounded parser cursor progress and valid error offsets; no verifier/generated-code trial is claimed for this migration.

## Sequence

1. Pin SDK/TFMs and update active commands/workflows; obtain .NET11 baseline.
2. Observe immutable-catalog negative witness, adopt BCL frozen collections/C#15 comparer arguments and run full coverage.
3. Execute differential, demo/example, requirements and focused mutation qualification with memory watchdogs and fresh E: outputs.
4. Retain exact evidence and the five-package assertion ownership map. Commit verified migration; keep broader completion work planned rather than inventing completed iterations.
