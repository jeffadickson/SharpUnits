# Temperature quantity bridge

`TemperatureQuantityBridge` provides explicit conversion between absolute/differential temperature and the general `Quantity` model. Both share temperature dimensions, so converting to `Quantity` discards the absolute/difference distinction. Avoid arithmetic on untagged temperature quantities; prefer `TemperatureMeasurement` for persistence.

See [temperature arithmetic and shifted-unit conversions](temperature-semantic-envelope.md)
for the typed operation rules, scale-only interval conversions, and the intentional
distinction from `JavaQuantityAdapter`'s Indriya behavior.

Requirement IDs: SU-TEMP-001 through SU-TEMP-005. Tests: `TemperatureQuantityBridgeTests`.
