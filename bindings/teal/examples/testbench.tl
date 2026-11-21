-- Example Teal instrument testbench & plotting script using SharpUnits
local units = require("sharpunits")

-- Ohm's law with dynamic SI prefixes and symbolic units
local current = units.qty(250, "mA")
local load_r  = units.qty(4, "kOhm")
local voltage = (current * load_r):in_unit("kV")
print("Load voltage:", voltage:format("eng"))

-- Compile-time affine temperature safety
local ambient = units.point(25.0, "°C")
local boiling = units.point(212.0, "degF")
local delta_t = (boiling - ambient):in_unit("K")
print("Temperature rise:", delta_t:format("G4"))
-- Note: `local invalid = ambient + boiling` is rejected by `tl check`!

-- Gauge to absolute pressure
local p_gauge = units.point(2.0, "bar", "gauge")
local p_abs   = p_gauge:relative_to("vacuum")
print("Absolute pressure (bar):", p_abs.value)

-- SIMD-backed waveform buffer auto-scaled for plotting
local trace = units.series({ -0.05, 0.0, 0.125, 0.25 }, "V"):auto_scale()
print("Axis unit:", trace.unit.symbol, "RMS:", trace:rms():format("G4"))
