# Semantic quantity kinds

`QuantityKindRegistry` associates stable semantic identifiers with exact rational SI dimensions. `SemanticQuantity` checks both. Energy and torque have identical dimensions but distinct kinds, and cannot be added. `MultiplyAs` requires an explicit result kind; it checks dimensions but does not prove the physical law.

Requirement IDs: SU-SEM-001 through SU-SEM-005. Tests: `SemanticQuantityTests`.
