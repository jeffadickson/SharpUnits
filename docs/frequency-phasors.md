# Frequency-tagged phasors and complex power

The `FrequencyVoltage`, `FrequencyCurrent`, and `FrequencyImpedance` types carry a positive frequency in hertz and reject operations between different frequencies. Their phasor values use RMS, not peak amplitudes.

Complex power is calculated as `S = V_RMS * Conjugate(I_RMS)`. `Watts` is the real component, `Vars` the reactive component, and `ApparentVoltAmperes` the magnitude. With passive sign convention, ideal inductive loads have positive reactive power.

These APIs are for single-frequency sinusoidal steady state. They do not model harmonic spectra, frequency tolerances, three-phase networks, or non-sinusoidal power factor.

Tests: `FrequencyPhasorTests`. This is an AC extension, not a JSR 385 TCK requirement.
