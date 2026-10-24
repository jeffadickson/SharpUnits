# Java quantity formatting review

Baseline: `e9dcd8e`. GitHub CI remains disabled.

This increment targets retained display-value formatting, pinned Java double text,
canonical supported unit labels, appendable output and locale-sensitivity reporting.
The existing SI-only Format API is preserved. One hundred numbered scenarios will
be checked against actual pinned Indriya, with compilation/full tests every ten,
coverage, mutations and seeded generated/fuzz verification. These counts are not
commits or formal assertion closures.

Source review: SimpleQuantityFormat.format writes the retained Number text, omits
the unit/delimiter for AbstractUnit.ONE, otherwise appends a delimiter and the
SimpleUnitFormat label. It returns its original Appendable. QuantityFormat's
default isLocaleSensitive method returns false (confirmed from the pinned API
bytecode). NumberQuantity narrows integral Double inputs before formatting.

The supported numeric profile is constructed finite doubles. Java Number identity,
parsed BigDecimal lexical scale and arbitrary unit-expression labels remain gaps.
