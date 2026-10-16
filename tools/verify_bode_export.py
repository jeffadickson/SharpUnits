#!/usr/bin/env python3
"""Independent analytic oracle for SharpUnits Bode CSV; standard library only."""
import csv, math, cmath, sys
from collections import defaultdict
if len(sys.argv)!=2: raise SystemExit("usage: python3 tools/verify_bode_export.py output.csv")
groups=defaultdict(list)
with open(sys.argv[1],newline="",encoding="utf-8") as f:
 reader=csv.DictReader(f)
 if reader.fieldnames!=["model","hertz","magnitude_db","phase_degrees"]:raise SystemExit("CSV schema mismatch")
 for row in reader:
  model=row["model"]
  if model not in {"rc-low","rc-high","rl-low","rl-high","rlc-band"}:raise SystemExit("Unknown model")
  hz=float(row["hertz"]);mag=float(row["magnitude_db"]);phase=float(row["phase_degrees"])
  if not all(math.isfinite(v) for v in (hz,mag,phase)) or hz<=0:raise SystemExit("Nonfinite or invalid sample")
  groups[model].append((hz,mag,phase))
if set(groups)!={"rc-low","rc-high","rl-low","rl-high","rlc-band"}:raise SystemExit("Missing model")
for model,rows in groups.items():
 if len(rows)!=501:raise SystemExit(f"{model}: expected 501 samples")
 previous=0
 for hz,mag,phase in rows:
  if hz<=previous:raise SystemExit("Frequencies not strictly increasing")
  previous=hz
  w=2*math.pi*hz
  if model=="rc-low":gain=1/(1+1j*w*1000*1e-6)
  elif model=="rc-high":
   t=1j*w*1000*1e-6
   gain=t/(1+t)
  elif model=="rl-low":gain=1000/(1000+1j*w*.01)
  elif model=="rl-high":gain=1j*w*.01/(1000+1j*w*.01)
  else:gain=10/(10+1j*(w*.01-1/(w*1e-6)))
  expected_mag=20*math.log10(abs(gain))
  expected_phase=math.degrees(cmath.phase(gain))
  if not math.isclose(mag,expected_mag,rel_tol=0,abs_tol=1e-7):
   raise SystemExit(f"{model} {hz}: magnitude mismatch {mag} vs {expected_mag}")
  phase_error=(phase-expected_phase+180)%360-180
  if abs(phase_error)>1e-7:raise SystemExit(f"{model} {hz}: phase mismatch {phase} vs {expected_phase}")
print("PASS: 2505 independent Bode magnitude/phase comparisons")
