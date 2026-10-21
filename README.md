# SharpUnits

C# dimensional analysis, electrical quantities, and symbolic engineering equations.

## Status

GitHub CI is disabled at the user's request. The .NET and Java workflows are disabled
in GitHub and retain only manual dispatch triggers in source. Re-enable them explicitly
before requesting a manual GitHub run. Local build and test commands remain available.

Alpha prototype. Source baseline currently exists in the local project archive; the full source import to GitHub is pending. Do not treat CI as passing until the complete source is uploaded and workflows execute.

## Engineering goals

- Java JSR 385/Indriya behavioral compatibility where applicable
- UnitsNet interoperability
- Typed Ohm's law and electrical quantity algebra
- Dimension-checked symbolic solving using optional AngouriMath adapter
- Traceable requirements, STRIDE assessment, XML API documentation and DocFX
- Cross-platform .NET 8 build and tests

See GitHub Issues for work tracking.

For temperatures, see [absolute values, intervals, and shifted units](docs/temperature-semantic-envelope.md).
The dedicated temperature types enforce physical arithmetic rules; `JavaQuantityAdapter`
follows Indriya's compatibility conventions.

## Requirements closure

The [Java gap inventory](docs/java-parity-gap-analysis.md) and [closure policy](docs/requirements-closure.md) track each JP/EE requirement independently. GitHub issue closure does not imply verified feature parity.
