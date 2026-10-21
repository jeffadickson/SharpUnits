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
  expected=case["expected"]
  result=row.get("result")
  matches=(isinstance(result,(int,float)) and not isinstance(result,bool) and math.isclose(result,expected,rel_tol=1e-12,abs_tol=1e-10)) if isinstance(expected,(int,float)) and not isinstance(expected,bool) else type(result) is type(expected) and result==expected
  if "error" in row or not matches:
   raise SystemExit(f"{case['id']}: wrong value")
print(f"Validated {len(cases)} shared conformance fixtures")
