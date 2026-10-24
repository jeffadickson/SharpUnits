# Parser gap implementation review

Starting commit: `9d469b2`. This increment targets the three executable gaps
previously recorded as P-LIMIT-02 (compound units), P-LIMIT-03 (whitespace after
rational division), and P-LIMIT-04 (Celsius alias). Arbitrary Java Number storage
remains a separate architectural gap.

Fifty numbered Java/CLR scenarios will be reviewed, with compile/full-test
checkpoints every ten, plus coverage, seeded properties/fuzzing and mutation checks.
These are scenario reviews, not fifty commits or formal TCK closures. CI stays off.

Source review identified an important constraint: pinned SimpleUnitFormat parses
division chains recursively on the right. The existing CLR UnitExpressionParser
uses conventional left association. Reusing it unchanged would produce wrong
dimensions for expressions such as A/A/A. The Java parsing path must preserve its
own convention while retaining the old public parser's behavior.
