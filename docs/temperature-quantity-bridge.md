# Temperature quantity bridge

`TemperatureQuantityBridge` provides explicit conversion between absolute/differential temperature and the general `Quantity` model. Both share temperature dimensions, so converting to `Quantity` discards the absolute/difference distinction. Avoid arithmetic on untagged temperature quantities; prefer `TemperatureMeasurement` for persistence.

Requirement IDs: SU-TEMP-001 through SU-TEMP-005. Tests: `TemperatureQuantityBridgeTests`.
