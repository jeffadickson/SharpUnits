# Reviewed local closure design and implementation plan — 2026-11-19

The owner requested closures after discussing locally executed Windows, Ubuntu
and pinned Java qualification while keeping GitHub CI disabled. This increment
implements that local-evidence direction. It does not redefine local execution
as GitHub attestation or Java binary compatibility.

Retaining the CI-only gate would prevent the requested closures. Dropping
platform, source or review requirements would weaken qualification. Instead,
add an explicit LOCAL_REVIEWED path alongside the unchanged CI path: a clean
committed producer captures measured host/runtime metadata, all tracked Git
blob hashes, pinned upstream revisions and raw passing TRX/Surefire reports.
The reviewer approves the exact artifact hashes and assertion mappings. Local
provenance rests on that reviewed capture, not a remote signature or independent
machine/operator. Ubuntu under WSL is labelled explicitly; it is native Linux
execution on the same physical machine, not an independent physical host.

First candidates:51-A2,552-A1,561-A4. Preserve every exact Java precondition and
assertion; explicitly map Java unit toString to the C# service formatter and
Java Class-based discovery to built-in CLR provider/type identifiers. A missing
or materially incompatible precondition blocks that assertion, not the batch.

Independent source review narrowed the first promotion to51-A2. The parser
witnesses are preparatory regression tests only:552-A1/561-A4 remain open until
the actual Java unit-toString/symbol-eligibility catalog bridge is retained.
The hashed structured review record must itself bind revision, exact artifact
digests, assertion semantics, API mapping and tests; editable ledger declarations
alone are insufficient. JVM properties are checked against raw Surefire data.

Implementation sequence:

1. Add direct xUnit method witnesses for each pinned method and catalog bridge.
2. Add synthetic positive and negative local qualification tests before the
   local validator; retain existing CI tests. Reuse the C# QualificationBundle
   producer and existing Python validation infrastructure, without dependencies.
3. Require coherent raw report counters, full method identities, unique roles,
   exact committed source/pins and review bound to artifact hashes. Add a
   StrictDoc local-evidence requirement and update the existing closure rule.
4. Commit the candidate source. Run Windows and native Ubuntu24.04/.NET11
   tests plus pinned Java execution on that exact revision, retaining coverage.
   Ordinary runs have no memory caps; mutation controls alone are monitored.
5. Obtain an independent AI-agent review of the source, raw reports, hashes and
   adaptations. This is an independent agent review, not a human/external expert.
6. Promote only approved assertions, validate the ledger and tamper controls,
   bind evidence to the tested commit, commit and push. CI remains disabled.

No new library, provider mechanism or parser rewrite is needed. Broad normative
coverage and unreviewed TCK occurrences remain open.
