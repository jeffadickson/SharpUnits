# AC and Bode development status

This increment imports the core functionality from the local AC/Bode prototype into the main .NET 10 library:

- `AcElectricalQuantities.cs`: complex impedance, angular frequency and RMS phasor primitives.
- `RlcFrequencyResponse.cs`: ideal series RLC impedance, resonance and Q.
- `BodeFrequencyResponse.cs`: RC/RL low/high-pass and series RLC resistor-output transfer functions.
- `BodeFrequencyResponseTests.cs`: analytical xUnit checks.
- `demo/Program.cs`: 501 logarithmic samples per filter, exported to CSV.
- `tools/verify_bode_export.py`: independent analytical oracle, checking all 2,505 magnitude/phase samples.
- CI gate: runs .NET exporter and Python oracle; uploads CSV evidence.

The earlier local cumulative ZIP also contains additional frequency-tagged phasor APIs, a browser-interactive HTML demo, static plot PNGs, and more extensive test/reference datasets. **Those assets have not all been imported in this increment.** They remain available in the local cumulative ZIP for subsequent integration. Do not claim full archive parity until a file-by-file comparison is complete.

The Java TCK remains a separate compatibility gate. Passing this AC extension does not close any Java TCK assertions.
