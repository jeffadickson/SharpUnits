# Calibration decisions

`CalibrationRecord` validates instrument and calibration IDs and an inclusive-start/exclusive-end validity interval. `MeasurementDecisionRule` returns Pass, Fail, Indeterminate or InvalidCalibration. Expanded uncertainty is computed from the supplied standard uncertainty and coverage factor. A guard band narrows the acceptance interval; boundary overlap is indeterminate.

Requirement IDs: SU-CAL-001 through SU-CAL-005. Tests: `CalibrationDecisionTests`.
