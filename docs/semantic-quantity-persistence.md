# Semantic quantity persistence

`SemanticQuantityJson` stores a versioned record containing SI value, semantic kind ID and optional display unit. Deserialization requires a trusted `QuantityKindRegistry`, and display units are checked against a `UnitRegistry`. Unknown kinds, incompatible display units and unsupported versions are rejected.

Requirement IDs: SU-SEM-006 through SU-SEM-010. Tests: `SemanticQuantityJsonTests`.
