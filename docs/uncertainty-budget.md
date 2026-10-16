# Correlated uncertainty budgets

`UncertaintyBudget` calculates first-order combined standard uncertainty using sensitivity coefficients, standard uncertainty contributions and a symmetric positive-semidefinite correlation matrix.

\\[u_c^2=\\sum_i\\sum_j c_i c_j u_i u_j \\rho_{ij}\\]

The implementation is intended for small laboratory budgets, not numerically ill-conditioned covariance matrices. Correlation matrices are defensively copied, and input validation rejects duplicate IDs, nonfinite uncertainties, invalid correlations and negative coverage factors. `ApplyTo` copies the calculated standard uncertainty into an `InstrumentMeasurement`; it does not independently validate the physical units of sensitivity coefficients.

Requirement IDs: SU-UNC-001 through SU-UNC-005. Tests: `UncertaintyBudgetTests`.
