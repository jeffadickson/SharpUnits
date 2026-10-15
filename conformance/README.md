# Differential Java/.NET conformance

Shared cases in `cases.json` are versioned and reference the Java parity requirement IDs. A future Java JSR 385/Indriya runner must emit normalized JSON records with id, result or error category. A .NET 10 xUnit runner must evaluate the same inputs and compare dimensions, values and errors. Do not mark a requirement verified from static fixture expectations alone.

Reference dependencies must be pinned, and numeric tolerances documented. Keep semantic kind separate from dimensional equivalence. Capture reference version, runtime version, CI URL and per-case results in the closure ledger.

**Current state:** fixtures defined; Java reference runner and differential execution not yet implemented.
