# SharpUnits

C# dimensional analysis, electrical quantities, and symbolic engineering equations.

## Status

GitHub CI is disabled at the user's request. The .NET and Java workflows are disabled
in GitHub and retain only manual dispatch triggers in source. Re-enable them explicitly
before requesting a manual GitHub run. Local build and test commands remain available.

Alpha .NET 10 library. Java compatibility work is in progress; passing tests do not
imply full JSR 385 conformance. See the [assertion review](conformance/quantity-interface-review.md)
for executable evidence and remaining differences.

## Engineering goals

- Java JSR 385/Indriya behavioral compatibility where applicable
- UnitsNet interoperability
- Typed Ohm's law and electrical quantity algebra
- Dimension-checked symbolic solving using optional AngouriMath adapter
- Traceable requirements, STRIDE assessment, XML API documentation and DocFX
- Cross-platform .NET 10 build and tests

See GitHub Issues for work tracking.

For temperatures, see [absolute values, intervals, and shifted units](docs/temperature-semantic-envelope.md).
The dedicated temperature types enforce physical arithmetic rules; `JavaQuantityAdapter`
follows Indriya's compatibility conventions.

`IJavaQuantityFactory.CreateJavaQuantity` creates quantities retaining their display
unit and absolute/relative scale. See the [factory review and examples](conformance/factory-fifty-review.md).

`QuantityTextFormat.ParseJavaQuantity` retains display values and units for a
documented subset of Java decimal/rational and linear compound-unit syntax. It also
accepts whitespace after `÷` and the `℃` alias. See the
[parser gap implementations and evidence](conformance/parser-gap-fifty-review.md).
The [100-review grammar increment](conformance/parser-grammar-hundred-review.md)
adds superscript/double-star powers, colon roots, integer unit factors and automatic
SI prefixes. Java-specific default-exponent conventions and remaining limits are explicit.

`JavaNumber`, `JavaUnit` and `JavaNumberQuantity` provide an exact-number and structural-unit core. Use `IExactJavaQuantityFactory` for original integer/decimal inputs; `JavaQuantityAdapter.ToExact()` promotes an existing display double. Default `JavaNumber.Add`/`Subtract` preserve large integer magnitude; explicitly named `AddPinnedIndriya`/`SubtractPinnedIndriya` reproduce the measured Java narrowing bug. See the [verification repairs](conformance/verification-repair.md). See the [core reconciliation and migration notes](conformance/work-package-one-review.md) and [runnable example](examples/ExactJavaCore.cs).

## Requirements closure

The [Java gap inventory](docs/java-parity-gap-analysis.md) and [closure policy](docs/requirements-closure.md) track each JP/EE requirement independently. GitHub issue closure does not imply verified feature parity.

Current verification and reviewed residuals: [mutation debt reduction](conformance/mutation-debt.md).
