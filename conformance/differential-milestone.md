# Java TCK differential execution milestone

The pinned upstream TCK has two setup tests that both use `@SpecAssertion(id="Setup")`. Our internal IDs `Setup-1` and `Setup-2` distinguish the source methods; they are **not official upstream assertion IDs**.

## Work completed
- Full 16-suite inventory and 113 source annotation occurrences
- Auditable closure ledger and method-link audit
- Strict normalized JSONL Java/.NET comparison utility and its Python unit tests
- .NET 10 cross-platform xUnit baseline
- Pinned Java submodules in the repository

## Work not yet complete
- Executing the pinned Java TCK against Indriya in GitHub Actions
- Generating real normalized Java reference results
- Generating corresponding normalized .NET results
- Running the comparator against actual cross-language results
- Certifying any assertion as VERIFIED

Do not confuse passing comparator self-tests with actual Java/.NET differential execution.

## Java workflow implementation

GitHub Actions workflow `.github/workflows/java-reference.yml` now attempts a pinned Java 17 Indriya build followed by a TCK Maven test run, uploading reports. This is **reference execution infrastructure**, not yet a verified successful TCK run or normalized cross-language differential comparison. Inspect Maven results and adjust the TCK's runner/profile/configuration as needed; a plain Maven test invocation may not exercise the full TCK FULL profile.

## Verified Java baseline (2026-10-15)

Java workflow https://github.com/jeffadickson/SharpUnits/actions/runs/38833971549 completed successfully. Maven Surefire reported **112 tests, 0 failures, 0 errors, 0 skipped**; TestNG reported **111 passes**. This is the pinned Java reference suite, not a SharpUnits conformance result. .NET CI for the same commit also completed successfully, but cross-language normalized fixtures have not yet been generated or compared. Full TCK profile/configuration coverage should still be audited against the suite inventory.
