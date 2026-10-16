#!/usr/bin/env python3
"""Compare Java reference JSONL records with .NET reference JSONL records.

Each line must contain id and either result (numeric) or error (category).
Comparison tolerances are explicit and per-case, never implicit rounding.
"""
import argparse
import json
import math
from pathlib import Path

def load(path):
    rows={}
    for line in Path(path).read_text().splitlines():
        if not line.strip():
            continue
        record=json.loads(line)
        identifier=record["id"]
        if identifier in rows:
            raise ValueError(f"Duplicate case: {identifier}")
        rows[identifier]=record
    return rows

def compare(java,dotnet):
    problems=[]
    for identifier in sorted(set(java)|set(dotnet)):
        a=java.get(identifier);b=dotnet.get(identifier)
        if a is None or b is None:
            problems.append(f"{identifier}: missing case")
            continue
        if a.get("error")!=b.get("error"):
            problems.append(f"{identifier}: error categories differ")
            continue
        if a.get("error") is not None:
            continue
        expected=a.get("result");actual=b.get("result")
        if isinstance(expected,(int,float)) and not isinstance(expected,bool) and isinstance(actual,(int,float)) and not isinstance(actual,bool):
            absolute=a.get("absoluteTolerance",0)
            relative=a.get("relativeTolerance",0)
            if not math.isclose(expected,actual,rel_tol=relative,abs_tol=absolute):
                problems.append(f"{identifier}: numerical mismatch {expected} vs {actual}")
        elif expected!=actual:
            problems.append(f"{identifier}: value mismatch")
    return problems

def main():
    p=argparse.ArgumentParser()
    p.add_argument("java")
    p.add_argument("dotnet")
    args=p.parse_args()
    errors=compare(load(args.java),load(args.dotnet))
    print(f"Differential comparison: {len(errors)} mismatch(es)")
    for error in errors:
        print(error)
    if errors:
        raise SystemExit(1)

if __name__=="__main__":
    main()
