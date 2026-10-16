#!/usr/bin/env python3
"""Validate normalized JSONL against shared fixture expectations."""
import argparse,json,math
from pathlib import Path
p=argparse.ArgumentParser()
p.add_argument("cases")
p.add_argument("actual")
a=p.parse_args()
cases=json.loads(Path(a.cases).read_text())["cases"]
rows=[json.loads(line) for line in Path(a.actual).read_text().splitlines() if line.strip()]
actual={r["id"]:r for r in rows}
if len(actual)!=len(rows) or set(actual)!={c["id"] for c in cases}:
 raise SystemExit("Duplicate, missing or unexpected case IDs")
for case in cases:
 row=actual[case["id"]]
 if "expectedError" in case:
  if row.get("error")!=case["expectedError"]:
   raise SystemExit(f"{case['id']}: wrong error")
 else:
  if "error" in row or not math.isclose(row["result"],case["expected"],rel_tol=1e-12,abs_tol=1e-10):
   raise SystemExit(f"{case['id']}: wrong value")
print(f"Validated {len(cases)} shared conformance fixtures")
