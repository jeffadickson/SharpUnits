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
