# SharpUnits compliance-led first release

Revised2026-11-16 following the owner's clarification: full useful JSR semantics are a primary release objective. The earlier beta-first reduced-scope recommendation below is superseded; it is retained as proposal history, not an approved direction.

Read conformance/release-compliance-audit.md and docs/modern-dotnet-compatibility-design.md first. The113-occurrence audit found81 API-shape,9 configuration,21 behavioral and2 mixed checks; it is not a complete normative specification coverage inventory. Its58 bounded mappings and55 API adaptations are not VERIFIED closures.

The release critical path is now:

1. Pin the target semantic contract and complete normative/API coverage beyond TCK annotation counts. Map Java mechanisms to explicit native .NET contracts without weakening value/unit/scale/converter requirements.
2. Repair the exact graph/format/parser seam and extend typed recoverable boundaries. Preserve native temperature safety, numeric magnitude, semantic kinds, backward compatibility and existing electrical/symbolic behavior.
3. Qualify catalog/type/provider coverage and the actual native public entry points, including negative/property/fuzz/mutation evidence and independent review.
4. Produce fresh Windows/Ubuntu/Java and package-consumer evidence, license/metadata/docs and source-bound artifacts. CI stays disabled; existing CI-dependent formal closure gates remain unresolved unless the owner changes policy.
5. Approve the exact release package and its compatibility statement, then publish only with explicit authorization. A preview may collect feedback if requested, but is not a replacement for this compliance-led1.0 objective.

No reliable completion date or percentage follows from current aggregate counts. Estimate after the normative coverage and graph/text assessments establish a finite implementation backlog. See the audit's prioritized work table and StrictDoc REL-COMPAT-001 through005 for proposed acceptance detail.

---

## Superseded proposal history

# SharpUnits first-release plan

Proposed 2026-11-16; scope and release decisions await owner approval. This plan is for shipping a supported subset, not declaring the entire Java compatibility program complete. It refocuses docs/package-completion-five.md on a release critical path; that broader backlog and its stable requirement IDs remain intact.

## Recommendation and alternatives

Recommend a public NuGet 0.1.0-beta.1, followed by 0.1.0-beta.2 only for observed feedback, then a deliberately scoped 1.0.0. The beta is an actual installable public deliverable. Its version makes remaining API changes explicit. Resolve package ID ownership and license before promising those exact coordinates.

Alternatives: go directly to a narrowly scoped 1.0.0 once every stable gate below passes, accepting the API-support commitment; or wait for all113 TCK occurrences to close, which has the largest and least predictable scope. Recommend the staged route because current evidence is strong locally but packaging, Linux qualification and tool qualification remain open. Do not describe a beta as production-qualified.

## Inspected baseline

- Clean branch codex/quantity-pinned-evidence at4e599bbee03fb76c968903e75526b9ee0c1535fc; migration implementation94e12b42b431e0457a3e5807e6425d3b94c692e8.
- All four projects target net11.0 with SDK11.0.100. Preserve this user-selected platform. Determine whether waiting adopters can actually deploy it before changing supported targets; additional targeting is a separate measured decision.
- Historical 2026-11-13 local Windows evidence:3240 tests,2670 Java/.NET cases,95.72% library line and87.93% branch coverage. These are not fresh release-candidate results.
- Ledger113 occurrences:14 GAP,99 PARTIAL,0 VERIFIED. No Java conformance certification or literal JVM API identity is established.
- Stryker5.1.0 cannot compile the adopted released C#15 syntax; current migration has zero qualified mutation outcomes. Historical mutation debt remains separately recorded.
- Library project has XMLDoc generation but no explicit package version/license/readme/repository metadata. No root license file was found; upstream licenses are not a SharpUnits license decision. No GitHub releases were returned by the read-only query.
- Windows/Ubuntu and Java workflows remain disabled_manually. No workflows were enabled or dispatched. Existing closure policy requires their revision-bound evidence for VERIFIED claims.

## Release contract

Proposed initial supported surface: native dimensions, units, conversion and arithmetic; native absolute/interval temperature policy; existing electrical and symbolic functionality retained with its documented dependency boundaries. The existing exact Java adapters, numeric results, factories, parsers and services remain available but their experimental boundaries must be individually named. WP1 determines the precise namespace/type/member list from actual APIs and tests; this proposal is not a blanket qualification of every public member.

Keep one primary SharpUnits package initially. Do not split assemblies, introduce a new result framework, add a JVM bridge, rewrite parsers or expand metrology/electrical features merely to ship. Do not remove existing APIs to hide known issues. A surface with an unresolved correctness defect must be repaired or explicitly excluded from supported use; calling it experimental does not excuse silent numeric corruption or crashes in the supported path.

Make no full JSR385/Indriya conformance, Native AOT, performance or unsupported-platform claims. Safe native behavior stays distinct from named pinned-Indriya quirks. Document shifted-unit arithmetic, converter order, numeric precision/promotion, parse consumption, equality and failure behavior prominently.

## Five release work packages

### 1. Freeze the promise and triage blockers

Produces: docs/release-scope.md, docs/release-blockers.md and approved Draft release criteria in requirements/package-completion.sdoc. Map each promised API and example to existing JP/EE/PLAT requirements and executable witnesses. Give every113 TCK occurrence a release disposition: required for promised behavior, experimental/documented deviation, or deferred. Preserve actual GAP/PARTIAL/VERIFIED states; release disposition is a different field.

- [ ] Inventory public APIs, XMLDoc warnings, package dependency graph and known defects against the supported/experimental boundary.
- [ ] Confirm the first adopters' framework and three representative workflows: native conversion/temperature, existing electrical/symbolic usage, exact Java-adapter usage. Do not send messages to adopters without authorization.
- [ ] Audit the known precision/narrowing, converter/transform ordering, shifted-unit, parser and provider findings for a concrete user impact. Record affected member, reproducer, severity, owner and release disposition.
- [ ] Resolve package ID/ownership and owner-selected license; inspect any redistributed third-party content and notices. Do not invent a license.
- [ ] Freeze public signatures for the candidate and define SemVer/change policy, supported platforms and maintenance channel.

Exit: an owner-reviewable supported API list and finite blocker list. Every promised workflow has an acceptance test. All113 assertions have a release disposition without changing formal closure. This is the scheduling gate: estimate the remaining work from actual blockers rather than promising a date from test counts.

### 2. Repair release-critical behavior and qualify the tests

Produces: focused source/test repairs, StrictDoc trace updates, reproducible source-bound verification evidence and independent review dispositions. Reuse existing test frameworks, pinned Java oracle and bounded runners.

- [ ] For each blocker, add a failing independent-reference or requirement witness, repair the smallest affected path and retain the RED/GREEN evidence in a coherent commit.
- [ ] Cover noncommuting converters, absolute/relative shifted temperatures, large integer/decimal boundaries, malformed/cursor parser inputs, and failed registry/provider operations preserving valid state. Use the public consumer entry points.
- [ ] Run the full .NET suite, pinned Java TCK and all differential corpora on the candidate. Record exact counts, toolversions, seeds and code hashes; rerun affected checks after changes.
- [ ] Resolve C#15 mutation-tool compatibility with a maintained compatible tool/compiler configuration. If unavailable, use explicitly bounded source-fault tests for the release-critical algorithms and retain the broader Stryker gap; require owner acceptance of the limitation rather than treating an uncompiled run as a pass.
- [ ] Execute relevant property, malformed-input/fuzz and mutation checks with preserved replay data and watchdog limits. Measure library line/branch coverage; explain uncovered release-critical branches and changes in denominators.
- [ ] Obtain independent review of numerical semantics, observer completeness, negative witnesses and residual risk. Agent review must be labeled as agent review.

Exit: zero unresolved known data-corruption, wrong-result, crash or state-loss blockers in promised workflows; all relevant regressions pass. No need to finish unrelated TCK gaps to release a scoped package. No unsupported mutant, timeout or ignored branch counts as a demonstrated kill.

### 3. Build a package people can install

Produces: release metadata in src/SharpUnits/SharpUnits.csproj; owner-approved LICENSE and notices; package README, changelog, symbols/source metadata where supported; reusable tools/Pack-Release.ps1 and package-consumer smoke fixtures.

- [ ] Use SDK dotnet pack and NuGet conventions, not a custom package format. Set explicit package version, description, authors, repository/commit, README and license metadata. Include XML documentation; verify source/symbol links and avoid leaking local paths.
- [ ] Mark non-library projects non-packable as appropriate. Inspect the actual nupkg contents and dependency graph; exclude Java runtimes, upstream source trees, test tools and bulk artifacts.
- [ ] Restore a separate clean consumer using only the local candidate nupkg, never ProjectReference. Build/run the three WP1 workflows including documented typed-error recovery.
- [ ] Verify reproducible package content from the same source/tool inputs; retain package hash and any explained nondeterministic metadata. Promote exactly the reviewed package bytes rather than rebuilding after approval.
- [ ] Validate NuGet identity/ownership and prepare publishing authentication without exposing credentials. No publication is authorized by this planning request.

Exit: installable hashed beta candidate, clean consumer passes, complete license/metadata, correct XMLDoc and no unintended dependencies/content.

### 4. Qualify Windows/Linux and document adoption

Produces: candidate-specific platform evidence, quickstart, migration/compatibility matrix, generated API docs, limitations and troubleshooting. Preserve the existing DocFX/API workflow described by repository requirements; inspect actual configuration before adding tooling. Do not migrate documentation systems on the release critical path.

- [ ] Execute clean .NET11 build/test/consumer checks on Windows and Ubuntu plus pinned Java qualification, retaining exact source/package hashes and runtime versions. Use actual Linux execution, not a Windows substitute. Keep macOS unsupported/unqualified.
- [ ] Keep GitHub CI disabled. Use a suitable local Linux/WSL environment if available, or ask for an explicit policy change when a reviewable candidate needs remote qualification. Local evidence can qualify stated platform behavior but cannot satisfy the current CI-specific formal closure policy.
- [ ] Rebuild examples against the nupkg, generate API docs, validate real StrictDoc, and check requirement-to-implementation-to-test/evidence links.
- [ ] Put installation, first successful conversion, temperature/precision rules, safe versus pinned compatibility behavior and recoverable errors before internal engineering detail. State .NET11 runtime requirements clearly.
- [ ] Audit package/release wording: no claims beyond actual evidence, and no automatic formal TCK promotion. Any proposal to replace a CI-specific gate requires a separate explicit policy decision.

Exit: independently reviewable Windows/Ubuntu/Java results and working adoption documentation. If one platform is unavailable, retain it as an explicit release blocker for a cross-platform claim rather than inventing results.

### 5. Release the beta, learn, then decide 1.0

Produces: release checklist, release notes, approved publication record, changelog and support/patch procedure.

- [ ] Prepare a concrete approval bundle: exact commit/tag proposal, nupkg hash, supported API list, test/platform/coverage/mutation summary, known limitations and draft announcement. Tag/publish/send only with explicit authorization.
- [ ] Publish approved0.1.0-beta.1 and smoke-test installation from the public feed. Keep the reviewed artifact/commit association immutable. Verify source links and package page content after publication.
- [ ] Track feedback from representative adopters as reproducible issues. Fix blockers in patch/beta increments; do not require an arbitrary number of iterations or users as a substitute for evidence.
- [ ] Promote to1.0 only after the stable gates below pass. Agree a bounded feedback window and minimum representative workflows with the owner; do not infer absent feedback means success.
- [ ] For a defective package, document the issue, unlist/deprecate if appropriate and publish a new version; do not assume overwriting/retraction is a rollback. Maintain a tested patch path.

Exit: a usable public beta, followed by an explicit evidence-based1.0 decision. Publication itself is not proof of package completion.

## Decision gates and sequence

| Gate | Required result |
| --- | --- |
| Scope approval | Supported/experimental API list, license/package ownership, framework/platform contract and release-critical defect list accepted |
| Beta go/no-go | WP1–4 complete for the beta contract; no known critical correctness/state-loss defects in that contract; clean package consumers; retained review and candid mutation/platform limitations |
| Stable go/no-go | Stable API contract and maintenance policy; fresh candidate-specific Windows/Ubuntu/Java and package-consumer evidence; release-critical mutation/fuzz/property checks or explicitly accepted bounded alternatives; representative adoption feedback resolved; docs/license/traceability complete |
| Formal Java closure | Separate exact assertion gates, including current CI policy; not implied by beta/1.0 publication |

Critical path: WP1 -> release-blocking repairs inWP2 -> candidateWP3 -> platform qualificationWP4 -> betaWP5. Packaging/docs can proceed alongside repairs after scope freezes, but final evidence and consumer tests must bind the final candidate. Give the owner a calendar estimate afterWP1; do not schedule all broad completion packages before the beta.

## Immediate next engineering slice

Inventory the public release contract, license/metadata gaps and known defect reproducers, then produce the finite blocker list and a dry-run local nupkg/consumer result. Make no new features or publication part of that slice. Ask for publication approval only when the package and evidence are concrete and reviewable. Leave CI off throughout unless explicitly instructed otherwise.

## Primary references

- NuGet package authoring metadata: https://learn.microsoft.com/nuget/create-packages/package-authoring-best-practices
- Prerelease version behavior: https://learn.microsoft.com/en-us/nuget/create-packages/prerelease-packages
- Package version semantics: https://learn.microsoft.com/en-us/nuget/concepts/package-versioning
- Deterministic package support: https://learn.microsoft.com/en-us/nuget/create-packages/deterministic-packages

No implementation, test rerun, release publication, new requirement closure or CI dispatch is claimed by this plan.
