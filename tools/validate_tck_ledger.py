#!/usr/bin/env python3
"""Validate the TCK assertion inventory and closure ledger without dependencies."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
inventory = json.loads((ROOT / "conformance/tck-assertions.json").read_text())
ledger = json.loads((ROOT / "conformance/tck-closure-ledger.json").read_text())
expected = {(suite["path"], aid) for suite in inventory["suites"] for aid in suite["assertions"]}
actual = {(item["source"], item["id"]) for item in ledger["entries"]}
if len(actual) != len(ledger["entries"]):
    raise SystemExit("Duplicate assertion IDs/source pairs in closure ledger")
if expected != actual:
    missing = expected - actual
    extra = actual - expected
    raise SystemExit(f"TCK inventory mismatch: missing={missing}, extra={extra}")
if ledger["totalAssertions"] != len(expected):
    raise SystemExit("Assertion count mismatch")
allowed = {"GAP", "IMPLEMENTED_UNVERIFIED", "PARTIAL_BEHAVIORAL_MAPPING", "CI_PASS_BEHAVIORAL_REVIEW_PENDING", "VERIFIED", "NOT_APPLICABLE"}
for entry in ledger["entries"]:
    if entry["status"] not in allowed:
        raise SystemExit(f"Invalid status for {entry['id']}: {entry['status']}")
    if entry["status"] == "VERIFIED" and (not entry.get("test") or not entry.get("evidence") or not entry.get("javaEvidence")):
        raise SystemExit(f"Missing closure evidence for {entry['id']}")
print(f"TCK ledger valid: {len(expected)} assertions; " +
      ", ".join(f"{s}={sum(e['status']==s for e in ledger['entries'])}" for s in sorted(allowed)))
