# Temperature semantic envelope

`TemperatureMeasurement` stores magnitude in kelvin and a `TemperatureKind` of `Absolute` or `Difference`. Absolute minus absolute yields a difference; absolute plus difference yields absolute; difference plus difference yields difference. Absolute plus absolute and difference minus absolute are rejected. JSON serialization preserves kind.

Requirement IDs: SU-TEMP-006 through SU-TEMP-010. Tests: `TemperatureMeasurementTests`.
